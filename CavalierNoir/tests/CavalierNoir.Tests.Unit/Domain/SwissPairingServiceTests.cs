using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Services;
using Xunit;

namespace CavalierNoir.Tests.Unit.Domain;

/// <summary>
/// Vérifie l'appariement au système suisse : interdiction des revanches (BR-06),
/// attribution du bye et équilibre des couleurs.
/// </summary>
public class SwissPairingServiceTests
{
    private static PairingCandidate Joueur(
        int id,
        decimal score = 0m,
        int elo = 1500,
        IEnumerable<int>? adversaires = null,
        int blancs = 0,
        int noirs = 0,
        bool bye = false,
        bool? derniereCouleurBlanche = null) =>
        new(
            id,
            $"Joueur {id}",
            score,
            elo,
            new HashSet<int>(adversaires ?? []),
            blancs,
            noirs,
            bye,
            derniereCouleurBlanche);

    [Fact]
    public void PairSwissRound_QuatreJoueurs_ProduitDeuxAppariements()
    {
        var appariements = SwissPairingService.PairSwissRound(
        [
            Joueur(1, elo: 1800),
            Joueur(2, elo: 1700),
            Joueur(3, elo: 1600),
            Joueur(4, elo: 1500)
        ]);

        Assert.Equal(2, appariements.Count);
        Assert.All(appariements, a => Assert.False(a.IsBye));

        var joues = appariements
            .SelectMany(a => new[] { a.WhitePlayerId, a.BlackPlayerId!.Value })
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1, 2, 3, 4], joues);
    }

    [Fact]
    public void PairSwissRound_EffectifImpair_AttribueUnSeulBye()
    {
        var appariements = SwissPairingService.PairSwissRound(
        [
            Joueur(1, score: 2m, elo: 1900),
            Joueur(2, score: 1m, elo: 1700),
            Joueur(3, score: 1m, elo: 1600),
            Joueur(4, score: 0m, elo: 1500),
            Joueur(5, score: 0m, elo: 1400)
        ]);

        var byes = appariements.Where(a => a.IsBye).ToList();

        Assert.Single(byes);
        Assert.Null(byes[0].BlackPlayerId);

        // Le bye revient au joueur le moins bien classé au score.
        Assert.Equal(5, byes[0].WhitePlayerId);
    }

    [Fact]
    public void PairSwissRound_ByeNestJamaisRedonneAuMemeJoueur()
    {
        var appariements = SwissPairingService.PairSwissRound(
        [
            Joueur(1, score: 1m, elo: 1900),
            Joueur(2, score: 1m, elo: 1800),
            Joueur(3, score: 0m, elo: 1500),
            Joueur(4, score: 0m, elo: 1400),
            Joueur(5, score: 0m, elo: 1300, bye: true)
        ]);

        var bye = Assert.Single(appariements.Where(a => a.IsBye));

        Assert.NotEqual(5, bye.WhitePlayerId);
    }

    [Fact]
    public void PairSwissRound_NeRejoueJamaisUnMemeAdversaire()
    {
        // 1 a déjà rencontré 2, et 3 a déjà rencontré 4 : le seul appariement
        // possible sans revanche est 1-3 et 2-4 (ou 1-4 et 2-3).
        var appariements = SwissPairingService.PairSwissRound(
        [
            Joueur(1, score: 1m, elo: 1800, adversaires: [2]),
            Joueur(2, score: 1m, elo: 1750, adversaires: [1]),
            Joueur(3, score: 0m, elo: 1600, adversaires: [4]),
            Joueur(4, score: 0m, elo: 1550, adversaires: [3])
        ]);

        foreach (var appariement in appariements)
        {
            var paire = new[] { appariement.WhitePlayerId, appariement.BlackPlayerId!.Value }.OrderBy(x => x).ToArray();

            Assert.False(paire is [1, 2], "Les joueurs 1 et 2 se sont déjà rencontrés.");
            Assert.False(paire is [3, 4], "Les joueurs 3 et 4 se sont déjà rencontrés.");
        }
    }

    [Fact]
    public void PairSwissRound_DonneLesBlancsAuJoueurEnDeficitDeBlancs()
    {
        var appariements = SwissPairingService.PairSwissRound(
        [
            Joueur(1, elo: 1800, blancs: 2, noirs: 0),
            Joueur(2, elo: 1700, blancs: 0, noirs: 2)
        ]);

        var appariement = Assert.Single(appariements);

        Assert.Equal(2, appariement.WhitePlayerId);
        Assert.Equal(1, appariement.BlackPlayerId);
    }

    [Fact]
    public void PairSwissRound_NumeroteLesEchiquiersAPartirDeUn()
    {
        var appariements = SwissPairingService.PairSwissRound(
        [
            Joueur(1, elo: 1800),
            Joueur(2, elo: 1700),
            Joueur(3, elo: 1600),
            Joueur(4, elo: 1500)
        ]);

        Assert.Equal([1, 2], appariements.Select(a => a.BoardNumber).OrderBy(n => n));
    }

    [Fact]
    public void PairSwissRound_MoinsDeDeuxJoueurs_EstRefuse()
    {
        Assert.Throws<DomainException>(() => SwissPairingService.PairSwissRound([Joueur(1)]));
    }

    [Fact]
    public void PairRoundRobin_ChaqueJoueurRencontreTousLesAutresUneSeuleFois()
    {
        var joueurs = new List<PairingCandidate>
        {
            Joueur(1, elo: 1800),
            Joueur(2, elo: 1700),
            Joueur(3, elo: 1600),
            Joueur(4, elo: 1500)
        };

        var rondes = SwissPairingService.PairRoundRobin(joueurs);

        Assert.Equal(3, rondes.Count);

        var rencontres = rondes
            .SelectMany(r => r)
            .Where(p => !p.IsBye)
            .Select(p => (Math.Min(p.WhitePlayerId, p.BlackPlayerId!.Value),
                          Math.Max(p.WhitePlayerId, p.BlackPlayerId!.Value)))
            .ToList();

        Assert.Equal(6, rencontres.Count);
        Assert.Equal(6, rencontres.Distinct().Count());
    }

    [Fact]
    public void PairRoundRobin_EffectifImpair_ProduitUnByeParRonde()
    {
        var rondes = SwissPairingService.PairRoundRobin(
        [
            Joueur(1),
            Joueur(2),
            Joueur(3)
        ]);

        Assert.Equal(3, rondes.Count);
        Assert.All(rondes, ronde => Assert.Single(ronde.Where(p => p.IsBye)));
    }
}
