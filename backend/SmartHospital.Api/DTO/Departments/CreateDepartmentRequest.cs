using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Departments;

public class CreateDepartmentRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
