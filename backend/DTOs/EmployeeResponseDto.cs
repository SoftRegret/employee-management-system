using EmployeeManagement.Api.Models;

namespace EmployeeManagement.Api.DTOs;

// API Contract: defines employee list response fields independently of global Excel metadata.
public class EmployeeResponseDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;

    public DateTime DateOfJoining { get; set; }

    public EmployeeStatus Status { get; set; }

}