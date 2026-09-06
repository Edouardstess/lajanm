using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Application.Dtos;

/// <summary>Formule d'adhésion présentée au public.</summary>
public sealed record MembershipTypeDto(
    int Id,
    string Name,
    string Code,
    string? Description,
    decimal Price,
    string Currency,
    int DurationDays,
    bool RequiresProof);

/// <summary>Données saisies par un candidat lors de sa demande d'adhésion.</summary>
public sealed class MembershipApplicationRequest
{
    public int UserId { get; init; }

    public int MembershipTypeId { get; init; }

    public int DeclaredElo { get; init; } = 1200;

    public string? Motivation { get; init; }

    public string? SupportingDocuments { get; init; }

    public bool ParentalConsent { get; init; }

    public string? ParentContact { get; init; }
}

/// <summary>Ligne du tableau d'instruction du secrétariat.</summary>
public sealed record MembershipApplicationSummary(
    int Id,
    int UserId,
    string ApplicantName,
    string? Email,
    string MembershipTypeName,
    ApplicationStatus Status,
    DateTime? SubmittedAt,
    int DeclaredElo,
    int? AssessedElo,
    bool ParentalConsent);

/// <summary>Vue synthétique d'une adhésion pour l'espace membre.</summary>
public sealed record MembershipSummary(
    int Id,
    string MemberNumber,
    string TypeName,
    MembershipStatus Status,
    DateOnly Start,
    DateOnly End,
    decimal Amount,
    string Currency,
    int DaysRemaining,
    bool NeedsRenewal);
