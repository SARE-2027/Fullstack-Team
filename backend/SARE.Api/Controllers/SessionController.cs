using Microsoft.AspNetCore.Mvc;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api")]
public class SessionController(ISessionService sessionService) : ControllerBase
{
    // POST /api/sessions
    [HttpPost("sessions")]
    public async Task<IActionResult> StartSession(
        [FromBody] StartSessionRequest request,
        CancellationToken ct)
    {
        var result = await sessionService.StartSessionAsync(request, ct);
        return Created($"/api/sessions/{result.SessionId}", result);
    }

    // GET /api/sessions/{id}
    [HttpGet("sessions/{id:guid}")]
    public async Task<IActionResult> GetSessionSummary(Guid id, CancellationToken ct)
    {
        var result = await sessionService.GetSessionSummaryAsync(id, ct);
        return Ok(result);
    }

    // POST /api/sessions/{id:guid}/close
    [HttpPost("sessions/{id:guid}/close")]
    public async Task<IActionResult> CloseSession(
        Guid id,
        [FromBody] CloseSessionRequest? request,
        CancellationToken ct)
    {
        var result = await sessionService.CloseSessionAsync(id, request, ct);
        return Ok(result);
    }
}