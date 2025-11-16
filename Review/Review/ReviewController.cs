using System.Globalization;
using System.Net;
using EcommerceLib;
using EcommerceLib.Contract;
using Microsoft.AspNetCore.Mvc;
using Review.Contract;

namespace Review;

[ApiController]
[Route("api/v{version:apiVersion}/reviews")]
public class ReviewController(
    IReviewService reviewService,
    IUserContextService userContextService) : ControllerBase
{

    [HttpGet]
    [Route("{productId}")]
    public async Task<IActionResult> GetReviewsForProductAsync([FromRoute] string productId)
    {
        var reviews = await reviewService.GetReviewsForProductAsync(productId);
        return Ok(MapToDto(reviews));
    }

    [HttpPost]
    public async Task<IActionResult> CreateReviewAsync([FromBody] ReviewRequestDto request)
    {
        var customer = userContextService.GetUser();
        if (customer == null)
            return Unauthorized();
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var id = await reviewService.CreateReviewAsync(customer.Id, request);
        return new JsonResult(new { id })
        {
            StatusCode = (int)HttpStatusCode.Created
        };
    }

    [HttpDelete]
    [Route("{id:long}")]
    public async Task<IActionResult> DeleteReviewAsync([FromRoute] long id)
    {
        var user = userContextService.GetUser();
        if (user == null)
            return Unauthorized();
        await reviewService.DeleteReviewAsync(id, user);
        return NoContent();
    }

    [HttpPut]
    [Route("{id:long}")]
    public async Task<IActionResult> UpdateReviewAsync([FromRoute] long id, [FromBody] ReviewRequestDto request)
    {
        var customer = userContextService.GetUser();
        if (customer == null)
            return Unauthorized();
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        await reviewService.UpdateReviewAsync(id, customer.Id, request);
        return NoContent();
    }
    
    private static List<ReviewResponseDto> MapToDto(IEnumerable<Model.Review> reviews)
    {
        return [.. reviews
            .Select(x => new ReviewResponseDto()
            {
                Comment = x.Comment,
                UpdatedAt = x.UpdatedAt.ToString() ?? x.CreatedAt.ToString(CultureInfo.InvariantCulture),
                CustomerName = x.Customer.FullName,
                Rating = x.Rating
            })];
    }
    
}