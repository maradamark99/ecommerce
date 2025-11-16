using Ecommerce.Common.Data;
using Ecommerce.Domain.Auth;
using Ecommerce.Domain.Auth.Contract;
using Ecommerce.Domain.Profile.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Domain.Profile;

[ApiController]
[Route("api/v{version:apiVersion}/profile/me")]
public class ProfileController(
    IProfileService profileService,
    IUserContextService userContextService) : ControllerBase
{
    
    [HttpGet]
    [Route("")]
    [Authorize(Roles = nameof(Roles.Customer))]
    public async Task<ActionResult<CustomerDetails>> GetCustomerProfileAsync() 
    {
        var customerId = userContextService.GetUserId() ?? throw new UnauthorizedAccessException("User is not authenticated.");
        return Ok(await profileService.GetCustomerDetailsAsync(customerId));
    }
    
    [HttpPut]
    [Route("")]
    [Authorize(Roles = nameof(Roles.Customer))]
    public async Task<IActionResult> UpdateCustomerProfileAsync([FromBody] ProfileCustomerDetailsDto profileDetailsDto)
    {
        var customerId = userContextService.GetUserId() ?? throw new UnauthorizedAccessException("User is not authenticated.");
        await profileService.SaveCustomerDetailsAsync(customerId, profileDetailsDto);
        return NoContent();
    }
    
}