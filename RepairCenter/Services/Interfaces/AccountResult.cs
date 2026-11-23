namespace RepairCenter.Services.Interfaces;
public enum AccountError { None, InvalidCredentials, InactiveAccount, DuplicateIdentity, InvalidRoles, AccountNotFound, InvalidPassword, LastActiveAdmin, TechnicianProfileRequired }
public sealed record AccountResult<T>(T? Value, AccountError Error)
{
    public static AccountResult<T> Success(T value) => new(value, AccountError.None);
    public static AccountResult<T> Failure(AccountError error) => new(default, error);
}
