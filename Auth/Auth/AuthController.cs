using System.Net;
using Auth.Contract;
using EcommerceLib.Contract;
using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(
    IEmailService emailService,
    IUserContextService userContextService,
    ITokenProvider tokenProvider,
    IEventProducer<AuthEventDto> eventProducer, 
    UserManager<AppUser> userManager,
    AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Route("test")]
    public IActionResult Test()
    {
        return Ok("Auth service is running.");
    }

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        var existingUser = await userManager.FindByEmailAsync(registerRequest.Email);
        if (existingUser != null)
        {
            return Conflict("Email is already registered.");
        }
        var newUser = new AppUser()
        {
            Email = registerRequest.Email,
            NormalizedEmail = registerRequest.Email.ToUpper(),
            UserName = registerRequest.Email,
        };
        var creationResult = await userManager.CreateAsync(newUser, registerRequest.Password);
        if (!creationResult.Succeeded)
        {
            return BadRequest(creationResult);
        }
        var roleAdditionResult = await userManager.AddToRoleAsync(newUser, nameof(Roles.Customer));
        if (!roleAdditionResult.Succeeded)
        {
            await userManager.DeleteAsync(newUser);
            return BadRequest(roleAdditionResult);
        }
        if (!creationResult.Succeeded || !roleAdditionResult.Succeeded)
        {
            return BadRequest(creationResult);
        }
        var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(newUser);
        var encodedToken = WebUtility.UrlEncode(emailConfirmationToken);
        var confirmationLink = Url.Action(  // this should actually be the gateway address
            nameof(ConfirmEmail), 
            "Auth", 
            new { userId = newUser.Id, token = encodedToken }, 
            Request.Scheme);
        
        await emailService.SendAsync
        (
            registerRequest.Email, 
            "Email confirmation", 
            GetConfirmationEmailBody(confirmationLink!)
        );
        return Created();
    }

    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest loginRequest)
    {
        var existingUser = await userManager.FindByEmailAsync(loginRequest.Email);
        if (existingUser is null)
        {
            return Unauthorized();
        }
        var isPasswordCorrect = await userManager.CheckPasswordAsync(existingUser, loginRequest.Password);
        if (!isPasswordCorrect || !await userManager.IsEmailConfirmedAsync(existingUser))
        {
            return Unauthorized();
        }
        
        var roles = await userManager.GetRolesAsync(existingUser); 
        var accessToken = tokenProvider.GenerateToken(existingUser, roles);
        var refreshToken = new RefreshToken()
        {
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            Token = tokenProvider.GenerateRefreshToken(),
            User = existingUser
        };
        await dbContext.RefreshTokens.AddAsync(refreshToken);
        await dbContext.SaveChangesAsync();
        
        return Ok(Task.FromResult(new TokenResponse(accessToken, refreshToken.Token)));
    }
    
    [HttpGet]
    [Route("confirm-email/{userId}/{token}")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var decodedToken = WebUtility.UrlDecode(token);
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }
        if (user.EmailConfirmed) 
        {
            return Ok();
        }

        var result = await userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            return BadRequest("Email confirmation failed");
        }

        await eventProducer.ProduceAsync(new AuthEventDto()
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.EmailConfirmed),
            CorrelationId = Guid.NewGuid().ToString(),
            CustomerId = user.Id,
            Email = user.Email!,
            Timestamp = DateTime.UtcNow
        });
        return Ok();
    }

    [HttpPost]
    [Route("refresh")]
    public async Task<ActionResult> RefreshToken([FromBody] string refreshTokenRequest)
    {
        var refreshToken = await dbContext.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == refreshTokenRequest);
        if (refreshToken is null || refreshToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            throw new InvalidDataException("The refresh token has expired.");
        }
        
        var roles = await userManager.GetRolesAsync(refreshToken.User); 
        var accessToken = tokenProvider.GenerateToken(refreshToken.User, roles);
        refreshToken.Token = tokenProvider.GenerateRefreshToken();
        refreshToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        await dbContext.SaveChangesAsync();
        
        return Ok(Task.FromResult(new TokenResponse(accessToken, refreshToken.Token)));
    }
    
    [HttpDelete]
    [Route("{userId}/revoke")]
    public async Task<ActionResult> RevokeToken(string userId)
    {
        if (userId != userContextService.GetUser()?.Id)
        {
            return Unauthorized();  
        }
        await dbContext.RefreshTokens
            .Where(r => r.User.Id == userId)
            .ExecuteDeleteAsync();
        return NoContent();
    }
    
    private static string GetConfirmationEmailBody(string confirmationLink)
    {
        return $"""
                <html lang="">
                <body>
                    <h2>Confirm Your Email</h2>
                    <p>Thank you for registering. Please confirm your email by clicking the link below:</p>
                    <a href="{confirmationLink}" style="padding:10px 20px; color:#fff; background:#007bff; text-decoration:none; border-radius:5px;">
                        Confirm Email
                    </a>
                    <p>If you did not register, please ignore this email.</p>
                </body>
                </html>
                """;
    }
}