using Sunduk.Geometry;
using Sunduk.Geometry.ContourElements;
using Sunduk.Geometry.ContourElements.Base;
using Sunduk.PWA.Infrastructure;

namespace Sunduk.Tests;

/// <summary>Кнопка «Рассчитать» контура. Ожидания посчитаны вручную: X — диаметр, угол — к оси Z,
/// Δрадиуса = ΔZ·tg(угла).</summary>
public class ContourCalculatorTests
{
    [Fact]
    public void Line_ByDiameterAndAngle_45Degrees()
    {
        // от (X20, Z0) до X30: Δрадиуса 5, угол 45° → |ΔZ| = 5; диаметр растёт к −Z → Z = −5
        var contour = new List<Element> { new Point(20, 0), new Line(30, null, 45) };

        Assert.Null(ContourCalculator.Calculate(contour, 1));

        Assert.Equal(-5, contour[1].Z!.Value, 4);
    }

    [Fact]
    public void Line_ByDiameterAndAngle_30Degrees()
    {
        // Δрадиуса 5 при угле 30° к оси: ΔZ = 5 / tg30° = 8.6603
        var contour = new List<Element> { new Point(20, 0), new Line(30, null, 30) };

        Assert.Null(ContourCalculator.Calculate(contour, 1));

        Assert.Equal(-8.6603, contour[1].Z!.Value, 4);
    }

    [Fact]
    public void Line_ByZAndAngle()
    {
        // ΔZ = −10, угол 30°: Δрадиуса = 10·tg30° = 5.7735 → X = 20 + 11.547 = 31.547
        var contour = new List<Element> { new Point(20, 0), new Line(null, -10, 30) };

        Assert.Null(ContourCalculator.Calculate(contour, 1));

        Assert.Equal(31.547, contour[1].X!.Value, 3);
    }

    [Fact]
    public void Line_DiameterShrinking_GoesTowardPlusZ_ForPositiveAngle()
    {
        // положительный угол: диаметр растёт к −Z, значит убывающий диаметр лежит со стороны +Z
        var contour = new List<Element> { new Point(30, -20), new Line(20, null, 45) };

        Assert.Null(ContourCalculator.Calculate(contour, 1));

        Assert.Equal(-15, contour[1].Z!.Value, 4);
    }

    [Fact]
    public void Line_NegativeAngle_MeansDiameterShrinkingTowardMinusZ()
    {
        // сужающийся к −Z конус: из (X30, Z0) до X20, угол −45°: Δрадиуса −5 → Z = −5
        var contour = new List<Element> { new Point(30, 0), new Line(20, null, -45) };

        Assert.Null(ContourCalculator.Calculate(contour, 1));

        Assert.Equal(-5, contour[1].Z!.Value, 4);
    }

    [Fact]
    public void Line_NegativeAngle_ByZ()
    {
        // из (X30, Z0) до Z−10 с углом −45°: диаметр убывает на 2·10·tg45° = 20 → X10
        var contour = new List<Element> { new Point(30, 0), new Line(null, -10, -45) };

        Assert.Null(ContourCalculator.Calculate(contour, 1));

        Assert.Equal(10, contour[1].X!.Value, 4);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(120)]
    [InlineData(-90)]
    public void Line_AngleOutOfRange_Warns_AndChangesNothing(double angle)
    {
        var contour = new List<Element> { new Point(20, 0), new Line(30, null, angle) };

        Assert.NotNull(ContourCalculator.Calculate(contour, 1));

        Assert.Null(contour[1].Z);
    }

    [Fact]
    public void Line_WithoutAngle_InheritsMissingFromPrevious()
    {
        var contour = new List<Element> { new Point(20, -3), new Line(30, null) };

        Assert.Null(ContourCalculator.Calculate(contour, 1));

        Assert.Equal(-3, contour[1].Z!.Value, 4);
    }

    [Fact]
    public void Line_AngleButNoPreviousCoordinates_Warns()
    {
        var contour = new List<Element> { new Point(null, null), new Line(30, null, 45) };

        Assert.NotNull(ContourCalculator.Calculate(contour, 1));
    }

    [Fact]
    public void Arc_CenterComputed_InDiameterForX()
    {
        // от (X20, Z0) до (X30, Z−5), R5, CCW: в радиусах от (10; 0) до (15; −5), центр (10; −5);
        // CenterX хранится диаметром → 20
        var contour = new List<Element> { new Point(20, 0), new Arc(30, -5, 5, Direction.CCW) };

        Assert.Null(ContourCalculator.Calculate(contour, 1));

        var arc = (Arc)contour[1];
        Assert.Equal(20, arc.CenterX, 4);
        Assert.Equal(-5, arc.CenterZ, 4);
    }

    [Fact]
    public void Arc_CenterOnTheOtherSide_ForCw()
    {
        // то же, но CW: центр справа от хорды — (15; 0) в радиусах → X30, Z0
        var contour = new List<Element> { new Point(20, 0), new Arc(30, -5, 5, Direction.CW) };

        Assert.Null(ContourCalculator.Calculate(contour, 1));

        var arc = (Arc)contour[1];
        Assert.Equal(30, arc.CenterX, 4);
        Assert.Equal(0, arc.CenterZ, 4);
    }

    [Fact]
    public void Arc_CenterAtDistanceRadiusFromBothEnds()
    {
        var contour = new List<Element> { new Point(18, -2), new Arc(34, -13, 9, Direction.CCW) };

        Assert.Null(ContourCalculator.Calculate(contour, 1));

        var arc = (Arc)contour[1];
        double Dist(double x, double z) => Math.Sqrt(Math.Pow(x / 2 - arc.CenterX / 2, 2) + Math.Pow(z - arc.CenterZ, 2));
        Assert.Equal(9, Dist(18, -2), 4);
        Assert.Equal(9, Dist(34, -13), 4);
    }

    [Fact]
    public void Arc_RadiusTooSmall_Warns()
    {
        var contour = new List<Element> { new Point(20, 0), new Arc(40, -30, 5, Direction.CCW) };

        Assert.NotNull(ContourCalculator.Calculate(contour, 1));
    }

    [Fact]
    public void Arc_MissingEndCoordinate_InheritsPrevious()
    {
        var contour = new List<Element> { new Point(20, -4), new Arc(null, -9, 5, Direction.CCW) };

        Assert.Null(ContourCalculator.Calculate(contour, 1));

        Assert.Equal(20, contour[1].X!.Value, 4);
    }

    [Fact]
    public void Point_StartTakesDiameterOfNextNode_OnTheFace()
    {
        var contour = new List<Element> { new Point(null, null, 3), new Line(30, -20) };

        Assert.Null(ContourCalculator.Calculate(contour, 0));

        Assert.Equal(30, contour[0].X!.Value, 4);
        Assert.Equal(0, contour[0].Z!.Value, 4);
    }
}
