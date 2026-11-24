namespace RepairCenter.Services.Interfaces;

public enum CatalogChangeRequestError { None, Invalid, NotFound, Conflict }

public sealed record CatalogChangeRequestResult<T>(
    T? Value,
    CatalogChangeRequestError Error,
    string? Title = null,
    string? Detail = null);
