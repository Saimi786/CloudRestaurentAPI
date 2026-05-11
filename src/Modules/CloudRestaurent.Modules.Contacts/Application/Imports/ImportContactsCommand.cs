using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Application.Common.Imports;
using CloudRestaurent.Domain.Common;
using CloudRestaurent.Modules.Contacts.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.Imports;

/// <summary>
/// Bulk-import customers OR suppliers from CSV.
/// Customer columns:  FullName,Phone,Email,CustomerGroup,OpeningBalance,Notes
/// Supplier columns:  FullName,Phone,Email,SupplierBusinessName,TaxNumber,OpeningBalance,Notes
/// One handler — discriminated by command's ContactType. Avoids two near-identical files.
/// </summary>
public sealed record ImportContactsCommand(string CsvContent, ContactType Type) : IRequest<ImportResultDto>;

public sealed class ImportContactsValidator : AbstractValidator<ImportContactsCommand>
{
    public ImportContactsValidator()
    {
        RuleFor(x => x.CsvContent).NotEmpty();
        RuleFor(x => x.Type).Must(t => t is ContactType.Customer or ContactType.Supplier)
            .WithMessage("Bulk import only supports Customer or Supplier types (not Both).");
    }
}

public sealed class ImportContactsHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<ImportContactsCommand, ImportResultDto>
{
    public async Task<ImportResultDto> Handle(ImportContactsCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var rows = CsvParser.Parse(request.CsvContent);
        if (rows.Count < 2)
            return new ImportResultDto(0, 0, 0,
                [new ImportRowError(0, "header", "CSV must include a header row and at least one data row.")]);

        var header = rows[0].Select(h => h.Trim()).ToList();
        var headerIdx = header
            .Select((h, i) => (h, i))
            .ToDictionary(p => p.h, p => p.i, StringComparer.OrdinalIgnoreCase);

        var requiredColumns = request.Type == ContactType.Supplier
            ? new[] { "FullName", "SupplierBusinessName" }
            : new[] { "FullName" };

        var missing = requiredColumns.Where(c => !headerIdx.ContainsKey(c)).ToList();
        if (missing.Count > 0)
            return new ImportResultDto(rows.Count - 1, 0, rows.Count - 1,
                [new ImportRowError(0, "header", $"Missing required columns: {string.Join(", ", missing)}")]);

        // For phone-uniqueness we pull all existing phones — same reasoning as the product
        // importer: cheaper than per-row queries on a typical bulk-import size of ~1k rows.
        var existingPhones = await db.Set<Customer>().AsNoTracking()
            .Where(c => c.Phone != null)
            .Select(c => c.Phone!)
            .ToListAsync(ct);
        var phoneSet = new HashSet<string>(existingPhones, StringComparer.OrdinalIgnoreCase);

        var groups = await db.Set<CustomerGroup>().AsNoTracking()
            .ToDictionaryAsync(g => g.Name, g => g.Id, StringComparer.OrdinalIgnoreCase, ct);

        var errors = new List<ImportRowError>();
        var imported = 0;
        var data = rows.Skip(1).ToList();

        for (var rowIdx = 0; rowIdx < data.Count; rowIdx++)
        {
            var line = rowIdx + 2;
            var row = data[rowIdx];
            string? Get(string col) => headerIdx.TryGetValue(col, out var i) && i < row.Count
                ? row[i]?.Trim() : null;

            var fullName = Get("FullName");
            if (string.IsNullOrWhiteSpace(fullName))
            { errors.Add(new(line, "FullName", "FullName is required.")); continue; }

            var phone = Get("Phone");
            if (!string.IsNullOrWhiteSpace(phone) && phoneSet.Contains(phone))
            { errors.Add(new(line, "Phone", $"Phone '{phone}' already exists.")); continue; }

            var openingBalance = 0m;
            var openingRaw = Get("OpeningBalance");
            if (!string.IsNullOrWhiteSpace(openingRaw))
            {
                if (!decimal.TryParse(openingRaw, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out openingBalance) || openingBalance < 0)
                { errors.Add(new(line, "OpeningBalance", $"'{openingRaw}' is not a valid non-negative decimal.")); continue; }
            }

            string? supplierBusinessName = null;
            if (request.Type == ContactType.Supplier)
            {
                supplierBusinessName = Get("SupplierBusinessName");
                if (string.IsNullOrWhiteSpace(supplierBusinessName))
                { errors.Add(new(line, "SupplierBusinessName", "Required for suppliers.")); continue; }
            }

            Guid? groupId = null;
            var groupName = Get("CustomerGroup");
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                if (!groups.TryGetValue(groupName, out var gid))
                { errors.Add(new(line, "CustomerGroup", $"Group '{groupName}' not found.")); continue; }
                groupId = gid;
            }

            var customer = new Customer(Guid.NewGuid(), tenantId, fullName,
                string.IsNullOrWhiteSpace(phone) ? null : phone,
                string.IsNullOrWhiteSpace(Get("Email")) ? null : Get("Email"));
            customer.SetType(request.Type);
            customer.SetSupplierBusinessName(supplierBusinessName);
            customer.SetTaxNumber(Get("TaxNumber"));
            customer.SetOpeningBalance(new Money(openingBalance, "PKR"));
            customer.SetCustomerGroup(groupId);
            var notes = Get("Notes");
            if (!string.IsNullOrWhiteSpace(notes))
                customer.Update(customer.FullName, customer.Phone, customer.Email, notes);

            db.Set<Customer>().Add(customer);
            if (!string.IsNullOrWhiteSpace(phone)) phoneSet.Add(phone);
            imported++;
        }

        if (imported > 0) await db.SaveChangesAsync(ct);

        return new ImportResultDto(
            TotalRows: data.Count,
            ImportedRows: imported,
            SkippedRows: data.Count - imported,
            Errors: errors);
    }
}
