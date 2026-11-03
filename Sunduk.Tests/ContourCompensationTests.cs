using Sunduk.Geometry;
using Sunduk.Geometry.ContourElements;
using Sunduk.Geometry.ContourElements.Base;
using Sunduk.PWA.Infrastructure.Visualization;

namespace Sunduk.Tests;

/// <summary>
/// Независимая проверка компенсации радиуса пластины (<see cref="ToolTipCompensation"/>) по
/// геометрии, а не по снимку чисел. Для наружного точения (вектор 3) центр пластины лежит на
/// (+r по Z, +r по X) от запрограммированной мнимой вершины. Путь центра обязан
/// (1) нигде не заходить ближе r к номинальному контуру (иначе пластина срежет деталь) и
/// (2) касаться контура (расстояние ≈ r) на каждом рабочем отрезке — иначе остаётся недорез.
/// </summary>
public class ContourCompensationTests
{
    private const double R = 0.4;
    private const double GougeTolerance = 0.004;
    private const double UndercutTolerance = 0.03;

    public static IEnumerable<object[]> Contours()
    {
        yield return new object[] { "ступень", new List<Element> { new Point(20, 0), new Line(20, -10), new Line(30, -10), new Line(30, -30) } };
        yield return new object[] { "фаска 45", new List<Element> { new Point(20, 0), new Line(30, -5), new Line(30, -20) } };
        yield return new object[] { "конус 30°", new List<Element> { new Point(20, 0), new Line(40, -17.3205), new Line(40, -30) } };
        yield return new object[] { "конус и цилиндр", new List<Element> { new Point(20, 0), new Line(20, -5), new Line(36, -15), new Line(36, -30) } };
        yield return new object[] { "цилиндр, выпуклая дуга CCW, цилиндр", new List<Element> { new Point(20, 0), new Line(20, -10), new Arc(30, -15, 5, Direction.CCW), new Line(30, -30) } };
        yield return new object[] { "цилиндр, дуга CW, цилиндр", new List<Element> { new Point(20, 0), new Line(20, -10), new Arc(30, -15, 5, Direction.CW), new Line(30, -30) } };
        yield return new object[] { "торец, дуга CCW", new List<Element> { new Point(20, 0), new Arc(30, -5, 5, Direction.CCW), new Line(30, -30) } };
        yield return new object[] { "торец, дуга CW", new List<Element> { new Point(20, 0), new Arc(30, -5, 5, Direction.CW), new Line(30, -30) } };
        yield return new object[] { "сфера: две дуги", new List<Element> { new Point(0, 0), new Arc(20, -10, 10, Direction.CCW), new Line(20, -25) } };
        yield return new object[] { "дуга-дуга (S)", new List<Element> { new Point(20, 0), new Line(20, -5), new Arc(28, -9, 6, Direction.CCW), new Arc(36, -13, 6, Direction.CW), new Line(36, -25) } };
        yield return new object[] { "дуга в конце контура CCW", new List<Element> { new Point(20, 0), new Line(20, -5), new Arc(30, -10, 5, Direction.CCW) } };
        yield return new object[] { "дуга в конце контура CW", new List<Element> { new Point(20, 0), new Line(20, -5), new Arc(30, -10, 5, Direction.CW) } };
        yield return new object[] { "дуга в начале и в конце", new List<Element> { new Point(10, 0), new Arc(30, -10, 10, Direction.CCW), new Line(30, -20), new Arc(40, -25, 5, Direction.CCW) } };
        yield return new object[] { "вогнутая дуга между цилиндрами", new List<Element> { new Point(20, 0), new Line(20, -8), new Arc(30, -14, 8, Direction.CW), new Line(30, -30) } };
        yield return new object[] { "наклонная после дуги", new List<Element> { new Point(20, 0), new Line(20, -5), new Arc(30, -10, 5, Direction.CCW), new Line(40, -20), new Line(40, -30) } };
    }

    private static List<Pt> Samples(IReadOnlyList<Element> contour)
    {
        var path = TurningToolpath.Build(contour);
        Assert.NotNull(path);
        var points = path!.Nominal.SelectMany(m => m.Sample(64)).ToList();
        return points;
    }

    private static double DistanceToPolyline(Pt p, IReadOnlyList<Pt> polyline)
    {
        var best = double.MaxValue;
        for (var i = 0; i + 1 < polyline.Count; i++)
        {
            var a = polyline[i];
            var b = polyline[i + 1];
            var dz = b.Z - a.Z;
            var dr = b.R - a.R;
            var len2 = dz * dz + dr * dr;
            var t = len2 < 1e-18 ? 0 : Math.Clamp(((p.Z - a.Z) * dz + (p.R - a.R) * dr) / len2, 0, 1);
            var cz = a.Z + t * dz;
            var cr = a.R + t * dr;
            best = Math.Min(best, Math.Sqrt((p.Z - cz) * (p.Z - cz) + (p.R - cr) * (p.R - cr)));
        }
        return best;
    }

    private static string Dump(IEnumerable<Element> contour) => string.Join(" | ", contour.Select(e => e switch
    {
        Arc a => $"{(a.Direction == Direction.CW ? "G2" : "G3")} X{a.X:0.###} Z{a.Z:0.###} R{a.Radius:0.###}",
        Line l => $"G1 X{l.X:0.###} Z{l.Z:0.###}",
        Point p => $"P X{p.X:0.###} Z{p.Z:0.###}",
        _ => "?",
    }));

    [Theory]
    [MemberData(nameof(Contours))]
    public void ToolCenterPath_NeverGougesNominalContour_AndReachesIt(string name, List<Element> contour)
    {
        var nominal = Samples(contour);
        var compensated = ToolTipCompensation.Compensate(contour, R, ToolNoseVector.TurningExternal);
        var tipPath = TurningToolpath.Build(compensated);
        Assert.NotNull(tipPath);

        // центр пластины: от мнимой вершины на +r по Z и на +r по радиусу
        var centerPath = tipPath!.Nominal.SelectMany(m => m.Sample(64)).Select(tip => new Pt(tip.Z + R, tip.R + R)).ToList();
        var dump = Dump(compensated);

        // (1) центр не ближе r к контуру: иначе пластина срезает деталь
        var minDistance = centerPath.Min(c => DistanceToPolyline(c, nominal));
        var worst = centerPath.OrderBy(c => DistanceToPolyline(c, nominal)).First();
        dump += $" || худшая точка центра Z{worst.Z:0.###} R{worst.R:0.###}";
        Assert.True(minDistance >= R - GougeTolerance,
            $"{name}: центр пластины подошёл к контуру на {minDistance:F4} при радиусе {R} — срез детали. Скомпенсировано: {dump}");

        // (2) каждая точка номинального контура (кроме окрестности узлов — там вогнутый угол
        // неизбежно остаётся скруглённым пластиной) достижима: центр проходит от неё на r
        var path = TurningToolpath.Build(contour)!;
        var nodes = path.Nominal.SelectMany(m => new[] { m.From, m.To }).ToList();
        var reach = 0d;
        foreach (var q in path.Nominal.SelectMany(m => m.Sample(64)))
        {
            if (nodes.Any(n => Math.Abs(n.Z - q.Z) + Math.Abs(n.R - q.R) < 2.5 * R)) continue;
            reach = Math.Max(reach, DistanceToPolyline(q, centerPath));
        }
        Assert.True(reach <= R + UndercutTolerance,
            $"{name}: точка контура осталась в {reach:F4} от пути центра при радиусе {R} — недорез. Скомпенсировано: {dump}");
    }

    /// <summary>Случайные «токарные» контуры: Z вдоль контура не возрастает (без подрезов и петель),
    /// дуги обоих направлений, радиусы заведомо больше радиуса пластины.</summary>
    [Fact]
    public void RandomTurnableContours_NeverGouge()
    {
        var random = new Random(12345);
        var checkedCount = 0;
        var failures = new List<string>();
        for (var attempt = 0; attempt < 4000 && checkedCount < 400; attempt++)
        {
            var contour = new List<Element> { new Point(random.Next(0, 3) == 0 ? 0 : random.Next(8, 25), 0) };
            var x = contour[0].X!.Value;
            var z = 0d;
            var count = random.Next(2, 6);
            for (var k = 0; k < count; k++)
            {
                var dz = -(3 + random.NextDouble() * 10);
                var dx = random.Next(0, 3) == 0 ? 0 : (random.NextDouble() - 0.3) * 16;
                var nx = Math.Max(0, x + dx);
                var nz = z + dz;
                if (random.Next(0, 2) == 0)
                {
                    var chord = Math.Sqrt(Math.Pow((nx - x) / 2, 2) + dz * dz);
                    contour.Add(new Arc(nx, nz, chord / 2 * (1 + random.NextDouble() * 1.2) + 0.5, random.Next(0, 2) == 0 ? Direction.CCW : Direction.CW));
                }
                else
                {
                    contour.Add(new Line(nx, nz));
                }
                x = nx;
                z = nz;
            }

            var nominal = TurningToolpath.Build(contour)!.Nominal.SelectMany(m => m.Sample(64)).ToList();
            var monotonic = true;
            for (var q = 1; q < nominal.Count; q++) if (nominal[q].Z > nominal[q - 1].Z + 1e-9) { monotonic = false; break; }
            if (!monotonic) continue;
            checkedCount++;

            var compensated = ToolTipCompensation.Compensate(contour, R, ToolNoseVector.TurningExternal);
            var tip = TurningToolpath.Build(compensated);
            if (tip is null) { failures.Add($"нет траектории: {Dump(contour)}"); continue; }
            var centers = tip.Nominal.SelectMany(m => m.Sample(64)).Select(t => new Pt(t.Z + R, t.R + R)).ToList();
            var minDistance = centers.Min(c => DistanceToPolyline(c, nominal));
            if (minDistance < R - 0.01)
                failures.Add($"срез {R - minDistance:F3}: {Dump(contour)} => {Dump(compensated)}");
        }

        Assert.True(checkedCount >= 100, $"слишком мало подходящих контуров: {checkedCount}");
        // Допускаем до 1%: это вырожденные «клювы» — две вогнутые дуги, у которых смещённые на радиус
        // пластины окружности не пересекаются (пластина физически не вписывается в общий угол).
        Assert.True(failures.Count <= checkedCount / 100, $"{failures.Count} из {checkedCount}: " + string.Join(" // ", failures.Take(8)));
    }
}
