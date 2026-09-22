using SmartHospital.Api.DTOs.MedicalResources;

namespace SmartHospital.Api.Services.Interfaces;

public interface IMedicalResourceService
{
    Task<MedicalResourceResponse> CreateResourceAsync(CreateResourceRequest request);
    Task<List<MedicalResourceResponse>> GetResourcesAsync(ResourceQueryFilter filter);
    Task<MedicalResourceResponse?> GetResourceByIdAsync(int id);
    Task<MedicalResourceResponse?> UpdateResourceAsync(int id, UpdateResourceRequest request);
    Task<MedicalResourceResponse?> AssignResourceAsync(int id, AssignResourceRequest request);
    Task<bool> DeactivateResourceAsync(int id);
}
