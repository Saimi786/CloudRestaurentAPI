namespace CloudRestaurent.Infrastructure.Identity;

/// <summary>
/// Flat permission catalog. Roles map to subsets via RolePermission; endpoints can
/// require a specific permission via [HasPermission(...)] in addition to (or instead of)
/// role checks. Format is "Area.Action" — keep stable; renames break seeded mappings.
/// </summary>
public static class AppPermissions
{
    // Catalog
    public const string CatalogRead = "Catalog.Read";
    public const string CatalogManageProducts = "Catalog.ManageProducts";
    public const string CatalogManageCategories = "Catalog.ManageCategories";
    public const string CatalogManagePricing = "Catalog.ManagePricing";
    public const string CatalogManageRecipes = "Catalog.ManageRecipes";
    public const string CatalogToggleAvailability = "Catalog.ToggleAvailability";  // 86 list

    // Sales / POS
    public const string SalesOpenOrder = "Sales.OpenOrder";
    public const string SalesAddPayment = "Sales.AddPayment";
    public const string SalesApplyDiscount = "Sales.ApplyDiscount";
    public const string SalesVoidOrder = "Sales.VoidOrder";
    public const string SalesIssueRefund = "Sales.IssueRefund";

    // Kitchen
    public const string KitchenAdvanceTickets = "Kitchen.AdvanceTickets";
    public const string KitchenBumpStations = "Kitchen.BumpStations";

    // Inventory
    public const string InventoryRead = "Inventory.Read";
    public const string InventoryRecordMovement = "Inventory.RecordMovement";
    public const string InventoryTransfer = "Inventory.Transfer";
    public const string InventoryRecordWaste = "Inventory.RecordWaste";

    // Purchasing
    public const string PurchasingCreatePO = "Purchasing.CreatePO";
    public const string PurchasingSendPO = "Purchasing.SendPO";
    public const string PurchasingReceiveGRN = "Purchasing.ReceiveGRN";
    public const string PurchasingPayBill = "Purchasing.PayBill";
    public const string PurchasingMatchBill = "Purchasing.MatchBill";

    // Accounting
    public const string AccountingViewLedger = "Accounting.ViewLedger";
    public const string AccountingRecordExpense = "Accounting.RecordExpense";

    // Cash registers
    public const string CashOpenShift = "Cash.OpenShift";
    public const string CashCloseShift = "Cash.CloseShift";
    public const string CashManageRegisters = "Cash.ManageRegisters";

    // Tenant admin
    public const string AdminManageUsers = "Admin.ManageUsers";
    public const string AdminManageRoles = "Admin.ManageRoles";
    public const string AdminManageBranches = "Admin.ManageBranches";
    public const string AdminViewReports = "Admin.ViewReports";

    // Platform (SuperAdmin only)
    public const string PlatformManageTenants = "Platform.ManageTenants";
    public const string PlatformManagePackages = "Platform.ManagePackages";
    public const string PlatformApproveSubscriptions = "Platform.ApproveSubscriptions";

    public static readonly string[] All = typeof(AppPermissions)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
        .Select(f => (string)f.GetValue(null)!)
        .ToArray();

    /// <summary>
    /// Default mapping of built-in roles to permissions. Seeded on first run; tenant admins
    /// can edit via the role-permissions endpoint.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> DefaultsByRole =
        new Dictionary<string, string[]>
        {
            [AppRoles.SuperAdmin] = All,
            [AppRoles.TenantAdmin] =
            [
                CatalogRead, CatalogManageProducts, CatalogManageCategories,
                CatalogManagePricing, CatalogManageRecipes, CatalogToggleAvailability,
                SalesOpenOrder, SalesAddPayment, SalesApplyDiscount, SalesVoidOrder, SalesIssueRefund,
                KitchenAdvanceTickets, KitchenBumpStations,
                InventoryRead, InventoryRecordMovement, InventoryTransfer, InventoryRecordWaste,
                PurchasingCreatePO, PurchasingSendPO, PurchasingReceiveGRN, PurchasingPayBill, PurchasingMatchBill,
                AccountingViewLedger, AccountingRecordExpense,
                CashOpenShift, CashCloseShift, CashManageRegisters,
                AdminManageUsers, AdminManageRoles, AdminManageBranches, AdminViewReports
            ],
            [AppRoles.BranchManager] =
            [
                CatalogRead, CatalogToggleAvailability,
                SalesOpenOrder, SalesAddPayment, SalesApplyDiscount, SalesVoidOrder, SalesIssueRefund,
                KitchenAdvanceTickets, KitchenBumpStations,
                InventoryRead, InventoryRecordMovement, InventoryTransfer, InventoryRecordWaste,
                PurchasingCreatePO, PurchasingSendPO, PurchasingReceiveGRN, PurchasingPayBill, PurchasingMatchBill,
                AccountingRecordExpense,
                CashOpenShift, CashCloseShift,
                AdminViewReports
            ],
            [AppRoles.Cashier] =
            [
                CatalogRead,
                SalesOpenOrder, SalesAddPayment,
                CashOpenShift, CashCloseShift
            ],
            [AppRoles.KitchenStaff] =
            [
                CatalogRead, CatalogToggleAvailability,
                KitchenAdvanceTickets, KitchenBumpStations
            ],
            [AppRoles.Waiter] =
            [
                CatalogRead,
                SalesOpenOrder
            ],
            [AppRoles.InventoryManager] =
            [
                CatalogRead,
                InventoryRead, InventoryRecordMovement, InventoryTransfer, InventoryRecordWaste,
                PurchasingCreatePO, PurchasingReceiveGRN
            ]
        };
}
