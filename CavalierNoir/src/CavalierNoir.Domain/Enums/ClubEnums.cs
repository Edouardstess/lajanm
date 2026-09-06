namespace CavalierNoir.Domain.Enums;

/// <summary>Poste occupé au sein du bureau exécutif.</summary>
public enum CommitteePosition
{
    President = 0,
    VicePresident = 1,
    SecretaireGeneral = 2,
    SecretaireAdjoint = 3,
    Tresorier = 4,
    TresorierAdjoint = 5,
    ResponsableCommunication = 6,
    ResponsablePedagogique = 7,
    OrganisateurTournois = 8,
    Arbitre = 9,
    MembreDuBureau = 10
}

/// <summary>Nature d'une réunion statutaire.</summary>
public enum MeetingType
{
    AssembleeGeneraleOrdinaire = 0,
    AssembleeGeneraleExtraordinaire = 1,
    ReunionBureau = 2,
    CommissionTechnique = 3,
    Autre = 4
}

/// <summary>État d'une réunion et de son procès-verbal.</summary>
public enum MeetingStatus
{
    Planifiee = 0,
    Tenue = 1,
    PvRedige = 2,
    PvValide = 3,
    Annulee = 4
}

/// <summary>Niveau de partenariat d'un sponsor.</summary>
public enum PartnerLevel
{
    Institutionnel = 0,
    Or = 1,
    Argent = 2,
    Bronze = 3,
    Soutien = 4
}

/// <summary>Niveau de sensibilité d'une donnée (classification §6 du référentiel v6.0).</summary>
public enum DataClassification
{
    Publique = 0,
    Interne = 1,
    Confidentielle = 2,
    Secrete = 3,
    Critique = 4
}
