using Sunduk.Geometry;
using Sunduk.Geometry.ContourElements;
using Sunduk.Geometry.ContourElements.Base;
using Sunduk.PWA.Infrastructure.Visualization;

namespace Sunduk.Tests;

/// <summary>
/// Траектория превью точения в плоскости XZ: координаты в (Z, R), R = X / 2. Ожидания посчитаны
/// вручную (см. комментарии в тестах), не снимком результата.
/// </summary>
public class TurningToolpathTests
{
    [Fact]
    public void NominalContour_RadiusOnNode_IsRoundedWithTangentArc()
    {
        // (X30,Z0) → (X30,Z-20 R3) → (X44,Z-20): идём к -Z, затем вверх — поворот направо (CW),
        // касательные точки в 3 мм от угла, центр скругления (Z-17, R18).
        var contour = new List<Element>
        {
            new Point(30, 0),
            new Line(30, -20, 0, 3),
            new Line(44, -20),
        };

        var path = TurningToolpath.Build(contour);

        Assert.NotNull(path);
        Assert.Equal(3, path!.Nominal.Count);
        var arc = path.Nominal[1];
        Assert.True(arc.IsArc);
        Assert.Equal(Direction.CW, arc.Direction);
        Assert.Equal(3, arc.Radius, 4);
        Assert.Equal(-17, arc.From.Z, 4);
        Assert.Equal(15, arc.From.R, 4);
        Assert.Equal(-20, arc.To.Z, 4);
        Assert.Equal(18, arc.To.R, 4);
        Assert.Equal(-17, arc.Center.Z, 4);
        Assert.Equal(18, arc.Center.R, 4);
    }

    [Fact]
    public void Chamfer_IsStraightCutBetweenTangentPoints()
    {
        var contour = new List<Element>
        {
            new Point(30, 0),
            new Line(30, -20, 0, 2, Blunt.SimpleChamfer),
            new Line(44, -20),
        };

        var path = TurningToolpath.Build(contour);

        var cut = path!.Nominal[1];
        Assert.False(cut.IsArc);
        Assert.Equal(-18, cut.From.Z, 4);
        Assert.Equal(15, cut.From.R, 4);
        Assert.Equal(-20, cut.To.Z, 4);
        Assert.Equal(17, cut.To.R, 4);
    }

    [Fact]
    public void Arc_CenterLiesOnTheSideOfDirection()
    {
        // от (Z0,R10) к (Z-10,R20) радиусом 10: при CCW центр слева от хорды → (Z0,R20)
        var move = new PathMove { From = new Pt(0, 10), To = new Pt(-10, 20), Radius = 10, Direction = Direction.CCW };
        Assert.Equal(-10, move.Center.Z, 4);
        Assert.Equal(10, move.Center.R, 4);
        var cw = new PathMove { From = move.From, To = move.To, Radius = 10, Direction = Direction.CW };
        Assert.Equal(0, cw.Center.Z, 4);
        Assert.Equal(20, cw.Center.R, 4);
    }

    [Fact]
    public void IncompleteContour_GivesNoPath()
    {
        Assert.Null(TurningToolpath.Build(new List<Element> { new Point(null, null), new Line(10, -5) }));
        Assert.Null(TurningToolpath.Build(new List<Element> { new Point(10, 0) }));
    }
}

public class StartCornerTests
{
    [Fact]
    public void ExternalStart_RoundingIsOnTheConvexEdge_BelowTheNode()
    {
        // торец детали идёт от оси вверх до узла (X18), дальше конус вверх-влево. Угол выпуклый:
        // скругление R1 — на детали, касание на торце ниже узла: поворот 53.13°, t = R·tg(θ/2) = 0.5 →
        // касание на торце в (Z0; R8.5), на конусе в (Z−0.4; R9.3), центр (Z−1; R8.5) — внутри детали
        var contour = new List<Element> { new Point(18, 0, 1), new Line(30, -8) };

        var path = TurningToolpath.Build(contour)!;

        var arc = path.Nominal[0];
        Assert.True(arc.IsArc);
        Assert.Equal(0, arc.From.Z, 4);
        Assert.Equal(8.5, arc.From.R, 4);
        Assert.Equal(-0.4, arc.To.Z, 4);
        Assert.Equal(9.3, arc.To.R, 4);
        Assert.Equal(Direction.CCW, arc.Direction);
        Assert.Equal(-1, arc.Center.Z, 4);
        Assert.Equal(8.5, arc.Center.R, 4);
    }

    [Fact]
    public void ExternalStart_ChamferOnFirstNode_CutsTheEdgeBelow()
    {
        var contour = new List<Element> { new Line(20, 0, 0, 2, Blunt.SimpleChamfer), new Line(20, -10) };
        contour[0] = new Point(20, 0, 2);
        contour[1] = new Line(20, -10);

        var path = TurningToolpath.Build(contour)!;

        // фаска идёт между точкой на торце (R8) и точкой на цилиндре (R10, Z−2)
        var first = path.Nominal[0];
        Assert.Equal(0, first.From.Z, 4);
        Assert.True(first.To.Z < 0);
    }
}
