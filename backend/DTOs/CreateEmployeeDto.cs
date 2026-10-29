using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using EmployeeManagement.Api.Models;

namespace EmployeeManagement.Api.DTOs;

// DTO: accepts editable fields only; clients cannot set Id or DateOfJoining.
// Interface: IValidatableObject supplies custom validation to ASP.NET Core.
public class CreateEmployeeDto : IValidatableObject
{
    // Server-side Validation: required fields and valid status values are checked before CRUD runs.
    [Required(ErrorMessage = "Full name is required.")]
    public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Email is required.")]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Department is required.")]
    public string Department { get; set; } = string.Empty;
    [Required(ErrorMessage = "Job title is required.")]
    public string JobTitle { get; set; } = string.Empty;
    [EnumDataType(typeof(EmployeeStatus), ErrorMessage = "Select a valid employee status.")]
    public EmployeeStatus Status { get; set; }

    // Business Rules: names exclude digits and unsupported characters; email must be lowercase and valid.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var name = FullName?.Trim() ?? "";
        var email = Email?.Trim() ?? "";
        if (Regex.IsMatch(name, @"\p{Nd}"))
            yield return new ValidationResult("Full name cannot contain numbers.", new[] { nameof(FullName) });
        else if (name.Length > 0 && (!Regex.IsMatch(name, @"^[\p{L}\p{M} '\-]+$") || !Regex.IsMatch(name, @"\p{L}")))
            yield return new ValidationResult("Full name must contain letters and may only include spaces, hyphens and apostrophes.", new[] { nameof(FullName) });

        if (email.Any(char.IsUpper))
            yield return new ValidationResult("Email must be in lowercase.", new[] { nameof(Email) });
        else if (email.Length > 0 && !Regex.IsMatch(email, @"^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$"))
            yield return new ValidationResult("Enter a valid email address.", new[] { nameof(Email) });
    }
}
