using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Infrastructure.Persistence.SeedData;

/// <summary>
/// Exercice d'amorçage. Les solutions sont exprimées en notation par
/// coordonnées (case de départ + case d'arrivée, par exemple « a1a8 »), la même
/// que produit l'échiquier interactif du site : la correction est ainsi
/// indépendante de la langue de la notation algébrique.
/// </summary>
/// <param name="Title">Intitulé affiché.</param>
/// <param name="Fen">Position de départ.</param>
/// <param name="Solution">Suite de coups attendue.</param>
/// <param name="Theme">Motif tactique.</param>
/// <param name="Difficulty">Difficulté de 1 à 5.</param>
/// <param name="Hint1">Premier indice.</param>
/// <param name="Hint2">Deuxième indice.</param>
/// <param name="Hint3">Troisième indice.</param>
/// <param name="Explanation">Correction détaillée.</param>
/// <param name="IsPublic">Visible des visiteurs non authentifiés.</param>
public sealed record ExerciseSeed(
    string Title,
    string Fen,
    string Solution,
    ExerciseTheme Theme,
    DifficultyLevel Difficulty,
    string Hint1,
    string Hint2,
    string Hint3,
    string Explanation,
    bool IsPublic);

/// <summary>
/// Bibliothèque d'exercices livrée avec l'application. Chaque position a été
/// vérifiée à la main : légalité de la position, idée gagnante unique et
/// exactitude du mat annoncé.
/// </summary>
public static class ExerciseSeedData
{
    public static IReadOnlyList<ExerciseSeed> All { get; } =
    [
        new ExerciseSeed(
            "Le mat du couloir",
            "6k1/5ppp/8/8/8/8/8/R5K1 w - - 0 1",
            "a1a8",
            ExerciseTheme.MatEnUn,
            DifficultyLevel.TresFacile,
            "Le roi noir est enfermé par ses propres pions.",
            "Quelle pièce blanche peut atteindre la huitième rangée ?",
            "La tour n'a qu'un seul coup à jouer.",
            "1. Ta8# — La tour arrive sur la rangée du roi. Les cases f8 et h8 sont "
            + "contrôlées par la tour, et f7, g7, h7 sont occupées par les pions noirs : "
            + "c'est le mat du couloir, la faiblesse la plus fréquente en partie amicale.",
            true),

        new ExerciseSeed(
            "La dame soutenue par son roi",
            "6k1/8/6K1/8/8/8/8/4Q3 w - - 0 1",
            "e1e8",
            ExerciseTheme.MatEnUn,
            DifficultyLevel.TresFacile,
            "Le roi blanc contrôle déjà f7, g7 et h7.",
            "Il ne reste qu'une rangée à couvrir.",
            "La dame doit arriver sur la huitième rangée.",
            "1. De8# — Le roi blanc en g6 prive le roi noir de f7, g7 et h7 ; la dame "
            + "couvre toute la huitième rangée. C'est la position de mat élémentaire à "
            + "connaître avant d'aborder les finales.",
            true),

        new ExerciseSeed(
            "Le mat de l'escalier",
            "7k/1R6/8/8/8/8/R7/6K1 w - - 0 1",
            "a2a8",
            ExerciseTheme.MatEnUn,
            DifficultyLevel.Facile,
            "Une tour contrôle déjà la septième rangée.",
            "L'autre tour doit refermer le filet.",
            "La huitième rangée est libre.",
            "1. Ta8# — La tour en b7 interdit toute la septième rangée, la tour arrivant "
            + "en a8 donne échec sur la huitième : le roi noir n'a plus aucune case. "
            + "C'est la technique du mat des deux tours, à maîtriser en finale.",
            true),

        new ExerciseSeed(
            "La fourchette du cavalier",
            "4k3/8/8/1q6/4N3/8/8/4K3 w - - 0 1",
            "e4d6",
            ExerciseTheme.Fourchette,
            DifficultyLevel.Facile,
            "Le cavalier peut attaquer deux pièces à la fois.",
            "Cherchez une case d'où il donne échec.",
            "La case d6 mérite votre attention.",
            "1. Cd6+ — Le cavalier attaque simultanément le roi en e8 et la dame en b5. "
            + "Le roi doit parer l'échec, et les blancs prennent la dame au coup suivant. "
            + "La fourchette de cavalier est le motif tactique le plus rentable du jeu.",
            true),

        new ExerciseSeed(
            "Exploiter un clouage",
            "4k3/8/2n5/1B1P4/8/8/8/4K3 w - - 0 1",
            "d5c6",
            ExerciseTheme.Clouage,
            DifficultyLevel.Facile,
            "Le cavalier noir peut-il vraiment bouger ?",
            "Le fou en b5 vise le roi noir à travers le cavalier.",
            "Un pion peut prendre en toute impunité.",
            "1. dxc6 — Le cavalier en c6 est cloué sur la diagonale b5-e8 : le déplacer "
            + "exposerait le roi. Il ne peut donc pas riposter et les blancs gagnent une "
            + "pièce. Une pièce clouée est une pièce qui ne défend plus rien.",
            false),

        new ExerciseSeed(
            "L'enfilade",
            "4q3/8/8/4k3/8/8/8/R6K w - - 0 1",
            "a1e1",
            ExerciseTheme.Enfilade,
            DifficultyLevel.Moyen,
            "Roi et dame noirs sont sur la même colonne.",
            "Donnez échec sur cette colonne.",
            "La tour doit venir en e1.",
            "1. Te1+ — Le roi et la dame sont alignés sur la colonne e. L'échec force le "
            + "roi à se déplacer ou la dame à s'interposer ; dans les deux cas les blancs "
            + "gagnent du matériel. L'enfilade est l'inverse du clouage : c'est la pièce "
            + "la plus forte qui est devant.",
            false),

        new ExerciseSeed(
            "Le mat du berger",
            "rnbqk2r/pppp1ppp/5n2/2b1p2Q/2B1P3/8/PPPP1PPP/RNB1K1NR w KQkq - 4 4",
            "h5f7",
            ExerciseTheme.MatEnUn,
            DifficultyLevel.TresFacile,
            "Le point f7 est le plus faible de la position noire.",
            "Deux pièces blanches visent déjà cette case.",
            "La dame peut y entrer sans risque.",
            "1. Dxf7# — La dame prend en f7, protégée par le fou en c4. Le roi noir ne "
            + "peut ni prendre la dame (elle est défendue), ni fuir en d8 (occupée) ni en "
            + "f8 (contrôlée). C'est le mat du berger : à connaître pour ne jamais le subir.",
            true),

        new ExerciseSeed(
            "Le mat étouffé",
            "6rk/6pp/8/4N3/8/8/8/6K1 w - - 0 1",
            "e5f7",
            ExerciseTheme.MatEnUn,
            DifficultyLevel.Moyen,
            "Le roi noir est entouré de ses propres pièces.",
            "Seul le cavalier peut franchir cette muraille.",
            "Une case donne échec sans être reprenable.",
            "1. Cf7# — Le cavalier est la seule pièce capable de sauter par-dessus la "
            + "défense. Le roi est étouffé par sa tour en g8 et ses pions g7 et h7 : aucune "
            + "case de fuite, aucune prise possible.",
            false),

        new ExerciseSeed(
            "L'attaque double",
            "3r3k/8/4P3/6N1/8/8/8/6K1 w - - 0 1",
            "g5f7",
            ExerciseTheme.AttaqueDouble,
            DifficultyLevel.Moyen,
            "Le cavalier cherche une case protégée.",
            "Le pion e6 défend une case intéressante.",
            "De f7, que le cavalier attaque-t-il ?",
            "1. Cf7+ — Le cavalier donne échec au roi en h8 tout en attaquant la tour en "
            + "d8, et il est défendu par le pion e6 : le roi ne peut donc pas le prendre. "
            + "Après le coup de roi obligatoire, Cxd8 gagne la tour.",
            false),

        new ExerciseSeed(
            "La fourchette de pion",
            "4k3/8/2n1n3/8/3P4/8/8/4K3 w - - 0 1",
            "d4d5",
            ExerciseTheme.Fourchette,
            DifficultyLevel.TresFacile,
            "Le pion blanc est attaqué : ne le défendez pas, avancez-le.",
            "Un pion attaque en diagonale.",
            "De d5, le pion vise c6 et e6.",
            "1. d5 — Le pion attaque les deux cavaliers d'un coup. Les noirs ne peuvent en "
            + "sauver qu'un seul. Le pion, la pièce la moins chère, est aussi le meilleur "
            + "attaquant : personne n'aime capturer un cavalier avec un pion.",
            true),

        new ExerciseSeed(
            "La promotion décisive",
            "7k/P7/8/8/8/8/8/K7 w - - 0 1",
            "a7a8q",
            ExerciseTheme.Promotion,
            DifficultyLevel.TresFacile,
            "Le pion est à une case du bout.",
            "Un pion promu devient la pièce de votre choix.",
            "Choisissez la dame.",
            "1. a8=D — Le pion atteint la huitième rangée et se transforme en dame. En "
            + "finale, un pion passé qui avance vaut souvent plus qu'une pièce mineure : "
            + "« un pion passé doit être poussé ».",
            true)
    ];
}
