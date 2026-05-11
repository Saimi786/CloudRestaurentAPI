using CloudRestaurent.Modules.Accounting.Application.Reports;
using CloudRestaurent.Modules.Inventory.Application.Queries;
using CloudRestaurent.Modules.Sales.Application.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reports")]
public sealed class ReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet("sales-summary")]
    public async Task<ActionResult<SalesSummaryDto>> SalesSummary(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] Guid? branchId,
        CancellationToken ct) =>
        Ok(await mediator.Send(new GetSalesSummaryQuery(from, to, branchId), ct));

    [HttpGet("top-products")]
    public async Task<ActionResult<IReadOnlyList<TopProductRow>>> TopProducts(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] Guid? branchId,
        [FromQuery] int take = 20,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetTopProductsQuery(from, to, branchId, take), ct));

    [HttpGet("stock-valuation")]
    public async Task<ActionResult<StockValuationDto>> StockValuation(
        [FromQuery] Guid? branchId, CancellationToken ct) =>
        Ok(await mediator.Send(new GetStockValuationQuery(branchId), ct));

    [HttpGet("profit-and-loss")]
    public async Task<ActionResult<ProfitAndLossDto>> ProfitAndLoss(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken ct) =>
        Ok(await mediator.Send(new GetProfitAndLossQuery(from, to), ct));

    [HttpGet("tax-summary")]
    public async Task<ActionResult<TaxSummaryDto>> TaxSummary(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] Guid? branchId,
        CancellationToken ct) =>
        Ok(await mediator.Send(new GetTaxSummaryQuery(from, to, branchId), ct));

    [HttpGet("expense-by-category")]
    public async Task<ActionResult<ExpenseByCategoryDto>> ExpenseByCategory(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] Guid? branchId,
        CancellationToken ct) =>
        Ok(await mediator.Send(new GetExpenseByCategoryQuery(from, to, branchId), ct));
}
