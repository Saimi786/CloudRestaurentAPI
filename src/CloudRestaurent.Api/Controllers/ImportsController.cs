using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Application.Common.Imports;
using CloudRestaurent.Infrastructure.Identity;
using CloudRestaurent.Modules.Catalog.Application.Imports;
using CloudRestaurent.Modules.Contacts.Application.Imports;
using CloudRestaurent.Modules.Contacts.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

/// <summary>
/// CSV bulk import endpoints. All require TenantAdmin since imports can create
/// hundreds of records at once. The flow is single-shot: upload CSV, get back a
/// row-by-row result with errors. There's no preview-then-commit step in v1 —
/// users iterate by re-uploading a corrected file.
/// </summary>
[ApiController]
[Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
[Route("api/v1/imports")]
public sealed class ImportsController(IMediator mediator) : ControllerBase
{
    [HttpPost("products")]
    [RequestSizeLimit(10_000_000)] // 10 MB cap; ~50k product rows worth
    public async Task<ActionResult<ImportResultDto>> Products(IFormFile file, CancellationToken ct)
    {
        var csv = await ReadCsvAsync(file);
        return Ok(await mediator.Send(new ImportProductsCommand(csv), ct));
    }

    [HttpPost("customers")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ImportResultDto>> Customers(IFormFile file, CancellationToken ct)
    {
        var csv = await ReadCsvAsync(file);
        return Ok(await mediator.Send(new ImportContactsCommand(csv, ContactType.Customer), ct));
    }

    [HttpPost("suppliers")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ImportResultDto>> Suppliers(IFormFile file, CancellationToken ct)
    {
        var csv = await ReadCsvAsync(file);
        return Ok(await mediator.Send(new ImportContactsCommand(csv, ContactType.Supplier), ct));
    }

    private static async Task<string> ReadCsvAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["file"] = ["A CSV file is required."]
            });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".csv" or ".txt"))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["file"] = ["Only .csv files are accepted."]
            });

        using var reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync();
    }
}
