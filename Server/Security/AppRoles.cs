namespace FarmerQuest.Server.Security;

/// <summary>
/// Role name constants and helpers for authorization checks
/// </summary>
public static class AppRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";

    // True for Admin or SuperAdmin
    public static bool IsElevated(string role) =>
        role is Admin or SuperAdmin;

    // True if the role is one of the known app roles
    public static bool IsValid(string role) =>
        role is User or Admin or SuperAdmin;
}
