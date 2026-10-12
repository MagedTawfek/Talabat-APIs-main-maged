namespace Talabat.Core.Models.Identity;

public static class RolePermission
{
    public const string ClaimType = "permission";

    public const string ProductsCreate = "products.create";
    public const string RolesManage = "roles.manage";
    public const string AccountEmailLookup = "accounts.email.lookup";
    public const string BasketRead = "basket.read";
    public const string BasketWrite = "basket.write";
    public const string BasketDelete = "basket.delete";
    public const string OrdersCreate = "orders.create";
    public const string OrdersRead = "orders.read";
}
