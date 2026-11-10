using Sunduk.PWA.Infrastructure;
using Sunduk.PWA.Infrastructure.Files;
using Sunduk.PWA.Infrastructure.Sequences;
using Sunduk.PWA.Infrastructure.Sequences.Milling;

namespace Sunduk.Tests;

public class MillingMathTests
{
    [Fact]
    public void ChipThinning_NarrowCut_ThinsTheChip()
    {
        // D10, ae 1: 2·√(1·9)/10 = 0.6
        Assert.Equal(0.6, MillingMath.ChipThinningFactor(10, 1), 6);
        Assert.Equal(0.1 / 0.6, MillingMath.FeedForChipThickness(10, 1, 0.1), 6);
    }

    [Fact]
    public void ChipThinning_FromHalfDiameter_IsAbsent()
    {
        Assert.Equal(1, MillingMath.ChipThinningFactor(10, 5), 6);
        Assert.Equal(1, MillingMath.ChipThinningFactor(10, 10), 6);
        Assert.True(double.IsNaN(MillingMath.ChipThinningFactor(10, 12)));
    }

    [Fact]
    public void BallEffectiveDiameter_ShallowCut()
    {
        // D10, ap 1: 2·√(1·9) = 6
        Assert.Equal(6, MillingMath.BallEffectiveDiameter(10, 1), 6);
        Assert.Equal(10, MillingMath.BallEffectiveDiameter(10, 6), 6);
        Assert.Equal(1000 * 120 / (Math.PI * 6), MillingMath.SpindleSpeed(120, 6), 4);
    }

    [Fact]
    public void Scallop_HeightAndStepAreInverse()
    {
        // D10, шаг 1: 5 − √(25 − 0.25) = 0.025063
        Assert.Equal(0.025063, MillingMath.ScallopHeight(10, 1), 6);
        Assert.Equal(1, MillingMath.StepForScallop(10, MillingMath.ScallopHeight(10, 1)), 6);
    }
}

public class HoleGridTests
{
    private static string Text(HoleGrid grid) => string.Join(" ", grid.Build().Select(h => $"{h.X};{h.Y}"));

    [Fact]
    public void Grid_GoesRowByRow_Zigzag()
    {
        var grid = new HoleGrid { X = 10, Y = 5, StepX = 20, StepY = 15, CountX = 3, CountY = 2 };
        Assert.Equal("10;5 30;5 50;5 50;20 30;20 10;20", Text(grid));
    }

    [Fact]
    public void Grid_WithoutZigzag_StartsEveryRowFromTheFirstColumn()
    {
        var grid = new HoleGrid { StepX = 10, StepY = 10, CountX = 2, CountY = 2, Zigzag = false };
        Assert.Equal("0;0 10;0 0;10 10;10", Text(grid));
    }

    [Fact]
    public void Grid_RotatesAroundTheFirstHole()
    {
        var grid = new HoleGrid { X = 10, Y = 10, StepX = 20, StepY = 5, CountX = 2, CountY = 2, Angle = 90, Zigzag = false };
        Assert.Equal("10;10 10;30 5;10 5;30", Text(grid));
    }

    [Fact]
    public void Grid_SurvivesWorkFileRoundTrip()
    {
        var machine = new Machine { MachineType = MachineType.Milling };
        var grid = new HoleGrid { X = 10, Y = 5, StepX = 20, StepY = 15, CountX = 3, CountY = 2, Angle = 30, Zigzag = false };
        var sequence = new MillingHighSpeedDrillingSequence(machine, CoordinateSystem.G54, Material.Steel, null, 2, -10, 80, 0.1, grid.Build(), false, 50) { Grid = grid };
        var manual = new MillingHighSpeedDrillingSequence(machine, CoordinateSystem.G54, Material.Steel, null, 2, -10, 80, 0.1, new List<Hole> { new(1, 2) }, false, 50);

        var json = CndcFile.Write(new CndcDocument { Program = { sequence, manual } });
        var read = CndcFile.Read(json, _ => machine);

        Assert.Empty(read.Errors);
        var loaded = Assert.IsType<MillingHighSpeedDrillingSequence>(read.Document.Program[0]);
        Assert.NotNull(loaded.Grid);
        Assert.Equal(Text(grid), Text(loaded.Grid));
        Assert.Equal(6, loaded.Holes.Count);
        Assert.Null(Assert.IsType<MillingHighSpeedDrillingSequence>(read.Document.Program[1]).Grid);
    }
}
