using System.Security.Claims;
using Fcmb.Assessment.CSharp.Common.Application.Authorization;
using Microsoft.AspNetCore.Http;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Authorization;

internal sealed class ClaimsProvider(
    IHttpContextAccessor httpContextAccessor) : IClaimsProvider
{
    private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;

    #region Claims

    public string GetUserId() =>
        _user?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("JWT missing 'sub' claim.");

    #endregion
}
