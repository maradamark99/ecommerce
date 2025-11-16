using EcommerceLib.Contract;
using EcommerceLib.Exception;
using Microsoft.AspNetCore.Mvc;
using Profile.Contract;
using CustomerDetails = Profile.Data.CustomerDetails;

namespace Profile;

[ApiController]
[Route("api/v{version:apiVersion}/profile")]
public class ProfileController(
    IProfileService profileService,
    IUserContextService userContextService) : ControllerBase
{
    
    [HttpGet]
    [Route("me")]
    public async Task<ActionResult<CustomerDetails>> GetCustomerContactInfoAsync()
    {
        var appUser = userContextService.GetUser() ?? throw new UnauthorizedException("User not authenticated.");
        var customerDetails = await profileService.GetCustomerDetailsAsync(appUser.Id);
        if (customerDetails == null)
        {
            return NoContent();
        }
        return Ok(customerDetails);
    } 
    
    [HttpPut]
    [Route("me")]
    public async Task<IActionResult> UpdateCustomerContactInfoAsync([FromBody] Contract.CustomerDetailsDto profileDetailsDto)
    {
        var appUser = userContextService.GetUser() ?? throw new UnauthorizedException("User not authenticated.");       
        await profileService.SaveCustomerDetailsAsync(appUser, profileDetailsDto);
        return NoContent();
    }
    
    [HttpGet]
    [Route("{customerId}")]
    public async Task<ActionResult<CustomerDetails>> GetCustomerOrderDetailsAsync(string customerId)
    {
        var customerDetails = await profileService.GetCustomerDetailsAsync(customerId);
        if (customerDetails == null)
        {
            return NoContent();
        }
        return Ok(customerDetails);
    }
    
}