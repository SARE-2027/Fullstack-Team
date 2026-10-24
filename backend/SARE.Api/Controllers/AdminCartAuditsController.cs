using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/admin/sessions/{sessionId:guid}")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public sealed class AdminCartAuditsController(ICartMovementService movements, ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("staff-request")]
    public async Task<ActionResult<CartMovementResult>> RequestStaff(Guid sessionId, CartAuditCommand request, CancellationToken ct) =>
        Ok(await movements.RequestStaffAsync(sessionId, request, ct));

    [HttpPost("audit/start")]
    public async Task<ActionResult<CartMovementResult>> Start(Guid sessionId, CartAuditCommand request, CancellationToken ct) =>
        Ok(await movements.ManageAuditAsync(sessionId, CartAuditAction.Start,
            currentUser.UserId.GetValueOrDefault(), request, ct));

    [HttpPost("audit/remove-item")]
    public async Task<ActionResult<CartMovementResult>> RemoveItem(Guid sessionId, CartAuditCommand request, CancellationToken ct) =>
        Ok(await movements.ManageAuditAsync(sessionId, CartAuditAction.RemoveItem,
            currentUser.UserId.GetValueOrDefault(), request, ct));

    [HttpPost("audit/commit")]
    public async Task<ActionResult<CartMovementResult>> Commit(Guid sessionId, CartAuditCommand request, CancellationToken ct) =>
        Ok(await movements.ManageAuditAsync(sessionId, CartAuditAction.Commit,
            currentUser.UserId.GetValueOrDefault(), request, ct));
}
