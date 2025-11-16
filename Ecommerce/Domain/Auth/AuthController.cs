using System.Net;
using Ecommerce.Common;
using Ecommerce.Common.Data;
using Ecommerce.Common.Exception;
using Ecommerce.Domain.Auth.Contract;
using Ecommerce.Domain.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Domain.Auth;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(
    IEmailService emailService,
    IUserContextService userContextService,
    ITokenProvider tokenProvider,
    UserManager<AppUser> userManager,
    AppDbContext dbContext) : ControllerBase
{

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest registerRequest)
    {
        var existingUser = await userManager.FindByEmailAsync(registerRequest.Email);
        if (existingUser != null)
        {
            return Unauthorized();
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
        var roleAdditionResult = await userManager.AddToRoleAsync(newUser, RoleConstants.Customer);
        if (!roleAdditionResult.Succeeded)
        {
            await userManager.DeleteAsync(newUser);
            return BadRequest(roleAdditionResult);
        }
        var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(newUser);
        var encodedToken = WebUtility.UrlEncode(emailConfirmationToken);
        var confirmationLink = Url.Action(
            nameof(ConfirmEmailAsync), 
            "Auth", 
            new { userId = newUser.Id, token = encodedToken }, 
            Request.Scheme);
        
        await emailService.SendEmailAsync
        (
            registerRequest.Email, 
            "Email confirmation", 
            GetConfirmationEmailBody(confirmationLink!)
        );
        return Created();
    }

    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<TokenResponse>> LoginAsync([FromBody] LoginRequest loginRequest)
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
        
        var accessToken = tokenProvider.GenerateToken(existingUser);
        var refreshToken = new RefreshToken()
        {
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            Token = tokenProvider.GenerateRefreshToken(),
            User = existingUser,
            CreatedByIp = userContextService.GetIpAddress()
        };
        await dbContext.RefreshTokens.AddAsync(refreshToken);
        await dbContext.SaveChangesAsync();
        
        return Ok(Task.FromResult(new TokenResponse(accessToken, refreshToken.Token)));
    }
    
    [HttpGet]
    [Route("confirm-email/{userId}/{token}")]
    public async Task<IActionResult> ConfirmEmailAsync(string userId, string token)
    {
        var decodedToken = WebUtility.UrlDecode(token);
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var result = await userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            return BadRequest("Email confirmation failed");
        }

        return Ok();
    }

    [HttpPost]
    [Route("refresh")]
    public async Task<ActionResult> RefreshTokenAsync([FromBody] string refreshTokenRequest)
    {
        var refreshToken = await dbContext.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == refreshTokenRequest);
        if (refreshToken is null || refreshToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            throw new UnauthorizedException("The refresh token has expired.");
        }
        
        var accessToken = tokenProvider.GenerateToken(refreshToken.User);
        refreshToken.Token = tokenProvider.GenerateRefreshToken();
        refreshToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        await dbContext.SaveChangesAsync();
        
        return Ok(Task.FromResult(new TokenResponse(accessToken, refreshToken.Token)));
    }
    
    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeAsync([FromBody] string refreshTokenRequest)
    {
        var refreshToken = await dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshTokenRequest);

        if (refreshToken is null)
        {
            return NotFound("Token not found.");
        }

        if (refreshToken.IsRevoked)
        {
            return BadRequest("Token already revoked.");
        }

        var userId = userContextService.GetUserId();
        if (userId != null && refreshToken.User.Id != userId)
        {
            return Forbid();
        }

        refreshToken.IsRevoked = true;
        refreshToken.ExpiresAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
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