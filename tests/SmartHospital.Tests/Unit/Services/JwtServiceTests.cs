using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartHospital.Api.Configuration;
using SmartHospital.Api.Models;
using SmartHospital.Api.Services;
using Xunit;

namespace SmartHospital.Tests.Unit.Services;

public class JwtServiceTests
{
    private const string Key = "unit-test-signing-key-that-is-at-least-32-characters";
    private const string Issuer = "SmartHospital.Tests";
    private const string Audience = "SmartHospital.Clients";

    [Fact]
    public void GenerateToken_ReturnsWellFormedJwt()
    {
        var (token, _) = CreateService().GenerateToken(NewUser());

        Assert.Equal(3, token.Split('.').Length);
        Assert.True(new JwtSecurityTokenHandler().CanReadToken(token));
    }

    [Fact]
    public void GenerateToken_IncludesSubjectAndNameIdentifierClaims()
    {
        var jwt = Read(CreateService().GenerateToken(NewUser(id: 42)).Token);

        Assert.Equal("42", jwt.Subject);
        Assert.Equal("42", Claim(jwt, ClaimTypes.NameIdentifier));
    }

    [Fact]
    public void GenerateToken_IncludesEmailFullNameAndRoleClaims()
    {
        var jwt = Read(CreateService().GenerateToken(NewUser(role: UserRole.Admin)).Token);

        Assert.Equal("kasun@example.test", Claim(jwt, ClaimTypes.Email));
        Assert.Equal("Kasun Silva", Claim(jwt, ClaimTypes.Name));
        Assert.Equal("Admin", Claim(jwt, ClaimTypes.Role));
    }

    [Fact]
    public void GenerateToken_UsesConfiguredIssuerAndAudience()
    {
        var jwt = Read(CreateService().GenerateToken(NewUser()).Token);

        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Contains(Audience, jwt.Audiences);
    }

    [Fact]
    public void GenerateToken_IsSignedWithHmacSha256()
    {
        var jwt = Read(CreateService().GenerateToken(NewUser()).Token);

        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(60)]
    [InlineData(1440)]
    public void GenerateToken_ExpiryMatchesConfiguredMinutes(int minutes)
    {
        var before = DateTime.UtcNow;
        var (token, expiresAt) = CreateService(minutes).GenerateToken(NewUser());

        Assert.InRange(expiresAt, before.AddMinutes(minutes).AddSeconds(-5), DateTime.UtcNow.AddMinutes(minutes).AddSeconds(5));
        // JWT exp has second precision.
        Assert.InRange((Read(token).ValidTo - expiresAt).Duration(), TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GenerateToken_ValidatesWithTheCorrectKey()
    {
        var (token, _) = CreateService().GenerateToken(NewUser(role: UserRole.Doctor));

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(Key), out _);

        Assert.True(principal.IsInRole("Doctor"));
    }

    [Fact]
    public void GenerateToken_FailsValidationWithAWrongKey()
    {
        var (token, _) = CreateService().GenerateToken(NewUser());

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(
                token, ValidationParameters("a-completely-different-signing-key-of-32+-chars"), out _));
    }

    [Fact]
    public void GenerateToken_TamperedPayloadFailsValidation()
    {
        var (token, _) = CreateService().GenerateToken(NewUser(role: UserRole.Patient));
        var parts = token.Split('.');
        // Swap the payload for one claiming the Admin role, keeping the original signature.
        var forged = CreateService().GenerateToken(NewUser(role: UserRole.Admin)).Token.Split('.')[1];
        var tampered = $"{parts[0]}.{forged}.{parts[2]}";

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(tampered, ValidationParameters(Key), out _));
    }

    [Fact]
    public void GenerateToken_DifferentUsersGetDifferentTokens()
    {
        var service = CreateService();

        var first = service.GenerateToken(NewUser(id: 1)).Token;
        var second = service.GenerateToken(NewUser(id: 2)).Token;

        Assert.NotEqual(first, second);
    }

    private static JwtService CreateService(int expiryMinutes = 60) =>
        new(Options.Create(new JwtSettings
        {
            Key = Key,
            Issuer = Issuer,
            Audience = Audience,
            ExpiryMinutes = expiryMinutes
        }));

    private static TokenValidationParameters ValidationParameters(string key) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateLifetime = true
    };

    private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    private static string Claim(JwtSecurityToken jwt, string type) => jwt.Claims.First(c => c.Type == type).Value;

    private static User NewUser(int id = 7, UserRole role = UserRole.Patient) => new()
    {
        Id = id,
        FirstName = "Kasun",
        LastName = "Silva",
        Email = "kasun@example.test",
        Role = role,
        Status = UserStatus.Active
    };
}
