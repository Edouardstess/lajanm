using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Domain.Learning;

/// <summary>Recueil thématique d'exercices, par exemple « Tactique 101 ».</summary>
public class ExerciseCollection : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public PlayerLevel Level { get; set; } = PlayerLevel.Debutant;

    public bool IsPublished { get; set; }

    public int DisplayOrder { get; set; }

    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
}
