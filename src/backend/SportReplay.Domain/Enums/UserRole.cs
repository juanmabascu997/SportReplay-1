namespace SportReplay.Domain.Enums;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string ClubOwner = "ClubOwner";
    public const string Operator = "Operator";
    public const string Player = "Player";

    public static readonly string[] All = [Admin, ClubOwner, Operator, Player];
}

public enum UserRole
{
    Admin = 1,
    ClubOwner = 2,
    Operator = 3,
    Player = 4
}
