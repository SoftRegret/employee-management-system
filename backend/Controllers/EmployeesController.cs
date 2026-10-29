//controller is where CRUD operations from React application are handled.
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

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
    [HttpGet] //this endpoints handles the GET request 
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees() //use EF CORE to retrieve all employees from the database asynchronously.
    {
        var employees = await _context.Employees.ToListAsync(); //ASP.NET core serializes the result to JSON for the frontend.

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
            .AnyAsync(e => e.Email == dto.Email);

        if (emailExists)
        {
            return Conflict(
                "An employee with this email already exists."); //API returns  409 Conflict
        }

        //map the values from the DTO to a new Employee entity.
        var employee = new Employee
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Department = dto.Department,
            JobTitle = dto.JobTitle,
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
                e.Email == dto.Email &&
                e.Id != id);

        if (emailExists)
        {
            return Conflict(
                "An employee with this email already exists.");
        }

        //date of joining is not updated as it is set when the employee is created and should not change.
        employee.FullName = dto.FullName;
        employee.Email = dto.Email;
        employee.Department = dto.Department;
        employee.JobTitle = dto.JobTitle;
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

    [HttpPost("{id}/excel")]
public async Task<IActionResult> UploadExcel(int id, IFormFile file)
{
    var employee = await _context.Employees.FindAsync(id);

    if (employee == null)
    {
        return NotFound("Employee not found.");
    }

    if (file == null || file.Length == 0)
    {
        return BadRequest("No file was uploaded.");
    }

    var extension = Path.GetExtension(file.FileName).ToLower();

    if (extension != ".xlsx")
    {
        return BadRequest("Only .xlsx Excel files are allowed.");
    }

    using var memoryStream = new MemoryStream();

    await file.CopyToAsync(memoryStream);

    var existingDocument = await _context.EmployeeDocuments
        .FirstOrDefaultAsync(d => d.EmployeeId == id);

    if (existingDocument != null)
    {
        existingDocument.FileName = file.FileName;
        existingDocument.ContentType = file.ContentType;
        existingDocument.FileData = memoryStream.ToArray();
        existingDocument.UploadedAt = DateTime.Now;
    }
    else
    {
        var document = new EmployeeDocument
        {
            EmployeeId = id,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileData = memoryStream.ToArray(),
            UploadedAt = DateTime.Now
        };

        _context.EmployeeDocuments.Add(document);
    }

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Excel file uploaded successfully.",
        fileName = file.FileName
    });
}

    [HttpGet("{id}/excel")]
public async Task<IActionResult> ViewExcel(int id)
{
    var employee = await _context.Employees.FindAsync(id);

    if (employee == null)
    {
        return NotFound("Employee not found.");
    }

    var document = await _context.EmployeeDocuments
        .FirstOrDefaultAsync(d => d.EmployeeId == id);

    if (document == null)
    {
        return NotFound("No Excel file found for this employee.");
    }

    using var stream = new MemoryStream(document.FileData);
    using var workbook = new XLWorkbook(stream);

    var sheets = new List<object>();

    foreach (var worksheet in workbook.Worksheets)
    {
        var rows = new List<List<string>>();

        foreach (var row in worksheet.RowsUsed())
        {
            var cells = row.Cells(1, worksheet.LastColumnUsed()?.ColumnNumber() ?? 1)
                .Select(cell => cell.GetFormattedString())
                .ToList();

            rows.Add(cells);
        }

        sheets.Add(new
        {
            name = worksheet.Name,
            rows = rows
        });
    }

    return Ok(new
    {
        fileName = document.FileName,
        uploadedAt = document.UploadedAt,
        sheets = sheets
    });
}

    [HttpGet("{id}/excel/download")]
public async Task<IActionResult> DownloadExcel(int id)
{
    var employee = await _context.Employees.FindAsync(id);

    if (employee == null)
    {
        return NotFound("Employee not found.");
    }

    var document = await _context.EmployeeDocuments
        .FirstOrDefaultAsync(d => d.EmployeeId == id);

    if (document == null)
    {
        return NotFound("No Excel file found for this employee.");
    }

    return File(
        document.FileData,
        document.ContentType,
        document.FileName
    );
}
}