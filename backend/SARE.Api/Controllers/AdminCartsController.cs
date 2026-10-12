using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Cart;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/admin/carts")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public sealed class AdminCartsController(ICartService service) : ControllerBase
{
    [HttpPost]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<CartRegistrationResponse>> Create(
        [FromBody] CreateCartRequest request, CancellationToken ct)
    {
        var cart = await service.RegisterCartAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, cart);
    }
}
