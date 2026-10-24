using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController(
    ICartService cartService,
    ISessionService sessionService,
    ICartMovementService movements) : ControllerBase
{
    // PATCH: /api/carts/{cartId}
    [HttpPatch("/api/carts/{cartId}")]
    public async Task<IActionResult> UpdateCartTelemetry(
        string cartId,
        [FromBody] UpdateCartTelemetryRequest request,
        [FromHeader(Name = "X-Cart-Token")] string? cartToken,
        CancellationToken ct)
    {
        var result = await sessionService.UpdateCartTelemetryAsync(cartId, request, cartToken, ct);
        return Ok(result);
    }

    // POST: api/cart/session/start
    [HttpPost("session/start")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<CartSummaryResponse>> StartSession(
        [FromBody] LegacyStartSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await sessionService.StartSessionAsync(
            new StartSessionRequest(request.CartId, UserId: request.UserId), cancellationToken);
        return Ok(session);
    }

    // HTTP and SignalR share one movement service and one result contract.
    [HttpPost("{sessionId:guid}/movements")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<CartMovementResult>> ReportMovement(
        Guid sessionId,
        [FromBody] CartMovementRequest request,
        CancellationToken ct)
    {
        return Ok(await movements.ReportAsync(sessionId, request, ct));
    }
    [HttpPost("{sessionId:guid}/exit-confirmation")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<CartMovementResult>> ConfirmExit(
        Guid sessionId, [FromBody] ConfirmExitChoiceRequest request, CancellationToken ct)
    {
        return Ok(await movements.ConfirmExitAsync(sessionId, request, ct));
    }

    // POST: api/cart/{sessionId}/checkout
    [HttpPost("{sessionId:guid}/checkout")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<CheckoutResponseDto>> Checkout(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await cartService.CheckoutSessionAsync(sessionId, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET: api/cart/active/{cartId}
    // GET: /api/carts/{cartId}/session
    [HttpGet("active/{cartId}")]
    [HttpGet("/api/carts/{cartId}/session")]
    [Authorize(Policy = AuthorizationPolicies.CatalogRead)]
    public async Task<ActionResult<CartSummaryResponse>> GetActiveSession(
        string cartId,
        CancellationToken cancellationToken)
    {
        var session = await sessionService.GetActiveSessionByCartIdAsync(cartId, cancellationToken);
        return Ok(session);
    }

    // GET: api/cart/{sessionId}
    [HttpGet("{sessionId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CatalogRead)]
    public async Task<ActionResult<CartSummaryResponse>> GetSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await sessionService.GetSessionSummaryAsync(sessionId, cancellationToken);
        return Ok(session);
    }
}
