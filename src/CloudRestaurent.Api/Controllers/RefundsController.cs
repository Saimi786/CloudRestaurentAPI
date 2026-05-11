using CloudRestaurent.Modules.Sales.Application.Refunds;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/orders/{orderId:guid}/refunds")]
public sealed class RefundsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RefundDto>>> List(Guid orderId, CancellationToken ct) =>
        Ok(await mediator.Send(new GetRefundsForOrderQuery(orderId), ct));

    [HttpPost]
    public async Task<ActionResult<RefundDto>> Create(
        Guid orderId, [FromBody] RefundOrderBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new RefundOrderCommand(
            orderId, body.Amount, body.Method, body.Reason, body.Lines), ct));

    public sealed record RefundOrderBody(
        decimal Amount,
        CloudRestaurent.Modules.Sales.Domain.PaymentMethod Method,
        string? Reason,
        IReadOnlyList<RefundLineInput>? Lines);
}
