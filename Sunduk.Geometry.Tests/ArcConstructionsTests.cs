using Sunduk.Geometry;

namespace Sunduk.Geometry.Tests;

/// <summary>Числовые проверки построений по дугам и прямым (<see cref="ArcConstructions"/>).
/// Координаты здесь — (X радиус, Z). Часть тестов — «свойства»: независимо от конкретных чисел
/// касательная перпендикулярна радиусу в точке касания, точки лежат на своих окружностях и т.п.</summary>
public class ArcConstructionsTests
{
    private static double Dist((double X, double Z) a, (double X, double Z) b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));

    // ---------------------------------------------------------------- дуга ∩ дуга

    [Fact]
    public void CircleCircle_TwoPoints_Pythagorean345()
    {
        var res = ArcConstructions.IntersectCircles(new(0, 0, 5), new(0, 6, 5));

        Assert.Equal(CircleRelation.Intersecting, res.Relation);
        Assert.Equal(6, res.CenterDistance, 4);
        Assert.Equal(2, res.Points.Count);
        Assert.Equal(-4, res.Points[0].X, 4);
        Assert.Equal(3, res.Points[0].Z, 4);
        Assert.Equal(4, res.Points[1].X, 4);
        Assert.Equal(3, res.Points[1].Z, 4);
    }

    [Fact]
    public void CircleCircle_PointsLieOnBothCircles()
    {
        GeoCircle a = new(1.5, -2, 7), b = new(-3, 4.25, 5.5);
        var res = ArcConstructions.IntersectCircles(a, b);

        Assert.Equal(CircleRelation.Intersecting, res.Relation);
        foreach (var p in res.Points)
        {
            Assert.Equal(a.R, Dist(p, (a.X, a.Z)), 4);
            Assert.Equal(b.R, Dist(p, (b.X, b.Z)), 4);
        }
    }

    [Fact]
    public void CircleCircle_ExternalTangent()
    {
        var res = ArcConstructions.IntersectCircles(new(0, 0, 3), new(0, 7, 4));

        Assert.Equal(CircleRelation.ExternalTangent, res.Relation);
        var p = Assert.Single(res.Points);
        Assert.Equal(0, p.X, 4);
        Assert.Equal(3, p.Z, 4);
    }

    [Theory]
    [InlineData(0, 2, 5, 3, 0, 5)]   // малая дуга внутри большой — большая слева, точка у большой
    [InlineData(0, 2, 3, 5, 0, -3)]  // наоборот
    public void CircleCircle_InternalTangent(double ax, double bz, double ar, double br, double ex, double ez)
    {
        var res = ArcConstructions.IntersectCircles(new(ax, 0, ar), new(0, bz, br));

        Assert.Equal(CircleRelation.InternalTangent, res.Relation);
        var p = Assert.Single(res.Points);
        Assert.Equal(ex, p.X, 4);
        Assert.Equal(ez, p.Z, 4);
    }

    [Theory]
    [InlineData(0, 0, 3, 0, 20, 4, CircleRelation.Separate)]
    [InlineData(0, 0, 10, 0, 2, 3, CircleRelation.Nested)]
    [InlineData(0, 0, 10, 0, 0, 3, CircleRelation.Nested)]       // соосные разного радиуса
    [InlineData(0, 0, 3, 0, 0, 3, CircleRelation.Coincident)]
    public void CircleCircle_NoPoints(double ax, double az, double ar, double bx, double bz, double br, CircleRelation expected)
    {
        var res = ArcConstructions.IntersectCircles(new(ax, az, ar), new(bx, bz, br));

        Assert.Equal(expected, res.Relation);
        Assert.Empty(res.Points);
    }

    // ---------------------------------------------------------------- дуга ∩ линия

    [Fact]
    public void LineCircle_Secant()
    {
        // Линия Z = 2 вдоль оси X, окружность R5 в начале координат.
        var line = new GeoLine(0, 2, 1, 0);
        var res = ArcConstructions.IntersectLineCircle(line, new(0, 0, 5));

        Assert.Equal(LineCircleRelation.Secant, res.Relation);
        Assert.Equal(2, res.Distance, 4);
        Assert.Equal(-Math.Sqrt(21), res.Points[0].X, 4);
        Assert.Equal(Math.Sqrt(21), res.Points[1].X, 4);
        Assert.All(res.Points, p => Assert.Equal(2, p.Z, 4));
    }

    [Fact]
    public void LineCircle_Tangent()
    {
        var res = ArcConstructions.IntersectLineCircle(new GeoLine(0, 5, 1, 0), new(0, 0, 5));

        Assert.Equal(LineCircleRelation.Tangent, res.Relation);
        var p = Assert.Single(res.Points);
        Assert.Equal(0, p.X, 4);
        Assert.Equal(5, p.Z, 4);
    }

    [Fact]
    public void LineCircle_None()
    {
        var res = ArcConstructions.IntersectLineCircle(new GeoLine(0, 6, 1, 0), new(0, 0, 5));

        Assert.Equal(LineCircleRelation.None, res.Relation);
        Assert.Empty(res.Points);
    }

    [Fact]
    public void Line_FromAngle_45Degrees()
    {
        var line = GeoLine.FromAngle(0, 0, 45);
        Assert.Equal(Math.Sqrt(0.5), line.DX, 4);
        Assert.Equal(Math.Sqrt(0.5), line.DZ, 4);
    }

    [Fact]
    public void Lines_IntersectAndParallel()
    {
        var p = ArcConstructions.IntersectLines(new GeoLine(0, 0, 1, 0), new GeoLine(3, 0, 0, 1));
        Assert.NotNull(p);
        Assert.Equal(3, p!.Value.X, 4);
        Assert.Equal(0, p.Value.Z, 4);

        Assert.Null(ArcConstructions.IntersectLines(new GeoLine(0, 0, 1, 0), new GeoLine(0, 5, 1, 0)));
    }

    // ---------------------------------------------------------------- касательные

    [Fact]
    public void TangentsFromPoint_345()
    {
        var tangents = ArcConstructions.TangentsFromPoint(0, 5, new(0, 0, 3));

        Assert.Equal(2, tangents.Count);
        Assert.All(tangents, t => Assert.Equal(4, t.Length, 4));
        Assert.Equal(-2.4, tangents[0].Touch.X, 4);
        Assert.Equal(1.8, tangents[0].Touch.Z, 4);
        Assert.Equal(2.4, tangents[1].Touch.X, 4);
        Assert.Equal(1.8, tangents[1].Touch.Z, 4);
    }

    [Fact]
    public void TangentsFromPoint_InsideNone_OnCircleOne()
    {
        Assert.Empty(ArcConstructions.TangentsFromPoint(0, 1, new(0, 0, 3)));

        var one = ArcConstructions.TangentsFromPoint(0, 3, new(0, 0, 3));
        var t = Assert.Single(one);
        Assert.Equal(0, t.Length, 4);
    }

    [Fact]
    public void CommonTangents_SeparateCircles_FourLines()
    {
        GeoCircle a = new(0, 0, 2), b = new(1, 10, 3);
        var tangents = ArcConstructions.CommonTangents(a, b);

        Assert.Equal(4, tangents.Count);
        Assert.Equal(2, tangents.Count(t => t.Kind == CommonTangentKind.External));
        Assert.Equal(2, tangents.Count(t => t.Kind == CommonTangentKind.Internal));
        AssertCommonTangentsAreTangent(a, b, tangents);
    }

    [Fact]
    public void CommonTangents_EqualRadii_ExternalAreParallelToCenters()
    {
        var tangents = ArcConstructions.CommonTangents(new(0, 0, 2), new(0, 10, 2));

        var ext = tangents.Where(t => t.Kind == CommonTangentKind.External).ToList();
        Assert.Equal(2, ext.Count);
        Assert.Contains(ext, t => Math.Abs(t.Touch1.X - 2) < 1e-4 && Math.Abs(t.Touch2.X - 2) < 1e-4);
        Assert.Contains(ext, t => Math.Abs(t.Touch1.X + 2) < 1e-4 && Math.Abs(t.Touch2.X + 2) < 1e-4);
    }

    [Fact]
    public void CommonTangents_Intersecting_OnlyExternal()
    {
        GeoCircle a = new(0, 0, 5), b = new(0, 6, 5);
        var tangents = ArcConstructions.CommonTangents(a, b);

        Assert.Equal(2, tangents.Count);
        Assert.All(tangents, t => Assert.Equal(CommonTangentKind.External, t.Kind));
        AssertCommonTangentsAreTangent(a, b, tangents);
    }

    [Fact]
    public void CommonTangents_ExternallyTouching_ThreeLines()
    {
        GeoCircle a = new(0, 0, 3), b = new(0, 7, 4);
        var tangents = ArcConstructions.CommonTangents(a, b);

        Assert.Equal(3, tangents.Count);
        Assert.Single(tangents, t => t.Kind == CommonTangentKind.Internal);
        AssertCommonTangentsAreTangent(a, b, tangents);
    }

    [Fact]
    public void CommonTangents_Nested_None()
    {
        Assert.Empty(ArcConstructions.CommonTangents(new(0, 0, 10), new(0, 1, 2)));
    }

    private static void AssertCommonTangentsAreTangent(GeoCircle a, GeoCircle b, IReadOnlyList<CommonTangent> tangents)
    {
        foreach (var t in tangents)
        {
            Assert.Equal(a.R, Dist(t.Touch1, (a.X, a.Z)), 4);
            Assert.Equal(b.R, Dist(t.Touch2, (b.X, b.Z)), 4);

            // Прямая T1→T2 перпендикулярна радиусам в обеих точках касания.
            var dx = t.Touch2.X - t.Touch1.X;
            var dz = t.Touch2.Z - t.Touch1.Z;
            Assert.Equal(0, dx * (t.Touch1.X - a.X) + dz * (t.Touch1.Z - a.Z), 4);
            Assert.Equal(0, dx * (t.Touch2.X - b.X) + dz * (t.Touch2.Z - b.Z), 4);
        }
    }

    // ---------------------------------------------------------------- сопряжение радиусом

    [Fact]
    public void FilletLineLine_PerpendicularLines_FourCircles()
    {
        // Оси: линия X = 0 (вдоль Z) и линия Z = 0 (вдоль X), радиус 2.
        var res = ArcConstructions.FilletLineLine(new GeoLine(0, 0, 0, 1), new GeoLine(0, 0, 1, 0), 2);

        Assert.Equal(4, res.Count);
        Assert.All(res, f =>
        {
            Assert.Equal(2, Math.Abs(f.Center.X), 4);
            Assert.Equal(2, Math.Abs(f.Center.Z), 4);
            Assert.Equal(2, Dist(f.Touch1, f.Center), 4);
            Assert.Equal(2, Dist(f.Touch2, f.Center), 4);
        });
    }

    [Fact]
    public void FilletLineLine_Parallel_None()
    {
        Assert.Empty(ArcConstructions.FilletLineLine(new GeoLine(0, 0, 1, 0), new GeoLine(0, 5, 1, 0), 2));
    }

    [Fact]
    public void FilletCircleLine_ConcreteCase()
    {
        // Окружность R10 в начале координат и линия X = 14 (вдоль Z); r = 2, снаружи по обе стороны.
        // Центр сопрягающей дуги на X = 12 (расстояние до линии r) и на расстоянии 12 от начала → (12, 0).
        var res = ArcConstructions.FilletCircleLine(new(0, 0, 10), new GeoLine(14, 0, 0, 1), 2);

        var f = Assert.Single(res, r => r.Contact1 == ContactKind.Outside && Math.Abs(r.Center.X - 12) < 1e-4);
        Assert.Equal(0, f.Center.Z, 4);
        Assert.Equal(10, f.Touch1.X, 4);
        Assert.Equal(0, f.Touch1.Z, 4);
        Assert.Equal(14, f.Touch2.X, 4);
        Assert.Equal(0, f.Touch2.Z, 4);
    }

    [Fact]
    public void FilletCircleCircle_SixCircles_AllTangent()
    {
        GeoCircle a = new(0, 0, 10), b = new(0, 15, 10);
        var res = ArcConstructions.FilletCircleCircle(a, b, 3);

        // внеш/внеш (2) + внеш/внутр (2) + внутр/внеш (2); внутр/внутр (R−r = 7, d = 15 > 14) нет.
        Assert.Equal(6, res.Count);
        foreach (var f in res)
        {
            Assert.Equal(3, Dist(f.Touch1, f.Center), 4);
            Assert.Equal(3, Dist(f.Touch2, f.Center), 4);
            Assert.Equal(10, Dist(f.Touch1, (a.X, a.Z)), 4);
            Assert.Equal(10, Dist(f.Touch2, (b.X, b.Z)), 4);
        }
    }

    [Fact]
    public void FilletCircleCircle_EnclosingContact_TouchOnFarSideOfSourceArc()
    {
        // r = 20 > R = 10: искомая дуга охватывает исходную.
        GeoCircle a = new(0, 0, 10);
        var res = ArcConstructions.FilletCircleCircle(a, new(0, 16, 5), 20);

        var f = res.First(x => x.Contact1 == ContactKind.Encloses);
        Assert.Equal(20, Dist(f.Touch1, f.Center), 4);
        Assert.Equal(10, Dist(f.Touch1, (a.X, a.Z)), 4);
        // Центры охватывающей дуги и исходной на расстоянии r − R, касание «за» исходной дугой.
        Assert.Equal(10, Dist(f.Center, (a.X, a.Z)), 4);
    }

    [Fact]
    public void Fillet_ZeroRadius_None()
    {
        Assert.Empty(ArcConstructions.FilletCircleCircle(new(0, 0, 5), new(0, 6, 5), 0));
    }
}
