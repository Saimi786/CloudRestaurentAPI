namespace CloudRestaurent.Infrastructure.Identity;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string BranchManager = "BranchManager";
    public const string Cashier = "Cashier";
    public const string KitchenStaff = "KitchenStaff";
    public const string Waiter = "Waiter";
    public const string InventoryManager = "InventoryManager";

    public static readonly string[] All =
    [
        SuperAdmin, TenantAdmin, BranchManager, Cashier,
        KitchenStaff, Waiter, InventoryManager
    ];
}
