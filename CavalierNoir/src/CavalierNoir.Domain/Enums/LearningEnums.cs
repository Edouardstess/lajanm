namespace CavalierNoir.Domain.Enums;

/// <summary>Thème tactique ou stratégique d'un exercice.</summary>
public enum ExerciseTheme
{
    MatEnUn = 0,
    MatEnDeux = 1,
    MatEnTrois = 2,
    Fourchette = 3,
    Clouage = 4,
    Enfilade = 5,
    AttaqueDouble = 6,
    Deviation = 7,
    Attraction = 8,
    Sacrifice = 9,
    Finale = 10,
    Ouverture = 11,
    Strategie = 12,
    Defense = 13,
    Promotion = 14,
    Pat = 15
}

/// <summary>Niveau de difficulté d'un exercice (1 à 5).</summary>
public enum DifficultyLevel
{
    TresFacile = 1,
    Facile = 2,
    Moyen = 3,
    Difficile = 4,
    Expert = 5
}

/// <summary>Niveau pédagogique d'un membre, dérivé de son classement ELO.</summary>
public enum PlayerLevel
{
    Debutant = 0,
    Intermediaire = 1,
    Avance = 2,
    Expert = 3
}

/// <summary>Présence à une séance d'entraînement.</summary>
public enum AttendanceStatus
{
    Present = 0,
    Absent = 1,
    Excuse = 2,
    Retard = 3
}

/// <summary>Critère automatique d'attribution d'un badge.</summary>
public enum BadgeCriterion
{
    Manuel = 0,
    ExercicesResolus = 1,
    SerieQuotidienne = 2,
    TournoisJoues = 3,
    PartiesGagnees = 4,
    EloAtteint = 5,
    AncienneteAdhesion = 6,
    ArticlesPublies = 7
}
