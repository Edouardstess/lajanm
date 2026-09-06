using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Communication;

/// <summary>Message privé entre deux membres.</summary>
public class PrivateMessage : Entity
{
    public int SenderId { get; set; }

    public ApplicationUser Sender { get; set; } = null!;

    public int RecipientId { get; set; }

    public ApplicationUser Recipient { get; set; } = null!;

    public string Subject { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    /// <summary>Suppression côté expéditeur / destinataire, indépendamment l'une de l'autre.</summary>
    public bool DeletedBySender { get; set; }

    public bool DeletedByRecipient { get; set; }

    public int? ParentId { get; set; }

    public PrivateMessage? Parent { get; set; }

    public bool IsRead => ReadAt.HasValue;
}
