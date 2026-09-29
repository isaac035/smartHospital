using System.ComponentModel.DataAnnotations;

namespace SmartHospital.Api.DTOs.Schedules;

public class AddScheduleSlotsRequest
{
    [Required]
    public DateOnly Date { get; set; }

    [Range(1, 100)]
    public int AdditionalSlotCount { get; set; }
}
