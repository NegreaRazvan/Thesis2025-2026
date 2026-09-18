using Domain.Exceptions.Custom;
using System.Security.Claims;

namespace Controller.Security;

public static class ClaimsHelper
{
    public static string GetUserId(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedException("User ID claim is missing from token.");
        return userId;
    }
}
