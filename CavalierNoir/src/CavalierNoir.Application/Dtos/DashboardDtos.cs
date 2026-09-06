namespace CavalierNoir.Application.Dtos;

/// <summary>Indicateurs du tableau de bord d'administration.</summary>
public sealed record AdminDashboardStats(
    int TotalUsers,
    int ActiveMembers,
    int PendingApplications,
    int ExpiringMemberships,
    int PublishedExercises,
    int AttemptsThisMonth,
    int UpcomingEvents,
    int RunningTournaments,
    int PendingComments,
    int NewContactMessages,
    decimal RevenueThisYear,
    string Currency,
    IReadOnlyList<ActivityPoint> AttemptsPerDay,
    IReadOnlyList<ActivityPoint> NewMembersPerMonth);

/// <summary>Point d'une série temporelle (graphiques du back-office).</summary>
public sealed record ActivityPoint(string Label, int Value);

/// <summary>Tableau de bord de l'espace membre.</summary>
public sealed record MemberDashboard(
    string DisplayName,
    int Elo,
    string LevelLabel,
    ProgressSummary Progress,
    MembershipSummary? Membership,
    ExerciseCard? DailyExercise,
    bool DailyExerciseSolved,
    IReadOnlyList<TournamentCard> UpcomingTournaments,
    IReadOnlyList<string> Badges,
    int UnreadNotifications);
