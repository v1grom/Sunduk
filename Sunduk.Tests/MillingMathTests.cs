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
    public void HighFeed_ChipThicknessFollowsLeadAngle()
    {
        // κ 15°, полная ширина: fz·sin15°
        Assert.Equal(1.5 * Math.Sin(15 * Math.PI / 180), MillingMath.HighFeedChipThickness(32, 32, 15, 1.5), 6);
        // D10, ae 1: ещё ×0.6; подача поднимается в 1/0.6
        Assert.Equal(1 * Math.Sin(15 * Math.PI / 180) * 0.6, MillingMath.HighFeedChipThickness(10, 1, 15, 1), 6);
        Assert.Equal(1 / 0.6, MillingMath.HighFeedCorrectedFeed(10, 1, 1), 6);
        Assert.True(double.IsNaN(MillingMath.HighFeedChipThickness(10, 1, 0, 1)));
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

    [Fact]
    public void EdgeChamfer_SharpTool90_FollowsWidth()
    {
        // фаска 1×45° острой фрезой 90°, вершина ниже фаски на 0.5: Z = 1 + 0.5, ось от стенки 0.5·tg45°
        var r = MillingMath.EdgeChamfer(90, 0, 1, 0.5)!.Value;
        Assert.Equal(1.5, r.Depth, 4);
        Assert.Equal(0.5, r.Offset, 4);
        Assert.Equal(1, r.Height, 4);
        Assert.Equal(3, r.MaxDiameter, 4);
        Assert.Equal(2, r.MeanDiameter, 4);
    }

    [Fact]
    public void EdgeChamfer_FlatTipAndSpotAngle()
    {
        // фреза 60° с площадкой Ø2, ширина 1: высота 1/tg30°, ось от стенки 1 + 0.3·tg30°
        var tan = Math.Tan(30 * Math.PI / 180);
        var r = MillingMath.EdgeChamfer(60, 2, 1, 0.3)!.Value;
        Assert.Equal(1 / tan, r.Height, 4);
        Assert.Equal(1 / tan + 0.3, r.Depth, 4);
        Assert.Equal(1 + 0.3 * tan, r.Offset, 4);
        Assert.Null(MillingMath.EdgeChamfer(180, 0, 1, 0));
        Assert.Null(MillingMath.EdgeChamfer(90, 0, 0, 0));
    }

    [Fact]
    public void HoleChamfer_PlungeDepth()
    {
        // отверстие Ø10, фаска 1 на сторону, острая 90°: верх фаски Ø12, глубина 6
        Assert.Equal(6, MillingMath.HoleChamferDepth(90, 0, 10, 1), 4);
        // с площадкой Ø4: (12 − 4) / 2
        Assert.Equal(4, MillingMath.HoleChamferDepth(90, 4, 10, 1), 4);
        // площадка не входит в отверстие
        Assert.True(double.IsNaN(MillingMath.HoleChamferDepth(90, 10, 10, 1)));
    }
}
