using Sunduk.PWA.Infrastructure;
using Sunduk.PWA.Infrastructure.Tolerances;

namespace Sunduk.Tests;

/// <summary>Ввод размера с допуском в поле координаты: в контур идёт середина поля допуска.</summary>
public class DimensionInputTests
{
    [Theory]
    [InlineData("30", 30)]
    [InlineData("-12,5", -12.5)]
    [InlineData("30h7", 29.9895)]          // 0 / −21 мкм
    [InlineData("50H7", 50.0125)]          // +25 / 0
    [InlineData("50f7", 49.9625)]          // −25 / −50
    [InlineData("50 k6", 50.010)]          // +18 / +2
    [InlineData("-50h7", -49.9875)]        // размер 50h7, отложенный в минус по Z
    [InlineData("30+0.1/-0.05", 30.025)]
    [InlineData("30-0.02/-0.05", 29.965)]
    [InlineData("30+0,1/0", 30.05)]
    [InlineData("-25+0.2/0", -25.1)]
    [InlineData("45±0.1", 45)]
    [InlineData("45+-0.1", 45)]
    public void Parse_GivesMiddleOfToleranceField(string text, double expected)
        => Assert.Equal(expected, DimensionInput.Parse(text)!.Value, 4);

    [Theory]
    [InlineData("")]
    [InlineData("-")]
    [InlineData(null)]
    public void Parse_EmptyField_IsNull(string? text) => Assert.Null(DimensionInput.Parse(text));

    [Theory]
    [InlineData("30h", 30)]                // допуск ещё не дописан — контур не должен прыгать в ноль
    [InlineData("30+0.1/", 30)]
    [InlineData("30h1", 30)]               // квалитет не поддерживается
    [InlineData("abc", 0)]
    public void Parse_UnfinishedInput_FallsBackToLeadingNumber(string text, double expected)
        => Assert.Equal(expected, DimensionInput.Parse(text)!.Value, 4);
}

public class RoughnessTests
{
    [Fact]
    public void TheoreticalRz_MatchesClassicFormula()
    {
        // f²/8r: 0.2² / (8·0.8) = 6.25 мкм; точная формула по дуге даёт чуть больше: 0.8 − √(0.64 − 0.01)
        Assert.Equal(6.2746, Roughness.TheoreticalRz(0.2, 0.8), 3);
        Assert.Equal(6.2746 * Roughness.RaToHeight, Roughness.TheoreticalRa(0.2, 0.8), 3);
    }

    [Fact]
    public void FeedForRz_IsInverseOfTheoreticalRz()
    {
        Assert.Equal(0.2, Roughness.FeedForRz(Roughness.TheoreticalRz(0.2, 0.8), 0.8), 6);
        Assert.Equal(0.1, Roughness.FeedForRa(Roughness.TheoreticalRa(0.1, 0.4), 0.4), 6);
    }

    [Fact]
    public void FeedNotLessThanTwoRadii_HasNoResult()
        => Assert.True(double.IsNaN(Roughness.TheoreticalRz(0.8, 0.4)));

    [Fact]
    public void RaRz_UseFactor4ForRoughAnd5ForFineSurfaces()
    {
        Assert.Equal(10, Roughness.RaToRz(2.5), 4);
        Assert.Equal(8, Roughness.RaToRz(1.6), 4);
        Assert.Equal(2.5, Roughness.RzToRa(10), 4);
        Assert.Equal(1.26, Roughness.RzToRa(6.3), 4);
    }
}

public class HardnessTests
{
    [Fact]
    public void Convert_ReadsTableRowsAndInterpolatesBetweenThem()
    {
        Assert.Equal(400, Hardness.Convert(Hardness.Scale.HRC, Hardness.Scale.HV, 40.8)!.Value, 4);
        Assert.Equal(380, Hardness.Convert(Hardness.Scale.HRC, Hardness.Scale.HB, 40.8)!.Value, 4);
        Assert.Equal(1290, Hardness.Convert(Hardness.Scale.HV, Hardness.Scale.Strength, 400)!.Value, 4);
        Assert.Equal(405, Hardness.Convert(Hardness.Scale.HRC, Hardness.Scale.HV, 41.3)!.Value, 4);   // середина между 40.8 и 41.8
    }

    [Fact]
    public void Convert_OutsideTableOrScale_IsNull()
    {
        Assert.Null(Hardness.Convert(Hardness.Scale.HV, Hardness.Scale.HRC, 200));   // HRC ниже 20 не применяется
        Assert.Null(Hardness.Convert(Hardness.Scale.HRC, Hardness.Scale.HB, 62));    // HB выше 618 в таблице нет
        Assert.Null(Hardness.Convert(Hardness.Scale.HRC, Hardness.Scale.HV, 70));
    }

    [Fact]
    public void Table_IsMonotonic()
    {
        foreach (var scale in Enum.GetValues<Hardness.Scale>())
        {
            var values = Hardness.Table.Select(r => r[scale]).Where(v => v is not null).Select(v => v!.Value).ToArray();
            Assert.True(values.Zip(values.Skip(1)).All(p => p.First < p.Second), scale.ToString());
        }
    }
}
