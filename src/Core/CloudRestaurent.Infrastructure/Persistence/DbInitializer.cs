using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Accounting.Domain;
using CloudRestaurent.Modules.Catalog.Domain;
using CloudRestaurent.Domain.Common;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Domain.Tenants;
using CloudRestaurent.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CloudRestaurent.Infrastructure.Persistence;

public sealed class DbInitializer(
    AppDbContext db,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    ILogger<DbInitializer> logger) : IDbInitializer
{
    private static readonly Guid DemoTenantId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DemoCompanyId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DemoBranchId = new("33333333-3333-3333-3333-333333333333");
    private const string AdminEmail = "admin@demo.local";
    private const string AdminPassword = "Admin@12345!";
    private const string SuperAdminEmail = "superadmin@platform.local";
    private const string SuperAdminPassword = "SuperAdmin@123!";

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        await EnsureRolesAsync();
        await EnsureRolePermissionsAsync(ct);
        await EnsureDemoTenantAsync(ct);
        await EnsureSuperAdminUserAsync();
        await EnsureAdminUserAsync();
        await EnsureCatalogSeedAsync(ct);
        await EnsureRestaurantSeedAsync(ct);
        await EnsureChartOfAccountsAsync(ct);

        logger.LogInformation("Database initialization complete.");
    }

    private record SeedAccount(string Code, string Name, AccountClass Class, bool IsCashOrBank = false);

    private static readonly SeedAccount[] DefaultChartOfAccounts =
    [
        // Assets — debit-natured
        new("1000", "Cash",                AccountClass.Asset,   IsCashOrBank: true),
        new("1010", "Bank",                AccountClass.Asset,   IsCashOrBank: true),
        new("1020", "Mobile Wallet",       AccountClass.Asset,   IsCashOrBank: true),
        new("1100", "Accounts Receivable", AccountClass.Asset),
        new("1200", "Inventory",           AccountClass.Asset),
        // Liabilities — credit-natured
        new("2100", "Accounts Payable",    AccountClass.Liability),
        new("2200", "Tax Payable",         AccountClass.Liability),
        // Equity
        new("3000", "Owner's Equity",      AccountClass.Equity),
        // Revenue — credit-natured
        new("4000", "Sales Revenue",       AccountClass.Revenue),
        new("4500", "Discounts Given",     AccountClass.Revenue),  // contra-revenue, debit-natured in practice
        // Expenses — debit-natured
        new("5000", "Cost of Goods Sold",  AccountClass.Expense),
        new("5100", "General Expense",     AccountClass.Expense)
    ];

    private async Task EnsureChartOfAccountsAsync(CancellationToken ct)
    {
        var existingCodes = await db.Accounts.IgnoreQueryFilters()
            .Where(a => a.TenantId == DemoTenantId)
            .Select(a => a.Code).ToListAsync(ct);

        var added = false;
        foreach (var seed in DefaultChartOfAccounts)
        {
            if (existingCodes.Contains(seed.Code)) continue;
            db.Accounts.Add(new Account(
                Guid.NewGuid(), DemoTenantId, seed.Code, seed.Name, seed.Class,
                isSystem: true, isCashOrBank: seed.IsCashOrBank));
            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded default chart of accounts ({Count} accounts).", DefaultChartOfAccounts.Length);
        }
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new AppRole(roleName));
        }
    }

    /// <summary>
    /// Seed missing role↔permission rows from <see cref="AppPermissions.DefaultsByRole"/>.
    /// We only ADD missing rows — never remove — so a tenant admin's runtime edits survive
    /// service restarts.
    /// </summary>
    private async Task EnsureRolePermissionsAsync(CancellationToken ct)
    {
        var existingByRole = await db.RolePermissions.AsNoTracking()
            .GroupBy(rp => rp.RoleId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.Permission).ToHashSet(), ct);

        foreach (var (roleName, perms) in AppPermissions.DefaultsByRole)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;
            var existing = existingByRole.GetValueOrDefault(role.Id) ?? new HashSet<string>();
            foreach (var p in perms)
            {
                if (existing.Contains(p)) continue;
                db.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(), RoleId = role.Id, Permission = p
                });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureDemoTenantAsync(CancellationToken ct)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == DemoTenantId, ct))
            return;

        var tenant = new Tenant(DemoTenantId, "Demo Restaurant", "demo",
            BusinessType.Restaurant, SubscriptionPlan.Premium);
        db.Tenants.Add(tenant);

        var company = new Company(DemoCompanyId, DemoTenantId,
            "Demo Foods", "Demo Foods Pvt Ltd", "PKR");
        db.Companies.Add(company);

        var branch = new Branch(DemoBranchId, DemoTenantId, DemoCompanyId,
            "Main Branch", "MAIN",
            new Location(
                AddressLine1: "123 Main Street",
                AddressLine2: null,
                City: "Karachi",
                State: "Sindh",
                Country: "Pakistan",
                PostalCode: "75500",
                Latitude: 24.8607,
                Longitude: 67.0011,
                TimeZone: "Asia/Karachi"));
        db.Branches.Add(branch);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded Demo tenant {TenantId}", DemoTenantId);
    }

    /// <summary>
    /// Seed the platform-level SuperAdmin. This is a separate account from any tenant
    /// admin — mirrors UltimatePOS's split where superadmin / business-admin are distinct
    /// logins. The user lives inside the demo tenant for FK purposes but their SuperAdmin
    /// role bypasses tenant query filters across the whole platform.
    /// </summary>
    private async Task EnsureSuperAdminUserAsync()
    {
        var superAdmin = await userManager.FindByEmailAsync(SuperAdminEmail);
        if (superAdmin is null)
        {
            superAdmin = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = SuperAdminEmail,
                Email = SuperAdminEmail,
                EmailConfirmed = true,
                FullName = "Platform SuperAdmin",
                TenantId = DemoTenantId,
                IsActive = true
            };
            var result = await userManager.CreateAsync(superAdmin, SuperAdminPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    "Failed to create SuperAdmin user: " + string.Join("; ",
                        result.Errors.Select(e => e.Description)));
            logger.LogInformation("Seeded SuperAdmin user {Email}", SuperAdminEmail);
        }

        if (!await userManager.IsInRoleAsync(superAdmin, AppRoles.SuperAdmin))
            await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);
    }

    private async Task EnsureAdminUserAsync()
    {
        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin is null)
        {
            admin = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                FullName = "Demo Admin",
                TenantId = DemoTenantId,
                IsActive = true
            };
            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    "Failed to create admin user: " + string.Join("; ",
                        result.Errors.Select(e => e.Description)));
            logger.LogInformation("Seeded admin user {Email}", AdminEmail);
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.TenantAdmin))
            await userManager.AddToRoleAsync(admin, AppRoles.TenantAdmin);

        // Migrate older installs: this account used to double as SuperAdmin to keep the
        // seed minimal. Strip that role so the two responsibilities are split — the
        // dedicated superadmin@platform.local account now owns platform powers.
        if (await userManager.IsInRoleAsync(admin, AppRoles.SuperAdmin))
        {
            await userManager.RemoveFromRoleAsync(admin, AppRoles.SuperAdmin);
            logger.LogInformation(
                "Removed SuperAdmin role from {Email} (use {Super} for platform admin)",
                AdminEmail, SuperAdminEmail);
        }
    }

    private record SeedUnitGroup(string Name, (string Code, string Name, decimal Factor)[] Units);

    private static readonly SeedUnitGroup[] DefaultUnitGroups =
    [
        new("Count",  [
            ("PCS", "Piece",  1m),
            ("DOZ", "Dozen",  12m),
            ("PLT", "Plate",  1m)   // restaurant-specific portion unit
        ]),
        new("Mass",   [
            ("GM",  "Gram",     1m),
            ("KG",  "Kilogram", 1000m)
        ]),
        new("Volume", [
            ("ML",  "Millilitre", 1m),
            ("LTR", "Litre",      1000m)
        ])
    ];

    private async Task EnsureCatalogSeedAsync(CancellationToken ct)
    {
        var existingGroups = await db.UnitGroups.IgnoreQueryFilters()
            .Where(g => g.TenantId == DemoTenantId)
            .ToDictionaryAsync(g => g.Name, ct);

        var existingUnitCodes = await db.Units.IgnoreQueryFilters()
            .Where(u => u.TenantId == DemoTenantId)
            .Select(u => u.Code)
            .ToListAsync(ct);

        var anySeedAdded = false;

        foreach (var seedGroup in DefaultUnitGroups)
        {
            if (!existingGroups.TryGetValue(seedGroup.Name, out var group))
            {
                group = new UnitGroup(Guid.NewGuid(), DemoTenantId, seedGroup.Name);
                db.UnitGroups.Add(group);
                existingGroups[seedGroup.Name] = group;
                anySeedAdded = true;
            }

            foreach (var (code, name, factor) in seedGroup.Units)
            {
                if (existingUnitCodes.Contains(code)) continue;
                db.Units.Add(new Unit(Guid.NewGuid(), DemoTenantId, group.Id, code, name, factor));
                anySeedAdded = true;
            }
        }

        var hasCategory = await db.Categories.IgnoreQueryFilters()
            .AnyAsync(c => c.TenantId == DemoTenantId, ct);

        Category? burgers = null;
        if (!hasCategory)
        {
            burgers = new Category(Guid.NewGuid(), DemoTenantId, "Burgers", 0);
            db.Categories.Add(burgers);
            anySeedAdded = true;
        }

        if (anySeedAdded) await db.SaveChangesAsync(ct);

        var hasProduct = await db.Products.IgnoreQueryFilters()
            .AnyAsync(p => p.TenantId == DemoTenantId, ct);
        if (!hasProduct)
        {
            burgers ??= await db.Categories.IgnoreQueryFilters()
                .FirstAsync(c => c.TenantId == DemoTenantId, ct);
            var pcs = await db.Units.IgnoreQueryFilters()
                .FirstAsync(u => u.TenantId == DemoTenantId && u.Code == "PCS", ct);

            db.Products.Add(new Product(
                Guid.NewGuid(), DemoTenantId, burgers.Id, pcs.Id,
                "BUR-001", "Classic Beef Burger",
                new Money(750m, "PKR")));
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded demo catalog (unit groups, units, Burgers category, Classic Beef Burger).");
        }
    }

    private async Task EnsureRestaurantSeedAsync(CancellationToken ct)
    {
        var hasFloorPlan = await db.FloorPlans.IgnoreQueryFilters()
            .AnyAsync(p => p.BranchId == DemoBranchId, ct);
        if (hasFloorPlan) return;

        var mainFloor = new FloorPlan(Guid.NewGuid(), DemoTenantId, DemoBranchId, "Main Floor", 0);
        db.FloorPlans.Add(mainFloor);

        for (var i = 1; i <= 4; i++)
        {
            db.RestaurantTables.Add(new RestaurantTable(
                Guid.NewGuid(), DemoTenantId, mainFloor.Id, DemoBranchId,
                code: $"T-{i}", capacity: 4));
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded demo restaurant tables (Main Floor + T-1..T-4).");
    }
}
