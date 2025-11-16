using System.Security.Claims;
using EcommerceLib.Auth;

namespace ApiGateway;

public class UserClaimsHandler(IHttpContextAccessor contextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = contextAccessor.HttpContext;
        const string correlationId = "X-Correlation-Id";
        
        request.Headers.Remove(correlationId);
        request.Headers.Remove(AuthHeaders.UserId);
        request.Headers.Remove(AuthHeaders.UserEmail);
        request.Headers.Remove(AuthHeaders.UserRoles);
        
        if (context?.User.Identity?.IsAuthenticated == true)
        {
            var principal = context.User;

            request.Headers.Add(correlationId, Guid.NewGuid().ToString());
            
            var id = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(id?.Value))
            {
                request.Headers.Add(AuthHeaders.UserId, principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            }
            var email = principal.FindFirst(ClaimTypes.Email);
            if (!string.IsNullOrEmpty(email?.Value))
            {
                request.Headers.Add(AuthHeaders.UserEmail, principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty);
            }
            var roles = string.Join(",", principal.FindAll(ClaimTypes.Role).Select(r => r.Value));
            if (!string.IsNullOrEmpty(roles))
            {
                request.Headers.Add(AuthHeaders.UserRoles, roles);
            }
        }
        
        return base.SendAsync(request, cancellationToken);
    }
}