namespace AutoSale.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string InternalSales = "InternalSales";
    public const string CognitoGroupsClaimType = "cognito:groups";
    public const string AdministratorsGroup = "admins";
}
