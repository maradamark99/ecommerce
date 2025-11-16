using EcommerceLib.Auth;

namespace EcommerceLib.Testing.Auth;

public class AuthorizedClientBuilder(HttpClient client)
{
    private readonly HttpClient client = client;

    public string UserId { get; private set; } = null!;

    public string? Email { get; private set; }

    public string? Name { get; private set; }

    public IEnumerable<Roles>? Roles { get; private set; } = null!;

    public AuthorizedClientBuilder WithUserId(string userId)
    {
        UserId = userId;
        return this;
    }

    public AuthorizedClientBuilder WithEmail(string email)
    {
        Email = email;
        return this;
    }

    public AuthorizedClientBuilder WithName(string name)
    {
        Name = name;
        return this;
    }

    public AuthorizedClientBuilder WithRoles(params Roles[] roles)
    {
        Roles = roles;
        return this;
    }
    
    public HttpClient Build()
    {
        if (Roles == null || !Roles.Any())
        {
            Roles = [EcommerceLib.Auth.Roles.Customer];
        }
        if (string.IsNullOrEmpty(UserId))
        {
            UserId = Guid.NewGuid().ToString();
        }
        if (string.IsNullOrEmpty(Email))
        {
            Email = "john.doe@test.com";
        }
        client.DefaultRequestHeaders.Add(AuthHeaders.UserId, UserId);
        if (!string.IsNullOrEmpty(Email))
        {
            client.DefaultRequestHeaders.Add(AuthHeaders.UserEmail, Email);            
        }
        client.DefaultRequestHeaders.Add(
            AuthHeaders.UserRoles,
            string.Join(",", Roles!.Select(r => r.ToString()))
        );        
        return client;
    }
}