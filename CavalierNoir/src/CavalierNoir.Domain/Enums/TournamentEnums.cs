namespace CavalierNoir.Domain.Enums;

/// <summary>Formule de compétition retenue pour un tournoi.</summary>
public enum TournamentType
{
    SystemeSuisse = 0,
    ToutesRondes = 1,
    EliminationDirecte = 2,
    Match = 3
}

/// <summary>Cycle de vie d'un tournoi (machine à états §9.2 du DAL).</summary>
public enum TournamentStatus
{
    Brouillon = 0,
    InscriptionsOuvertes = 1,
    InscriptionsCloturees = 2,
    EnCours = 3,
    Termine = 4,
    Archive = 5,
    Annule = 6
}

/// <summary>État d'une ronde.</summary>
public enum RoundStatus
{
    Planifiee = 0,
    Apparee = 1,
    EnCours = 2,
    Terminee = 3
}

/// <summary>Issue d'une partie. Les points sont dérivés par <c>ScoreForWhite</c>.</summary>
public enum GameResult
{
    NonJouee = 0,
    VictoireBlancs = 1,
    VictoireNoirs = 2,
    Nulle = 3,
    ForfaitBlancs = 4,
    ForfaitNoirs = 5,
    DoubleForfait = 6,
    Bye = 7
}

/// <summary>Cadence de jeu, utilisée pour l'affichage et le facteur K.</summary>
public enum TimeControl
{
    Classique = 0,
    Rapide = 1,
    Blitz = 2,
    Bullet = 3
}

/// <summary>Nature d'un événement du calendrier du club.</summary>
public enum EventType
{
    Tournoi = 0,
    Stage = 1,
    Simultanee = 2,
    Entrainement = 3,
    AssembleeGenerale = 4,
    ReunionBureau = 5,
    Conference = 6,
    Autre = 7
}

/// <summary>État d'une inscription à un événement ou à un tournoi.</summary>
public enum RegistrationStatus
{
    EnAttente = 0,
    Confirmee = 1,
    ListeAttente = 2,
    Annulee = 3,
    Presente = 4,
    Absente = 5
}
