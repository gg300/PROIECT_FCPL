namespace MetroREX.Data;

internal static class AppPaths
{
    public static string StorageDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "Storage");

    public static string UsersFile { get; } = Path.Combine(StorageDirectory, "users.json");

    public static string ProductsFile { get; } = Path.Combine(StorageDirectory, "products.xlsx");

    public static string CouponsFile { get; } = Path.Combine(StorageDirectory, "coupons.txt");

    public static string StateFile { get; } = Path.Combine(StorageDirectory, "state.json");

    public static string EmailSettingsFile { get; } = Path.Combine(StorageDirectory, "email.json");
}
