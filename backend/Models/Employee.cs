//contains the C# definition of an employee.
//EF Core maps that entity to the Employees table in the database in SQL Server.
//use namespace to avoid naming conflicts
namespace EmployeeManagement.Api.Models;

//class lets us group all those together and create multiple Employee objects.
public class Employee
{   // get: allow to read value | set: allow to write value
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty; //string.Empty: default value for string type | FullName= ""

    public string Email { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;

    public DateTime DateOfJoining { get; set; }

    public EmployeeStatus Status { get; set; } 
}