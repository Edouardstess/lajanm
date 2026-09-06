using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.ValueObjects;
using Xunit;

namespace CavalierNoir.Tests.Unit.Domain;

/// <summary>Validation structurelle des positions au format Forsyth-Edwards.</summary>
public class FenTests
{
    [Theory]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
    [InlineData("6k1/5ppp/8/8/8/8/8/R5K1 w - - 0 1")]
    [InlineData("4k3/8/8/1q6/4N3/8/8/4K3 w - - 0 1")]
    [InlineData("7k/1R6/8/8/8/8/R7/6K1 w - - 0 1")]
    [InlineData("rnbqk2r/pppp1ppp/5n2/2b1p2Q/2B1P3/8/PPPP1PPP/RNB1K1NR w KQkq - 4 4")]
    public void TryParse_AccepteLesPositionsValides(string fen)
    {
        Assert.True(Fen.IsValid(fen), $"La position « {fen} » aurait dû être acceptée.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP")]              // 7 rangées seulement
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR")]      // trait manquant
    [InlineData("rnbqkbnr/pppppppp/9/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1")] // rangée de 9 cases
    [InlineData("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR x - - 0 1")] // trait invalide
    [InlineData("rnbqxbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w - - 0 1")] // pièce inconnue
    [InlineData("8/8/8/8/8/8/8/8 w - - 0 1")]                        // aucun roi
    [InlineData("4k3/8/8/8/8/8/8/8 w - - 0 1")]                      // roi blanc manquant
    public void TryParse_RejetteLesPositionsInvalides(string? fen)
    {
        Assert.False(Fen.IsValid(fen));
    }

    [Fact]
    public void Parse_PositionInvalide_LeveUneExceptionDeDomaine()
    {
        var exception = Assert.Throws<DomainException>(() => Fen.Parse("n'importe quoi"));

        Assert.Contains("FEN", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ComplèteLesChampsFacultatifs()
    {
        var position = Fen.Parse("6k1/5ppp/8/8/8/8/8/R5K1 w - -");

        Assert.EndsWith("0 1", position.Value);
    }

    [Theory]
    [InlineData("6k1/5ppp/8/8/8/8/8/R5K1 w - - 0 1", true)]
    [InlineData("6k1/5ppp/8/8/8/8/8/R5K1 b - - 0 1", false)]
    public void WhiteToMove_LitLeTrait(string fen, bool attendu)
    {
        Assert.Equal(attendu, Fen.Parse(fen).WhiteToMove);
    }

    [Fact]
    public void PiecePlacement_RenvoieLePremierChamp()
    {
        var position = Fen.Parse(Fen.StartingPosition);

        Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR", position.PiecePlacement);
    }
}
