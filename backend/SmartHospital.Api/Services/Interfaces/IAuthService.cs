using SmartHospital.Api.DTOs.Auth;

namespace SmartHospital.Api.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    Task<AuthResponse?> LoginAsync(LoginRequest request);
}