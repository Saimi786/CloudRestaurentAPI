using System.Text.Json;
using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Modules;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using CloudRestaurent.Modules.Catalog.Domain;
using CloudRestaurent.Modules.Catalog.Domain.Modifiers;
using CloudRestaurent.Modules.Catalog.Domain.Recipes;
using CloudRestaurent.Domain.Common;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Contacts.Domain;
using CloudRestaurent.Modules.Inventory.Domain;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using CloudRestaurent.Modules.Accounting.Domain;
using CloudRestaurent.Modules.Pricing.Domain;
using CloudRestaurent.Modules.SaaS.Domain;
using CloudRestaurent.Modules.Tax.Domain;
using CloudRestaurent.Domain.Tenants;
using CloudRestaurent.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantContext tenantContext,
    IModuleRegistry moduleRegistry,
    ICurrentUser? currentUser = null,
    IRequestAuditContext? auditContext = null)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), IAppDbContext
{
    private readonly ITenantContext _tenantContext = tenantContext;
    private readonly IModuleRegistry _moduleRegistry = moduleRegistry;
    private readonly ICurrentUser? _currentUser = currentUser;
    private readonly IRequestAuditContext? _auditContext = auditContext;

    /// <summary>
    /// Entities we don't audit — they're either themselves audit infrastructure (would
    /// recurse on save) or pure GL append-only ledger rows that already constitute their
    /// own audit (AccountTransaction). Including them would double the row count of the
    /// audit table for no analytic gain.
    /// </summary>
    private static readonly HashSet<string> ExcludedFromAudit = new(StringComparer.Ordinal)
    {
        nameof(AuditEntry),
        nameof(SyncOperation),
        nameof(IdempotencyRecord)
    };

    private static readonly JsonSerializerOptions AuditJson = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<UnitGroup> UnitGroups => Set<UnitGroup>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ComboComponent> ComboComponents => Set<ComboComponent>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<ModifierGroup> ModifierGroups => Set<ModifierGroup>();
    public DbSet<Modifier> Modifiers => Set<Modifier>();
    public DbSet<ProductModifierGroup> ProductModifierGroups => Set<ProductModifierGroup>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<WasteLog> WasteLogs => Set<WasteLog>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<SupplierBill> SupplierBills => Set<SupplierBill>();
    public DbSet<SupplierBillPayment> SupplierBillPayments => Set<SupplierBillPayment>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<FloorPlan> FloorPlans => Set<FloorPlan>();
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<MixMatchGroup> MixMatchGroups => Set<MixMatchGroup>();
    public DbSet<MixMatchProduct> MixMatchProducts => Set<MixMatchProduct>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<OrderLineModifier> OrderLineModifiers => Set<OrderLineModifier>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashRegisterShift> CashRegisterShifts => Set<CashRegisterShift>();
    public DbSet<CashRegisterShiftMovement> CashRegisterShiftMovements => Set<CashRegisterShiftMovement>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<RefundLine> RefundLines => Set<RefundLine>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<OrderPromotion> OrderPromotions => Set<OrderPromotion>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<ProductAvailabilityWindow> ProductAvailabilityWindows => Set<ProductAvailabilityWindow>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<SyncOperation> SyncOperations => Set<SyncOperation>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<KitchenTicket> KitchenTickets => Set<KitchenTicket>();
    public DbSet<KitchenStation> KitchenStations => Set<KitchenStation>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountTransaction> AccountTransactions => Set<AccountTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Core (Tenant, Company, Branch, AppUser configurations)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Each enabled module contributes its EF configurations.
        foreach (var module in _moduleRegistry.EnabledModules)
            module.ConfigureModel(modelBuilder);

        ApplyTenantQueryFilters(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
                continue;

            var method = typeof(AppDbContext)
                .GetMethod(nameof(SetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, [modelBuilder]);
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantScoped
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<TEntity>().HasIndex(nameof(ITenantScoped.TenantId));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Snapshot audit entries BEFORE the save: ChangeTracker entries may have keys
        // generated by the DB (Guid PKs are generated app-side here, but to be safe we
        // still capture into a list, save, then write audit rows in a second SaveChanges).
        var auditRows = CaptureAuditEntries();
        var result = await base.SaveChangesAsync(cancellationToken);
        if (auditRows.Count > 0)
        {
            // Our entities use Guid PKs assigned in the constructor, so EntityKey is already
            // correct at capture time. If we ever switch to identity columns we'd need to
            // re-resolve here.
            await Set<AuditEntry>().AddRangeAsync(auditRows, cancellationToken);
            await base.SaveChangesAsync(cancellationToken);
        }
        return result;
    }

    private List<AuditEntry> CaptureAuditEntries()
    {
        ChangeTracker.DetectChanges();
        var entries = new List<AuditEntry>();
        var now = DateTimeOffset.UtcNow;
        var userId = _currentUser?.UserId;
        var tenantId = _tenantContext.TenantId;
        var requestPath = _auditContext?.RequestPath;
        var idempotencyKey = _auditContext?.IdempotencyKey;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;
            var typeName = entry.Entity.GetType().Name;
            if (ExcludedFromAudit.Contains(typeName)) continue;

            var key = ResolveKey(entry);
            var (kind, before, after) = entry.State switch
            {
                EntityState.Added => (AuditChangeKind.Added, (string?)null, SerializeCurrentValues(entry)),
                EntityState.Deleted => (AuditChangeKind.Deleted, SerializeOriginalValues(entry), (string?)null),
                _ => (AuditChangeKind.Modified, SerializeChangedOriginal(entry), SerializeChangedCurrent(entry))
            };

            // Skip Modified rows where nothing actually changed (EF over-detects when
            // ChangeTracker is queried mid-handler and properties are re-assigned to same value).
            if (kind == AuditChangeKind.Modified && before == "{}" && after == "{}") continue;

            entries.Add(new AuditEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                OccurredAt = now,
                EntityType = typeName,
                EntityKey = key,
                Kind = kind,
                BeforeJson = before,
                AfterJson = after,
                RequestPath = requestPath,
                IdempotencyKey = idempotencyKey
            });
        }
        return entries;
    }

    private static string ResolveKey(EntityEntry entry)
    {
        var pkProps = entry.Metadata.FindPrimaryKey()?.Properties;
        if (pkProps is null || pkProps.Count == 0) return "(no-key)";
        var parts = pkProps.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "");
        return string.Join("|", parts);
    }

    private static string SerializeCurrentValues(EntityEntry entry)
    {
        var dict = entry.CurrentValues.Properties
            .ToDictionary(p => p.Name, p => entry.CurrentValues[p]);
        return JsonSerializer.Serialize(dict, AuditJson);
    }

    private static string SerializeOriginalValues(EntityEntry entry)
    {
        var dict = entry.OriginalValues.Properties
            .ToDictionary(p => p.Name, p => entry.OriginalValues[p]);
        return JsonSerializer.Serialize(dict, AuditJson);
    }

    private static string SerializeChangedOriginal(EntityEntry entry)
    {
        var dict = entry.Properties
            .Where(p => p.IsModified)
            .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
        return JsonSerializer.Serialize(dict, AuditJson);
    }

    private static string SerializeChangedCurrent(EntityEntry entry)
    {
        var dict = entry.Properties
            .Where(p => p.IsModified)
            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        return JsonSerializer.Serialize(dict, AuditJson);
    }
}
