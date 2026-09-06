namespace CavalierNoir.Domain.Enums;

/// <summary>Cycle de vie d'une demande d'adhésion (machine à états §9.1 du DAL).</summary>
public enum ApplicationStatus
{
    Brouillon = 0,
    Soumise = 1,
    EnInstruction = 2,
    ComplementsDemandes = 3,
    Approuvee = 4,
    Rejetee = 5,
    Annulee = 6
}

/// <summary>État d'une adhésion une fois la demande approuvée.</summary>
public enum MembershipStatus
{
    EnAttentePaiement = 0,
    Active = 1,
    DelaiDeGrace = 2,
    Expiree = 3,
    Suspendue = 4,
    Resiliee = 5
}

/// <summary>État d'un règlement (cotisation, inscription tournoi, don).</summary>
public enum PaymentStatus
{
    EnAttente = 0,
    Paye = 1,
    Echoue = 2,
    Rembourse = 3,
    Annule = 4
}

/// <summary>Moyen de paiement accepté par le club.</summary>
public enum PaymentMethod
{
    Especes = 0,
    Virement = 1,
    CarteBancaire = 2,
    MonCash = 3,
    NatCash = 4,
    Cheque = 5,
    Autre = 6
}

/// <summary>Nature d'une dépense pour le suivi budgétaire du trésorier.</summary>
public enum ExpenseCategory
{
    Materiel = 0,
    Location = 1,
    Deplacement = 2,
    Communication = 3,
    Recompenses = 4,
    Hebergement = 5,
    Administratif = 6,
    Autre = 7
}

/// <summary>Objet d'un encaissement.</summary>
public enum PaymentPurpose
{
    Cotisation = 0,
    InscriptionTournoi = 1,
    Don = 2,
    Stage = 3,
    Boutique = 4,
    Autre = 5
}
