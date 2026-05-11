namespace CloudRestaurent.Infrastructure.Identity;

/// <summary>
/// Flat permission catalog. Roles map to subsets via RolePermission; endpoints can
/// require a specific permission via [HasPermission(...)] in addition to (or instead of)
/// role checks. Format is "Area.Action" — keep stable; renames break seeded mappings.
///
/// Layout mirrors UltimatePOS / Blocks360 breadth: every screen-level action a tenant
/// admin would plausibly want to grant/revoke gets its own permission. The admin UI
/// groups them automatically by the part before the dot.
/// </summary>
public static class AppPermissions
{
    // ============================================================
    // CATALOG  — products, categories, recipes, modifiers, pricing
    // ============================================================
    public const string CatalogRead                 = "Catalog.Read";
    public const string CatalogManageProducts       = "Catalog.ManageProducts";
    public const string CatalogManageCategories     = "Catalog.ManageCategories";
    public const string CatalogManageBrands         = "Catalog.ManageBrands";
    public const string CatalogManageUnits          = "Catalog.ManageUnits";
    public const string CatalogManagePricing        = "Catalog.ManagePricing";
    public const string CatalogManageRecipes        = "Catalog.ManageRecipes";
    public const string CatalogManageModifiers      = "Catalog.ManageModifiers";
    public const string CatalogManageCombos         = "Catalog.ManageCombos";
    public const string CatalogManageMixMatch       = "Catalog.ManageMixMatch";
    public const string CatalogManagePromotions     = "Catalog.ManagePromotions";
    public const string CatalogToggleAvailability   = "Catalog.ToggleAvailability"; // 86 list
    public const string CatalogImport               = "Catalog.Import";
    public const string CatalogExport               = "Catalog.Export";
    public const string CatalogPrintBarcodes        = "Catalog.PrintBarcodes";

    // ============================================================
    // SALES / POS
    // ============================================================
    public const string SalesOpenOrder              = "Sales.OpenOrder";
    public const string SalesEditOrder              = "Sales.EditOrder";
    public const string SalesAddPayment             = "Sales.AddPayment";
    public const string SalesApplyDiscount          = "Sales.ApplyDiscount";
    public const string SalesOverridePrice          = "Sales.OverridePrice";
    public const string SalesVoidOrder              = "Sales.VoidOrder";
    public const string SalesIssueRefund            = "Sales.IssueRefund";
    public const string SalesIssuePartialRefund     = "Sales.IssuePartialRefund";
    public const string SalesReprintReceipt         = "Sales.ReprintReceipt";
    public const string SalesViewOwn                = "Sales.ViewOwn";
    public const string SalesViewAll                = "Sales.ViewAll";
    public const string SalesManageCoupons          = "Sales.ManageCoupons";
    public const string SalesRedeemRewardPoints     = "Sales.RedeemRewardPoints";

    // ============================================================
    // KITCHEN / KDS
    // ============================================================
    public const string KitchenViewTickets          = "Kitchen.ViewTickets";
    public const string KitchenAdvanceTickets       = "Kitchen.AdvanceTickets";
    public const string KitchenBumpStations         = "Kitchen.BumpStations";
    public const string KitchenFireTicket           = "Kitchen.FireTicket";
    public const string KitchenRecallServed         = "Kitchen.RecallServed";
    public const string KitchenManageStations       = "Kitchen.ManageStations";
    public const string KitchenPrintTicket          = "Kitchen.PrintTicket";

    // ============================================================
    // INVENTORY
    // ============================================================
    public const string InventoryRead               = "Inventory.Read";
    public const string InventoryRecordMovement     = "Inventory.RecordMovement";
    public const string InventoryAdjustStock        = "Inventory.AdjustStock";
    public const string InventoryTransfer           = "Inventory.Transfer";
    public const string InventoryRecordWaste        = "Inventory.RecordWaste";
    public const string InventoryStockTake          = "Inventory.StockTake";
    public const string InventorySetReorderLevel    = "Inventory.SetReorderLevel";
    public const string InventoryViewStockValue     = "Inventory.ViewStockValue";
    public const string InventoryManageWarehouses   = "Inventory.ManageWarehouses";
    public const string InventoryViewReports        = "Inventory.ViewReports";

    // ============================================================
    // PURCHASING
    // ============================================================
    public const string PurchasingCreatePO          = "Purchasing.CreatePO";
    public const string PurchasingApprovePO         = "Purchasing.ApprovePO";
    public const string PurchasingSendPO            = "Purchasing.SendPO";
    public const string PurchasingReceiveGRN        = "Purchasing.ReceiveGRN";
    public const string PurchasingPayBill           = "Purchasing.PayBill";
    public const string PurchasingMatchBill         = "Purchasing.MatchBill";
    public const string PurchasingManageSuppliers   = "Purchasing.ManageSuppliers";
    public const string PurchasingViewReports       = "Purchasing.ViewReports";

    // ============================================================
    // ACCOUNTING / FINANCE
    // ============================================================
    public const string AccountingViewLedger        = "Accounting.ViewLedger";
    public const string AccountingRecordExpense     = "Accounting.RecordExpense";
    public const string AccountingApproveExpense    = "Accounting.ApproveExpense";
    public const string AccountingManageAccounts    = "Accounting.ManageAccounts";
    public const string AccountingManageJournals    = "Accounting.ManageJournals";
    public const string AccountingReconcileBank     = "Accounting.ReconcileBank";
    public const string AccountingManageBudgets     = "Accounting.ManageBudgets";
    public const string AccountingManageTaxFiling   = "Accounting.ManageTaxFiling";
    public const string AccountingViewProfitLoss    = "Accounting.ViewProfitLoss";
    public const string AccountingViewBalanceSheet  = "Accounting.ViewBalanceSheet";
    public const string AccountingViewCashFlow      = "Accounting.ViewCashFlow";

    // ============================================================
    // CASH REGISTER / SHIFTS
    // ============================================================
    public const string CashOpenShift               = "Cash.OpenShift";
    public const string CashCloseShift              = "Cash.CloseShift";
    public const string CashManageRegisters         = "Cash.ManageRegisters";
    public const string CashViewShiftReport         = "Cash.ViewShiftReport";
    public const string CashApproveVariance         = "Cash.ApproveVariance";
    public const string CashForceCloseShift         = "Cash.ForceCloseShift";
    public const string CashDeposit                 = "Cash.Deposit";
    public const string CashWithdraw                = "Cash.Withdraw";

    // ============================================================
    // RESTAURANT (tables, floor plans, reservations)
    // ============================================================
    public const string RestaurantManageTables      = "Restaurant.ManageTables";
    public const string RestaurantManageFloorPlans  = "Restaurant.ManageFloorPlans";
    public const string RestaurantManageReservations= "Restaurant.ManageReservations";
    public const string RestaurantAssignWaiter      = "Restaurant.AssignWaiter";
    public const string RestaurantSeatGuests        = "Restaurant.SeatGuests";

    // ============================================================
    // CRM / CONTACTS
    // ============================================================
    public const string CrmManageCustomers          = "Crm.ManageCustomers";
    public const string CrmManageCustomerGroups     = "Crm.ManageCustomerGroups";
    public const string CrmViewCustomerHistory      = "Crm.ViewCustomerHistory";
    public const string CrmManageLoyalty            = "Crm.ManageLoyalty";
    public const string CrmManageRewardPoints       = "Crm.ManageRewardPoints";
    public const string CrmManageCommunications     = "Crm.ManageCommunications";
    public const string CrmExportCustomerData       = "Crm.ExportCustomerData";

    // ============================================================
    // REPORTS
    // ============================================================
    public const string ReportsViewDashboard        = "Reports.ViewDashboard";
    public const string ReportsViewSales            = "Reports.ViewSales";
    public const string ReportsViewInventory        = "Reports.ViewInventory";
    public const string ReportsViewPurchase         = "Reports.ViewPurchase";
    public const string ReportsViewCustomer         = "Reports.ViewCustomer";
    public const string ReportsViewEmployee         = "Reports.ViewEmployee";
    public const string ReportsViewProfitLoss       = "Reports.ViewProfitLoss";
    public const string ReportsViewTax              = "Reports.ViewTax";
    public const string ReportsExport               = "Reports.Export";

    // ============================================================
    // HR / EMPLOYEES
    // ============================================================
    public const string HrManageEmployees           = "Hr.ManageEmployees";
    public const string HrManageShifts              = "Hr.ManageShifts";
    public const string HrManageAttendance          = "Hr.ManageAttendance";
    public const string HrManagePayroll             = "Hr.ManagePayroll";
    public const string HrViewEmployeeData          = "Hr.ViewEmployeeData";
    public const string HrApproveLeave              = "Hr.ApproveLeave";

    // ============================================================
    // ADMIN (tenant-level)
    // ============================================================
    public const string AdminManageUsers            = "Admin.ManageUsers";
    public const string AdminManageRoles            = "Admin.ManageRoles";
    public const string AdminManageBranches         = "Admin.ManageBranches";
    public const string AdminManageBusinessSettings = "Admin.ManageBusinessSettings";
    public const string AdminManageTaxes            = "Admin.ManageTaxes";
    public const string AdminManageNotifications    = "Admin.ManageNotifications";
    public const string AdminManageIntegrations     = "Admin.ManageIntegrations";
    public const string AdminManageBackups          = "Admin.ManageBackups";
    public const string AdminViewAuditLog           = "Admin.ViewAuditLog";
    public const string AdminViewReports            = "Admin.ViewReports";
    public const string AdminManageTerminals        = "Admin.ManageTerminals";
    public const string AdminImportData             = "Admin.ImportData";

    // ============================================================
    // PLATFORM (SuperAdmin only)
    // ============================================================
    public const string PlatformManageTenants       = "Platform.ManageTenants";
    public const string PlatformManagePackages      = "Platform.ManagePackages";
    public const string PlatformApproveSubscriptions= "Platform.ApproveSubscriptions";
    public const string PlatformManageBilling       = "Platform.ManageBilling";
    public const string PlatformManageDataRequests  = "Platform.ManageDataRequests";
    public const string PlatformManageCommunicator  = "Platform.ManageCommunicator";
    public const string PlatformAccessAllBusinesses = "Platform.AccessAllBusinesses";
    public const string PlatformViewAuditLog        = "Platform.ViewAuditLog";

    public static readonly string[] All = typeof(AppPermissions)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
        .Select(f => (string)f.GetValue(null)!)
        .ToArray();

    /// <summary>
    /// Default mapping of built-in roles to permissions. Seeded on first run; tenant admins
    /// can edit at runtime via the role-permissions endpoint to add or revoke individual
    /// permissions per role. Adding a new permission constant above + listing it here is
    /// the whole change needed — the DbInitializer auto-grants it on next startup, and
    /// the admin UI surfaces it automatically.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> DefaultsByRole =
        new Dictionary<string, string[]>
        {
            // SuperAdmin: everything. Includes Platform.* which other roles never get.
            [AppRoles.SuperAdmin] = All,

            // TenantAdmin: everything in the tenant's scope (no Platform.*).
            [AppRoles.TenantAdmin] = All
                .Where(p => !p.StartsWith("Platform."))
                .ToArray(),

            // BranchManager: full operational control over one branch but no platform
            // structural management (can't add branches, manage backups, etc).
            [AppRoles.BranchManager] =
            [
                // Catalog
                CatalogRead, CatalogToggleAvailability, CatalogManagePromotions,
                // Sales / POS
                SalesOpenOrder, SalesEditOrder, SalesAddPayment, SalesApplyDiscount,
                SalesOverridePrice, SalesVoidOrder, SalesIssueRefund, SalesIssuePartialRefund,
                SalesReprintReceipt, SalesViewAll, SalesRedeemRewardPoints,
                // Kitchen
                KitchenViewTickets, KitchenAdvanceTickets, KitchenBumpStations,
                KitchenFireTicket, KitchenRecallServed, KitchenPrintTicket,
                // Inventory
                InventoryRead, InventoryRecordMovement, InventoryAdjustStock,
                InventoryTransfer, InventoryRecordWaste, InventoryStockTake,
                InventoryViewStockValue, InventoryViewReports,
                // Purchasing
                PurchasingCreatePO, PurchasingApprovePO, PurchasingSendPO,
                PurchasingReceiveGRN, PurchasingPayBill, PurchasingMatchBill,
                PurchasingManageSuppliers, PurchasingViewReports,
                // Accounting
                AccountingRecordExpense, AccountingApproveExpense, AccountingViewLedger,
                // Cash
                CashOpenShift, CashCloseShift, CashViewShiftReport, CashApproveVariance,
                CashForceCloseShift, CashDeposit, CashWithdraw,
                // Restaurant
                RestaurantManageTables, RestaurantManageReservations,
                RestaurantAssignWaiter, RestaurantSeatGuests,
                // CRM
                CrmManageCustomers, CrmManageCustomerGroups, CrmViewCustomerHistory,
                CrmManageRewardPoints, CrmManageCommunications,
                // Reports
                ReportsViewDashboard, ReportsViewSales, ReportsViewInventory,
                ReportsViewCustomer, ReportsViewEmployee, ReportsViewProfitLoss,
                ReportsExport,
                // HR
                HrManageShifts, HrManageAttendance, HrViewEmployeeData, HrApproveLeave,
                // Admin
                AdminViewReports, AdminViewAuditLog,
            ],

            // Cashier: just the POS hot path.
            [AppRoles.Cashier] =
            [
                CatalogRead,
                SalesOpenOrder, SalesAddPayment, SalesViewOwn, SalesReprintReceipt,
                SalesRedeemRewardPoints,
                KitchenViewTickets,
                CashOpenShift, CashCloseShift,
                RestaurantSeatGuests,
                CrmManageCustomers, CrmViewCustomerHistory,
            ],

            // KitchenStaff: KDS-only.
            [AppRoles.KitchenStaff] =
            [
                CatalogRead, CatalogToggleAvailability,
                KitchenViewTickets, KitchenAdvanceTickets, KitchenBumpStations,
                KitchenFireTicket, KitchenPrintTicket,
            ],

            // Waiter: open orders + seat guests; pass to kitchen.
            [AppRoles.Waiter] =
            [
                CatalogRead,
                SalesOpenOrder, SalesEditOrder, SalesViewOwn,
                KitchenViewTickets,
                RestaurantSeatGuests, RestaurantAssignWaiter,
                CrmManageCustomers, CrmViewCustomerHistory,
            ],

            // InventoryManager: stockroom-only.
            [AppRoles.InventoryManager] =
            [
                CatalogRead,
                InventoryRead, InventoryRecordMovement, InventoryAdjustStock,
                InventoryTransfer, InventoryRecordWaste, InventoryStockTake,
                InventorySetReorderLevel, InventoryViewStockValue,
                InventoryManageWarehouses, InventoryViewReports,
                PurchasingCreatePO, PurchasingReceiveGRN, PurchasingManageSuppliers,
                ReportsViewInventory, ReportsViewPurchase,
            ]
        };
}
