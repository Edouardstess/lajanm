namespace CavalierNoir.Domain.Enums;

/// <summary>Cycle de vie éditorial d'un article (machine à états §9.3 du DAL).</summary>
public enum BlogStatus
{
    Brouillon = 0,
    EnRelecture = 1,
    Programme = 2,
    Publie = 3,
    Archive = 4
}

/// <summary>État de modération d'un commentaire.</summary>
public enum CommentStatus
{
    EnAttente = 0,
    Approuve = 1,
    Masque = 2,
    Rejete = 3
}

/// <summary>Motif d'un signalement de commentaire ou de message de forum.</summary>
public enum ReportReason
{
    Spam = 0,
    Insultes = 1,
    HorsSujet = 2,
    ContenuIllegal = 3,
    Harcelement = 4,
    Autre = 5
}

/// <summary>Nature d'un média stocké.</summary>
public enum MediaType
{
    Image = 0,
    Video = 1,
    Document = 2,
    Audio = 3
}

/// <summary>Catégorie documentaire (gestion documentaire du secrétariat).</summary>
public enum DocumentCategoryCode
{
    Juridique = 0,
    Financier = 1,
    Pedagogique = 2,
    ProcesVerbal = 3,
    Rapport = 4,
    Convocation = 5,
    Presse = 6,
    Autre = 7
}

/// <summary>Visibilité d'un document ou d'un espace de contenu.</summary>
public enum Visibility
{
    Public = 0,
    Membres = 1,
    Bureau = 2,
    Prive = 3
}

/// <summary>État d'un sujet de forum.</summary>
public enum TopicStatus
{
    Ouvert = 0,
    Ferme = 1,
    Archive = 2
}

/// <summary>État d'une campagne de newsletter.</summary>
public enum CampaignStatus
{
    Brouillon = 0,
    Programmee = 1,
    EnCoursEnvoi = 2,
    Envoyee = 3,
    Annulee = 4
}

/// <summary>Statut d'un email dans le journal d'envoi.</summary>
public enum EmailStatus
{
    EnAttente = 0,
    Envoye = 1,
    Echoue = 2,
    Ouvert = 3,
    Clique = 4,
    Rejete = 5
}

/// <summary>Canal de notification.</summary>
public enum NotificationChannel
{
    InApp = 0,
    Email = 1,
    Sms = 2,
    Push = 3
}

/// <summary>Traitement d'un message reçu via le formulaire de contact.</summary>
public enum ContactMessageStatus
{
    Nouveau = 0,
    EnCours = 1,
    Traite = 2,
    Spam = 3
}
