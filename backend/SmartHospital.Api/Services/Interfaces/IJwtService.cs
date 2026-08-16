using SmartHospital.Api.Models;

namespace SmartHospital.Api.Services.Interfaces;

public interface IJwtService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}