using Microsoft.AspNetCore.Mvc;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api")]
public class SessionController(ISessionService sessionService) : ControllerBase
{
    // PATCH /api/carts/{cartId}
    [HttpPatch("carts/{cartId}")]
    public async Task<IActionResult> UpdateCartTelemetry(
        string cartId,
        [FromBody] UpdateCartTelemetryRequest request,
        CancellationToken ct)
    {
        try
        {
            await sessionService.UpdateCartTelemetryAsync(cartId, request, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}