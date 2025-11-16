using System.Net;
using Ecommerce.Domain.Auth;
using Ecommerce.Domain.Auth.Contract;
using Ecommerce.Domain.Review.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Domain.Review;

[ApiController]
[Route("api/v{version:apiVersion}/reviews")]
public class ReviewController(IReviewService reviewService, IUserContextService userContextService) : ControllerBase
{
    
    [HttpGet]
    [Route("{productId}")]
    public async Task<IActionResult> GetReviewsForProductAsync([FromRoute] string productId)
    {
        var reviews = await reviewService.GetReviewsForProductAsync(productId);
        return Ok(reviews);
    }

    [Authorize(Roles = nameof(Roles.Customer))]
    [HttpPost]
    public async Task<IActionResult> CreateReviewAsync([FromBody] ReviewRequestDto request)
    {
        var customerId = GetUserId();
        if (!ModelState.IsValid) 
        {
            return BadRequest(ModelState);
        }
        var id = await reviewService.CreateReviewAsync(customerId, request);
        return new JsonResult(new { id })
        {
            StatusCode = (int)HttpStatusCode.Created
        };
    }
    
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.Customer)}")]
    [HttpDelete]
    [Route("{id:long}")]
    public async Task<IActionResult> DeleteReviewAsync([FromRoute] long id)
    {
        var userId = GetUserId();
        await reviewService.DeleteReviewAsync(id, userId);
        return NoContent();
    }
    
    [Authorize(Roles = nameof(Roles.Customer))]
    [HttpPut]
    [Route("{id:long}")]
    public async Task<IActionResult> UpdateReviewAsync([FromRoute] long id, [FromBody] ReviewRequestDto request)
    {
        var customerId = GetUserId();
        await reviewService.UpdateReviewAsync(id, customerId, request);
        if (!ModelState.IsValid) 
        {
            return BadRequest(ModelState);
        }
        return NoContent();
    }
    
    private string GetUserId()
    {
        var userId = userContextService.GetUserId();
        if (userId == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }
        return userId;
    }
    
}