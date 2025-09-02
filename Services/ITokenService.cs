using System;
using System.Security.Claims;
using MyWebApi.Models;

namespace MyWebApi.Services;

public interface ITokenService
{
    Task<string> GenerateTokenAsync(ApplicationUser user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
