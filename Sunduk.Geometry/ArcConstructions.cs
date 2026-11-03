using System;
using System.Collections.Generic;

namespace Sunduk.Geometry
{
    /// <summary>Окружность в плоскости (X — РАДИУС, не диаметр; Z). Перевод диаметров — на стороне UI.</summary>
    public readonly record struct GeoCircle(double X, double Z, double R);

    /// <summary>Бесконечная прямая: точка + единичное направление (DX, DZ).</summary>
    public readonly record struct GeoLine(double X, double Z, double DX, double DZ)
    {
        /// <summary>Прямая через две точки. null — если точки совпадают.</summary>
        public static GeoLine? FromPoints(double x1, double z1, double x2, double z2)
        {
            var dx = x2 - x1;
            var dz = z2 - z1;
            var len = Math.Sqrt(dx * dx + dz * dz);
            return len < ArcConstructions.Eps ? null : new GeoLine(x1, z1, dx / len, dz / len);
        }

        /// <summary>Прямая через точку под углом к оси Z (градусы, от +Z к +X).</summary>
        public static GeoLine FromAngle(double x, double z, double angleDegrees)
        {
            var a = angleDegrees * Math.PI / 180;
            return new GeoLine(x, z, Math.Sin(a), Math.Cos(a));
        }

        /// <summary>Единичная нормаль (повёрнута на +90° от направления).</summary>
        public (double X, double Z) Normal => (DZ, -DX);

        /// <summary>Проекция точки на прямую.</summary>
        public (double X, double Z) Project(double x, double z)
        {
            var t = (x - X) * DX + (z - Z) * DZ;
            return (X + t * DX, Z + t * DZ);
        }

        /// <summary>Знаковое расстояние от точки до прямой (положительно со стороны <see cref="Normal"/>).</summary>
        public double SignedDistance(double x, double z)
        {
            var n = Normal;
            return (x - X) * n.X + (z - Z) * n.Z;
        }

        /// <summary>Прямая, смещённая на <paramref name="offset"/> вдоль <see cref="Normal"/>.</summary>
        public GeoLine Offset(double offset)
        {
            var n = Normal;
            return new GeoLine(X + n.X * offset, Z + n.Z * offset, DX, DZ);
        }
    }

    /// <summary>Взаимное расположение двух окружностей.</summary>
    public enum CircleRelation
    {
        /// <summary>Не пересекаются, лежат одна вне другой.</summary>
        Separate,
        /// <summary>Касаются снаружи (одна точка).</summary>
        ExternalTangent,
        /// <summary>Пересекаются в двух точках.</summary>
        Intersecting,
        /// <summary>Касаются изнутри (одна точка).</summary>
        InternalTangent,
        /// <summary>Одна внутри другой, без общих точек.</summary>
        Nested,
        /// <summary>Совпадают.</summary>
        Coincident
    }

    /// <summary>Взаимное расположение прямой и окружности.</summary>
    public enum LineCircleRelation { None, Tangent, Secant }

    /// <summary>Вид общей касательной двух окружностей.</summary>
    public enum CommonTangentKind { External, Internal }

    /// <summary>Касание искомой окружности (сопряжения) с исходным элементом.</summary>
    public enum ContactKind
    {
        /// <summary>С прямой.</summary>
        Line,
        /// <summary>Снаружи исходной дуги (дуги лежат рядом, не охватывая друг друга).</summary>
        Outside,
        /// <summary>Искомая дуга охватывает исходную.</summary>
        Encloses,
        /// <summary>Искомая дуга лежит внутри исходной.</summary>
        Inside
    }

    public readonly record struct CircleIntersection(
        CircleRelation Relation,
        double CenterDistance,
        IReadOnlyList<(double X, double Z)> Points);

    public readonly record struct LineCircleIntersection(
        LineCircleRelation Relation,
        double Distance,
        IReadOnlyList<(double X, double Z)> Points);

    /// <summary>Касательная из точки к окружности: точка касания и длина отрезка от точки.</summary>
    public readonly record struct PointTangent((double X, double Z) Touch, double Length);

    /// <summary>Общая касательная двух окружностей.</summary>
    public readonly record struct CommonTangent(
        CommonTangentKind Kind,
        (double X, double Z) Touch1,
        (double X, double Z) Touch2);

    /// <summary>Окружность заданного радиуса, касательная к двум исходным элементам (сопряжение).</summary>
    public readonly record struct TangentCircle(
        (double X, double Z) Center,
        double R,
        (double X, double Z) Touch1,
        (double X, double Z) Touch2,
        ContactKind Contact1,
        ContactKind Contact2);

    /// <summary>
    /// Построения по дугам и прямым: пересечения, касательные, сопряжение радиусом. Чистая
    /// математика в плоскости (X — радиус, Z); диаметры переводит вызывающий код. Допуск
    /// <see cref="Eps"/> (мм) отделяет «касание» от «пересечения»/«непересечения».
    /// </summary>
    public static class ArcConstructions
    {
        /// <summary>Допуск совпадения/касания, мм.</summary>
        public const double Eps = 1e-6;

        // ------------------------------------------------------------------ пересечения

        /// <summary>Пересечение двух окружностей. Точки упорядочены: сначала слева от вектора c1→c2
        /// (поворот на +90° в системе (X, Z)), затем справа.</summary>
        public static CircleIntersection IntersectCircles(GeoCircle a, GeoCircle b)
        {
            var dx = b.X - a.X;
            var dz = b.Z - a.Z;
            var d = Math.Sqrt(dx * dx + dz * dz);

            if (d < Eps)
            {
                return new(Math.Abs(a.R - b.R) < Eps ? CircleRelation.Coincident : CircleRelation.Nested, d, []);
            }

            var sum = a.R + b.R;
            var diff = Math.Abs(a.R - b.R);

            if (d > sum + Eps) return new(CircleRelation.Separate, d, []);
            if (d < diff - Eps) return new(CircleRelation.Nested, d, []);

            var ex = dx / d;
            var ez = dz / d;
            var l = (a.R * a.R - b.R * b.R + d * d) / (2 * d);
            var px = a.X + l * ex;
            var pz = a.Z + l * ez;

            if (Math.Abs(d - sum) <= Eps) return new(CircleRelation.ExternalTangent, d, [(px, pz)]);
            if (Math.Abs(d - diff) <= Eps) return new(CircleRelation.InternalTangent, d, [(a.X + Math.Sign(a.R - b.R) * a.R * ex, a.Z + Math.Sign(a.R - b.R) * a.R * ez)]);

            var h = Math.Sqrt(Math.Max(0, a.R * a.R - l * l));
            // нормаль (-ez, ex): «влево» от направления c1→c2
            return new(CircleRelation.Intersecting, d, [(px - h * ez, pz + h * ex), (px + h * ez, pz - h * ex)]);
        }

        /// <summary>Пересечение прямой и окружности. Точки упорядочены по направлению прямой.</summary>
        public static LineCircleIntersection IntersectLineCircle(GeoLine line, GeoCircle c)
        {
            var dist = Math.Abs(line.SignedDistance(c.X, c.Z));
            if (dist > c.R + Eps) return new(LineCircleRelation.None, dist, []);

            var foot = line.Project(c.X, c.Z);
            if (Math.Abs(dist - c.R) <= Eps) return new(LineCircleRelation.Tangent, dist, [foot]);

            var h = Math.Sqrt(Math.Max(0, c.R * c.R - dist * dist));
            return new(LineCircleRelation.Secant, dist,
                [(foot.X - h * line.DX, foot.Z - h * line.DZ), (foot.X + h * line.DX, foot.Z + h * line.DZ)]);
        }

        /// <summary>Пересечение двух прямых. null — параллельны (или совпадают).</summary>
        public static (double X, double Z)? IntersectLines(GeoLine a, GeoLine b)
        {
            var cross = a.DX * b.DZ - a.DZ * b.DX;
            if (Math.Abs(cross) < 1e-12) return null;
            var t = ((b.X - a.X) * b.DZ - (b.Z - a.Z) * b.DX) / cross;
            return (a.X + t * a.DX, a.Z + t * a.DZ);
        }

        // ------------------------------------------------------------------ касательные

        /// <summary>Касательные из точки к окружности: 0 (точка внутри), 1 (точка на окружности) или 2.</summary>
        public static IReadOnlyList<PointTangent> TangentsFromPoint(double x, double z, GeoCircle c)
        {
            var dx = x - c.X;
            var dz = z - c.Z;
            var d = Math.Sqrt(dx * dx + dz * dz);
            if (c.R < Eps || d < c.R - Eps) return [];
            if (Math.Abs(d - c.R) <= Eps) return [new((x, z), 0)];

            var ux = dx / d;
            var uz = dz / d;
            var alpha = Math.Acos(Math.Clamp(c.R / d, -1, 1));
            var length = Math.Sqrt(Math.Max(0, d * d - c.R * c.R));
            var (sin, cos) = Math.SinCos(alpha);

            (double X, double Z) Touch(double s) =>
                (c.X + c.R * (ux * cos - s * uz * sin), c.Z + c.R * (s * ux * sin + uz * cos));

            return [new(Touch(1), length), new(Touch(-1), length)];
        }

        /// <summary>Общие касательные двух окружностей: до 2 внешних и до 2 внутренних.
        /// Каждый вид при касании окружностей вырождается в одну прямую.</summary>
        public static IReadOnlyList<CommonTangent> CommonTangents(GeoCircle a, GeoCircle b)
        {
            var dx = b.X - a.X;
            var dz = b.Z - a.Z;
            var d = Math.Sqrt(dx * dx + dz * dz);
            var result = new List<CommonTangent>();
            if (d < Eps) return result;

            var ux = dx / d;
            var uz = dz / d;

            foreach (var (s, kind) in new[] { (1, CommonTangentKind.External), (-1, CommonTangentKind.Internal) })
            {
                // n·D = s·r2 − r1  →  cos φ = (s·r2 − r1) / d
                var cos = (s * b.R - a.R) / d;
                if (Math.Abs(cos) > 1 + Eps / d) continue;
                cos = Math.Clamp(cos, -1, 1);
                var sin = Math.Sqrt(Math.Max(0, 1 - cos * cos));
                var single = sin * d < Eps;

                foreach (var sign in single ? new[] { 1 } : new[] { 1, -1 })
                {
                    var nx = ux * cos - sign * uz * sin;
                    var nz = sign * ux * sin + uz * cos;
                    result.Add(new(kind,
                        (a.X - a.R * nx, a.Z - a.R * nz),
                        (b.X - s * b.R * nx, b.Z - s * b.R * nz)));
                }
            }
            return result;
        }

        // ------------------------------------------------------------------ сопряжение радиусом

        /// <summary>
        /// Окружности радиуса <paramref name="r"/>, касательные к двум окружностям. Центр искомой
        /// окружности лежит на «смещённой» окружности (радиус R+r или |R−r|) каждого элемента —
        /// пересекаем смещённые кривые во всех сочетаниях.
        /// </summary>
        public static IReadOnlyList<TangentCircle> FilletCircleCircle(GeoCircle a, GeoCircle b, double r)
        {
            var result = new List<TangentCircle>();
            if (r < Eps) return result;

            foreach (var la in CircleLoci(a, r))
            foreach (var lb in CircleLoci(b, r))
            {
                foreach (var p in IntersectCircles(la.Locus, lb.Locus).Points)
                {
                    AddUnique(result, new TangentCircle(p, r,
                        TouchOnCircle(a, p, la.Contact), TouchOnCircle(b, p, lb.Contact),
                        la.Contact, lb.Contact));
                }
            }
            return result;
        }

        /// <summary>Окружности радиуса <paramref name="r"/>, касательные к окружности и прямой.</summary>
        public static IReadOnlyList<TangentCircle> FilletCircleLine(GeoCircle a, GeoLine line, double r)
        {
            var result = new List<TangentCircle>();
            if (r < Eps) return result;

            foreach (var la in CircleLoci(a, r))
            foreach (var side in new[] { r, -r })
            {
                foreach (var p in IntersectLineCircle(line.Offset(side), la.Locus).Points)
                {
                    AddUnique(result, new TangentCircle(p, r,
                        TouchOnCircle(a, p, la.Contact), line.Project(p.X, p.Z),
                        la.Contact, ContactKind.Line));
                }
            }
            return result;
        }

        /// <summary>Окружности радиуса <paramref name="r"/>, касательные к двум прямым (до 4 при
        /// пересекающихся прямых, пусто для параллельных с зазором ≠ 2r).</summary>
        public static IReadOnlyList<TangentCircle> FilletLineLine(GeoLine a, GeoLine b, double r)
        {
            var result = new List<TangentCircle>();
            if (r < Eps) return result;

            foreach (var sa in new[] { r, -r })
            foreach (var sb in new[] { r, -r })
            {
                if (IntersectLines(a.Offset(sa), b.Offset(sb)) is not { } p) continue;
                AddUnique(result, new TangentCircle(p, r,
                    a.Project(p.X, p.Z), b.Project(p.X, p.Z), ContactKind.Line, ContactKind.Line));
            }
            return result;
        }

        private static IEnumerable<(GeoCircle Locus, ContactKind Contact)> CircleLoci(GeoCircle c, double r)
        {
            yield return (new GeoCircle(c.X, c.Z, c.R + r), ContactKind.Outside);
            // |R − r|: при r > R искомая дуга охватывает исходную, при r < R лежит внутри неё.
            // r == R даёт вырожденную окружность нулевого радиуса (центр совпадает) — пропускаем.
            if (Math.Abs(c.R - r) < Eps) yield break;
            yield return (new GeoCircle(c.X, c.Z, Math.Abs(c.R - r)), r > c.R ? ContactKind.Encloses : ContactKind.Inside);
        }

        private static (double X, double Z) TouchOnCircle(GeoCircle c, (double X, double Z) center, ContactKind contact)
        {
            var dx = center.X - c.X;
            var dz = center.Z - c.Z;
            var d = Math.Sqrt(dx * dx + dz * dz);
            if (d < Eps) return (c.X, c.Z);
            // Охватывающая дуга касается исходной со стороны, противоположной своему центру.
            var k = (contact == ContactKind.Encloses ? -1 : 1) * c.R / d;
            return (c.X + dx * k, c.Z + dz * k);
        }

        private static void AddUnique(List<TangentCircle> list, TangentCircle item)
        {
            foreach (var e in list)
            {
                if (Math.Abs(e.Center.X - item.Center.X) < Eps && Math.Abs(e.Center.Z - item.Center.Z) < Eps
                    && e.Contact1 == item.Contact1 && e.Contact2 == item.Contact2) return;
            }
            list.Add(item);
        }
    }
}
