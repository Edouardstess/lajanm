using CavalierNoir.Domain.Blog;
using CavalierNoir.Domain.Club;
using CavalierNoir.Domain.Communication;
using CavalierNoir.Domain.Finance;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.Learning;
using CavalierNoir.Domain.Media;
using CavalierNoir.Domain.Memberships;
using CavalierNoir.Domain.Tournaments;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Application.Common.Interfaces;

/// <summary>
/// Vue applicative du contexte de persistance. La couche Application dépend de
/// cette abstraction, jamais de <c>ApplicationDbContext</c> : les requêtes de
/// lecture restent composables (LINQ) sans coupler les services à EF Core.
/// </summary>
public interface IApplicationDbContext
{
    // --- Identité et sécurité ---
    DbSet<ApplicationUser> Users { get; }

    DbSet<ApplicationRole> Roles { get; }

    DbSet<IdentityUserRole<int>> UserRoles { get; }

    DbSet<IdentityRoleClaim<int>> RoleClaims { get; }

    DbSet<IdentityUserClaim<int>> UserClaims { get; }

    DbSet<UserPreference> UserPreferences { get; }

    DbSet<LoginHistory> LoginHistory { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DbSet<Notification> Notifications { get; }

    // --- Vie associative ---
    DbSet<Committee> Committees { get; }

    DbSet<CommitteeMember> CommitteeMembers { get; }

    DbSet<Meeting> Meetings { get; }

    DbSet<Document> Documents { get; }

    DbSet<Partner> Partners { get; }

    DbSet<FaqItem> FaqItems { get; }

    DbSet<StaticPage> StaticPages { get; }

    DbSet<ContactMessage> ContactMessages { get; }

    DbSet<SystemSetting> SystemSettings { get; }

    // --- Adhésions et finances ---
    DbSet<MembershipType> MembershipTypes { get; }

    DbSet<MembershipApplication> MembershipApplications { get; }

    DbSet<Membership> Memberships { get; }

    DbSet<Payment> Payments { get; }

    DbSet<Donation> Donations { get; }

    DbSet<Expense> Expenses { get; }

    // --- Compétitions ---
    DbSet<Tournament> Tournaments { get; }

    DbSet<TournamentRegistration> TournamentRegistrations { get; }

    DbSet<TournamentRound> TournamentRounds { get; }

    DbSet<TournamentGame> TournamentGames { get; }

    DbSet<TournamentStanding> TournamentStandings { get; }

    DbSet<EloHistory> EloHistory { get; }

    DbSet<ClubEvent> ClubEvents { get; }

    DbSet<EventRegistration> EventRegistrations { get; }

    // --- Pédagogie ---
    DbSet<ExerciseCollection> ExerciseCollections { get; }

    DbSet<Exercise> Exercises { get; }

    DbSet<ExerciseAttempt> ExerciseAttempts { get; }

    DbSet<UserProgress> UserProgress { get; }

    DbSet<Badge> Badges { get; }

    DbSet<UserBadge> UserBadges { get; }

    DbSet<DailyExercise> DailyExercises { get; }

    DbSet<Course> Courses { get; }

    DbSet<Lesson> Lessons { get; }

    DbSet<TrainingSession> TrainingSessions { get; }

    DbSet<Attendance> Attendances { get; }

    // --- Contenus ---
    DbSet<BlogPost> BlogPosts { get; }

    DbSet<BlogCategory> BlogCategories { get; }

    DbSet<BlogTag> BlogTags { get; }

    DbSet<BlogPostTag> BlogPostTags { get; }

    DbSet<BlogComment> BlogComments { get; }

    DbSet<CommentReport> CommentReports { get; }

    DbSet<Album> Albums { get; }

    DbSet<MediaItem> MediaItems { get; }

    // --- Communication ---
    DbSet<NewsletterSubscriber> NewsletterSubscribers { get; }

    DbSet<NewsletterCampaign> NewsletterCampaigns { get; }

    DbSet<EmailLog> EmailLogs { get; }

    DbSet<ForumCategory> ForumCategories { get; }

    DbSet<ForumTopic> ForumTopics { get; }

    DbSet<ForumPost> ForumPosts { get; }

    DbSet<PrivateMessage> PrivateMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
