//DTO is used to seperate the data accepted by the API from the database entity.
using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Api.Models;

namespace EmployeeManagement.Api.DTOs;

public class CreateEmployeeDto
{   //input validation
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Department { get; set; } = string.Empty;

    [Required]
    public string JobTitle { get; set; } = string.Empty;

    [Required]
    public EmployeeStatus Status { get; set; }
}