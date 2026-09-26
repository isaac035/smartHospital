using SmartHospital.Api.DTOs.ConsultationTypes;

namespace SmartHospital.Api.Services.Interfaces;

public interface IConsultationTypeService
{
    Task<List<ConsultationTypeResponse>> GetAllAsync();

    Task<ConsultationTypeResponse?> GetByIdAsync(int id);

    Task<ConsultationTypeResponse> CreateAsync(CreateConsultationTypeRequest request);

    Task<ConsultationTypeResponse?> UpdateAsync(
        int id,
        UpdateConsultationTypeRequest request);

    Task<bool> DeactivateAsync(int id);
}
