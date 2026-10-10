using Microsoft.AspNetCore.Mvc;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController(ICartService cartService, ISessionService sessionService) : ControllerBase
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

    // GET: /api/carts/{cartId}/session
    [HttpGet("/api/carts/{cartId}/session")]
    public async Task<IActionResult> GetActiveSessionByCart(string cartId, CancellationToken ct)
    {
        var result = await sessionService.GetActiveSessionByCartIdAsync(cartId, ct);
        return Ok(result);
    }

    // POST: api/cart/session/start
    [HttpPost("session/start")]
    public async Task<ActionResult<CartSessionResponseDto>> StartSession(
        [FromBody] LegacyStartSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await cartService.StartSessionAsync(request, cancellationToken);
        return Ok(session);
    }

    // POST: api/cart/{sessionId}/items
    [HttpPost("{sessionId:guid}/items")]
    public async Task<ActionResult<CartItemProcessResult>> AddItem(
        Guid sessionId,
        [FromBody] CartAddItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await cartService.ProcessItemDetectionAsync(sessionId, request, cancellationToken);

        if (!result.Success && result.IsWeightMismatch)
        {
            return BadRequest(result);
        }

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    // POST: api/cart/{sessionId}/checkout
    [HttpPost("{sessionId:guid}/checkout")]
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
    [HttpGet("active/{cartId}")]
    public async Task<ActionResult<CartSessionResponseDto>> GetActiveSession(
        string cartId,
        CancellationToken cancellationToken)
    {
        var session = await cartService.GetActiveSessionByCartIdAsync(cartId, cancellationToken);
        if (session == null) return NotFound(new { message = $"No active shopping session on cart '{cartId}'" });

        return Ok(session);
    }

    // GET: api/cart/{sessionId}
    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<CartSessionResponseDto>> GetSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await cartService.GetSessionByIdAsync(sessionId, cancellationToken);
        if (session == null) return NotFound(new { message = "Session not found" });

        return Ok(session);
    }
}
