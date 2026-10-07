using System.ComponentModel.DataAnnotations;
using SmartHospital.Api.Validation;

namespace SmartHospital.Api.DTOs.Departments;

public class CreateDepartmentRequest
{
    [Required]
    [MaxLength(100)]
    [NoHtml]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [NoHtml]
    public string Description { get; set; } = string.Empty;
}
