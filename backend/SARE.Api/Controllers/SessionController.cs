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
            var result = await sessionService.UpdateCartTelemetryAsync(cartId, request, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // POST /api/sessions
    [HttpPost("sessions")]
    public async Task<IActionResult> StartSession(
        [FromBody] StartSessionRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await sessionService.StartSessionAsync(request, ct);
            return Created($"/api/sessions/{result.SessionId}", result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/sessions/{id}
    [HttpGet("sessions/{id:guid}")]
    public async Task<IActionResult> GetSessionSummary(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await sessionService.GetSessionSummaryAsync(id, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET /api/carts/{cartId}/session
    [HttpGet("carts/{cartId}/session")]
    public async Task<IActionResult> GetActiveSessionByCart(string cartId, CancellationToken ct)
    {
        try
        {
            var result = await sessionService.GetActiveSessionByCartIdAsync(cartId, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // POST /api/sessions/{id:guid}/close
    [HttpPost("sessions/{id:guid}/close")]
    public async Task<IActionResult> CloseSession(
        Guid id,
        [FromBody] CloseSessionRequest? request,
        CancellationToken ct)
    {
        try
        {
            var result = await sessionService.CloseSessionAsync(id, request, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}