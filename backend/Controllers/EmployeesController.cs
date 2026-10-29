//controller is where CRUD operations from React application are handled.
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/employees
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeResponseDto>>> GetEmployees()
    {
        var employees = await _context.Employees
            .Select(employee => new EmployeeResponseDto
            {
                Id = employee.Id,
                FullName = employee.FullName,
                Email = employee.Email,
                Department = employee.Department,
                JobTitle = employee.JobTitle,
                DateOfJoining = employee.DateOfJoining,
                Status = employee.Status
            })
            .ToListAsync();

        return Ok(employees);
    }

    // GET: api/employees/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Employee>> GetEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
        {
            return NotFound();
        }

        return Ok(employee);
    }

    // POST: api/employees
    [HttpPost]
    public async Task<ActionResult<Employee>> CreateEmployee(
        CreateEmployeeDto dto)
    {
        bool emailExists = await _context.Employees
            .AnyAsync(e => e.Email.ToLower() == dto.Email.Trim().ToLower());

        if (emailExists)
        {
            return Conflict(
                new { message = "An employee with this email already exists.", errors = new { Email = new[] { "An employee with this email already exists." } } }); //API returns  409 Conflict
        }

        //map the values from the DTO to a new Employee entity.
        var employee = new Employee
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            Department = dto.Department.Trim(),
            JobTitle = dto.JobTitle.Trim(),
            Status = dto.Status,
            DateOfJoining = DateTime.Now
        };

        _context.Employees.Add(employee);

        await _context.SaveChangesAsync(); //persists the new employee to the database.

        return CreatedAtAction(
            nameof(GetEmployee),
            new { id = employee.Id },
            employee);
    }

    // PUT: api/employees/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(
        int id,
        UpdateEmployeeDto dto)
    {
        var employee = await _context.Employees.FindAsync(id);
        //find the employee by ID in the database. If not found, return 404 Not Found.
        if (employee == null)
        {
            return NotFound();
        }
        //check for duplicate email.
        bool emailExists = await _context.Employees
            .AnyAsync(e =>
                e.Email.ToLower() == dto.Email.Trim().ToLower() &&
                e.Id != id);

        if (emailExists)
        {
            return Conflict(
                new { message = "An employee with this email already exists.", errors = new { Email = new[] { "An employee with this email already exists." } } });
        }

        //date of joining is not updated as it is set when the employee is created and should not change.
        employee.FullName = dto.FullName.Trim();
        employee.Email = dto.Email.Trim();
        employee.Department = dto.Department.Trim();
        employee.JobTitle = dto.JobTitle.Trim();
        employee.Status = dto.Status;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/employees/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
        {
            return NotFound();
        }

        _context.Employees.Remove(employee);

        await _context.SaveChangesAsync();

        return NoContent();
    }

}
