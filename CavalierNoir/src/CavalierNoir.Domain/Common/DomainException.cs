namespace CavalierNoir.Domain.Common;

/// <summary>
/// Violation d'une règle de gestion (BR-xx). Traduite en HTTP 422 par la couche web.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }

    /// <summary>Identifiant de la règle de gestion violée, par exemple « BR-04 ».</summary>
    public string? Code { get; }
}
