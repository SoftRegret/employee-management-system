namespace EmployeeManagement.Api.Models;

public class Employee
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;

    public DateTime DateOfJoining { get; set; }

    public string Status { get; set; } = string.Empty;
}