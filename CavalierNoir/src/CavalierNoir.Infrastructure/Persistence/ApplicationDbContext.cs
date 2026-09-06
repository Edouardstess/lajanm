using System.Reflection;
using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Domain.Abstractions;
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
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Infrastructure.Persistence;

/// <summary>
/// Contexte de persistance unique du système. Étend
/// <see cref="IdentityDbContext{TUser,TRole,TKey}"/> pour héberger les tables
/// d'ASP.NET Core Identity aux côtés du modèle métier, et implémente
/// <see cref="IApplicationDbContext"/> que consomme la couche Application.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, int>(options), IApplicationDbContext, IUnitOfWork
{
    // --- Vie associative ---
    public DbSet<Committee> Committees => Set<Committee>();

    public DbSet<CommitteeMember> CommitteeMembers => Set<CommitteeMember>();

    public DbSet<Meeting> Meetings => Set<Meeting>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<Partner> Partners => Set<Partner>();

    public DbSet<FaqItem> FaqItems => Set<FaqItem>();

    public DbSet<StaticPage> StaticPages => Set<StaticPage>();

    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    // --- Identité complémentaire ---
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    public DbSet<LoginHistory> LoginHistory => Set<LoginHistory>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Notification> Notifications => Set<Notification>();

    // --- Adhésions et finances ---
    public DbSet<MembershipType> MembershipTypes => Set<MembershipType>();

    public DbSet<MembershipApplication> MembershipApplications => Set<MembershipApplication>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Donation> Donations => Set<Donation>();

    public DbSet<Expense> Expenses => Set<Expense>();

    // --- Compétitions ---
    public DbSet<Tournament> Tournaments => Set<Tournament>();

    public DbSet<TournamentRegistration> TournamentRegistrations => Set<TournamentRegistration>();

    public DbSet<TournamentRound> TournamentRounds => Set<TournamentRound>();

    public DbSet<TournamentGame> TournamentGames => Set<TournamentGame>();

    public DbSet<TournamentStanding> TournamentStandings => Set<TournamentStanding>();

    public DbSet<EloHistory> EloHistory => Set<EloHistory>();

    public DbSet<ClubEvent> ClubEvents => Set<ClubEvent>();

    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();

    // --- Pédagogie ---
    public DbSet<ExerciseCollection> ExerciseCollections => Set<ExerciseCollection>();

    public DbSet<Exercise> Exercises => Set<Exercise>();

    public DbSet<ExerciseAttempt> ExerciseAttempts => Set<ExerciseAttempt>();

    public DbSet<UserProgress> UserProgress => Set<UserProgress>();

    public DbSet<Badge> Badges => Set<Badge>();

    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    public DbSet<DailyExercise> DailyExercises => Set<DailyExercise>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();

    public DbSet<Attendance> Attendances => Set<Attendance>();

    // --- Contenus ---
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();

    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();

    public DbSet<BlogTag> BlogTags => Set<BlogTag>();

    public DbSet<BlogPostTag> BlogPostTags => Set<BlogPostTag>();

    public DbSet<BlogComment> BlogComments => Set<BlogComment>();

    public DbSet<CommentReport> CommentReports => Set<CommentReport>();

    public DbSet<Album> Albums => Set<Album>();

    public DbSet<MediaItem> MediaItems => Set<MediaItem>();

    // --- Communication ---
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();

    public DbSet<NewsletterCampaign> NewsletterCampaigns => Set<NewsletterCampaign>();

    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    public DbSet<ForumCategory> ForumCategories => Set<ForumCategory>();

    public DbSet<ForumTopic> ForumTopics => Set<ForumTopic>();

    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();

    public DbSet<PrivateMessage> PrivateMessages => Set<PrivateMessage>();

    // Users, Roles, UserRoles, UserClaims et RoleClaims proviennent d'IdentityDbContext
    // et satisfont directement les membres correspondants d'IApplicationDbContext.

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Tables d'Identity renommées en français pour rester cohérentes avec le reste du schéma.
        builder.Entity<ApplicationUser>().ToTable("Utilisateurs");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<int>>().ToTable("UtilisateursRoles");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UtilisateursClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UtilisateursConnexionsExternes");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RolesClaims");
        builder.Entity<IdentityUserToken<int>>().ToTable("UtilisateursJetons");
    }
}
