using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Memberships;

/// <summary>
/// Demande d'adhésion instruite par le secrétariat puis, pour le niveau de jeu,
/// par le responsable pédagogique (§2.2 du DAL).
/// </summary>
public class MembershipApplication : AuditableEntity, IAggregateRoot
{
    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public int MembershipTypeId { get; set; }

    public MembershipType MembershipType { get; set; } = null!;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Brouillon;

    public DateTime? SubmittedAt { get; set; }

    /// <summary>Niveau déclaré par le candidat, avant évaluation.</summary>
    public int DeclaredElo { get; set; } = 1200;

    /// <summary>Niveau retenu après évaluation par le responsable pédagogique.</summary>
    public int? AssessedElo { get; set; }

    public string? Motivation { get; set; }

    /// <summary>Chemins relatifs des pièces justificatives téléversées, séparés par « ; ».</summary>
    public string? SupportingDocuments { get; set; }

    public bool ParentalConsent { get; set; }

    public string? ParentContact { get; set; }

    // Instruction administrative
    public int? ReviewedById { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewComment { get; set; }

    // Évaluation pédagogique
    public int? AssessedById { get; set; }

    public DateTime? AssessedAt { get; set; }

    public string? RejectionReason { get; set; }

    public int? ResultingMembershipId { get; set; }

    public Membership? ResultingMembership { get; set; }

    // --- Comportement métier ---

    public bool IsPending => Status is ApplicationStatus.Soumise
        or ApplicationStatus.EnInstruction
        or ApplicationStatus.ComplementsDemandes;

    public void Submit(DateTime when)
    {
        if (Status is not (ApplicationStatus.Brouillon or ApplicationStatus.ComplementsDemandes))
        {
            throw new DomainException("Seule une demande en brouillon peut être soumise.");
        }

        Status = ApplicationStatus.Soumise;
        SubmittedAt = when;
        UpdatedAt = when;
    }

    public void StartReview(int reviewerId, DateTime when)
    {
        if (Status != ApplicationStatus.Soumise)
        {
            throw new DomainException("Seule une demande soumise peut être mise en instruction.");
        }

        Status = ApplicationStatus.EnInstruction;
        ReviewedById = reviewerId;
        ReviewedAt = when;
        UpdatedAt = when;
    }

    public void RequestMoreInformation(int reviewerId, string comment, DateTime when)
    {
        if (!IsPending)
        {
            throw new DomainException("La demande n'est plus en cours d'instruction.");
        }

        Status = ApplicationStatus.ComplementsDemandes;
        ReviewedById = reviewerId;
        ReviewedAt = when;
        ReviewComment = comment;
        UpdatedAt = when;
    }

    public void Approve(int reviewerId, int? assessedElo, DateTime when)
    {
        if (!IsPending)
        {
            throw new DomainException("Seule une demande en cours peut être approuvée.");
        }

        Status = ApplicationStatus.Approuvee;
        ReviewedById = reviewerId;
        ReviewedAt = when;
        AssessedElo = assessedElo ?? DeclaredElo;
        AssessedById = reviewerId;
        AssessedAt = when;
        UpdatedAt = when;
    }

    public void Reject(int reviewerId, string reason, DateTime when)
    {
        if (!IsPending)
        {
            throw new DomainException("Seule une demande en cours peut être rejetée.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Un motif de rejet est obligatoire.");
        }

        Status = ApplicationStatus.Rejetee;
        ReviewedById = reviewerId;
        ReviewedAt = when;
        RejectionReason = reason;
        UpdatedAt = when;
    }
}
