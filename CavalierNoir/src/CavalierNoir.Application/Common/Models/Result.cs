namespace CavalierNoir.Application.Common.Models;

/// <summary>
/// Résultat d'une opération applicative. Évite d'utiliser les exceptions pour
/// des échecs attendus (validation, règle métier).
/// </summary>
public class Result
{
    protected Result(bool succeeded, string? error, string? code)
    {
        Succeeded = succeeded;
        Error = error;
        Code = code;
    }

    public bool Succeeded { get; }

    public bool Failed => !Succeeded;

    public string? Error { get; }

    /// <summary>Code de la règle métier ou de l'erreur, par exemple « BR-04 ».</summary>
    public string? Code { get; }

    public static Result Success() => new(true, null, null);

    public static Result Failure(string error, string? code = null) => new(false, error, code);
}

/// <summary>Résultat porteur d'une valeur.</summary>
public sealed class Result<T> : Result
{
    private Result(bool succeeded, T? value, string? error, string? code)
        : base(succeeded, error, code)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, null, null);

    public static new Result<T> Failure(string error, string? code = null) => new(false, default, error, code);
}
