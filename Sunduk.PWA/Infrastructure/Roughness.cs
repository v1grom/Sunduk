using System;

namespace Sunduk.PWA.Infrastructure
{
    /// <summary>
    /// Шероховатость точёной поверхности: геометрический след радиуса пластины при подаче на оборот
    /// и приближённый пересчёт Ra ↔ Rz. Все высоты — в микрометрах, подача и радиус — в миллиметрах.
    /// </summary>
    public static class Roughness
    {
        /// <summary>
        /// Отношение Ra к высоте гребешка для следа радиуса (профиль — дуги, близкие к параболам):
        /// 8 / (18·√3) ≈ 0.2566.
        /// </summary>
        public const double RaToHeight = 0.2566;

        /// <summary>Граница правила пересчёта: от Ra 2.5 (Rz 10) и грубее Rz ≈ 4·Ra, тоньше — Rz ≈ 5·Ra.</summary>
        public const double RaRuleBorder = 2.5;

        /// <summary>
        /// Высота гребешка (теоретическая Rz), мкм: r − √(r² − f²/4). NaN, если подача не меньше
        /// двух радиусов — след радиуса уже не перекрывается.
        /// </summary>
        public static double TheoreticalRz(double feed, double radius)
        {
            if (feed <= 0 || radius <= 0 || feed >= 2 * radius) return double.NaN;
            return (radius - Math.Sqrt(radius * radius - feed * feed / 4)) * 1000;
        }

        /// <summary>Теоретическая Ra, мкм — <see cref="RaToHeight"/> от высоты гребешка.</summary>
        public static double TheoreticalRa(double feed, double radius) => TheoreticalRz(feed, radius) * RaToHeight;

        /// <summary>Подача, мм/об, при которой высота гребешка равна <paramref name="rz"/> мкм.</summary>
        public static double FeedForRz(double rz, double radius)
        {
            var height = rz / 1000;
            if (height <= 0 || radius <= 0 || height >= radius) return double.NaN;
            return 2 * Math.Sqrt(2 * radius * height - height * height);
        }

        /// <summary>Подача, мм/об, под заданную теоретическую Ra, мкм.</summary>
        public static double FeedForRa(double ra, double radius) => FeedForRz(ra / RaToHeight, radius);

        /// <summary>Приближённый пересчёт Ra → Rz по ГОСТ 2789: ×4 для грубых поверхностей, ×5 для тонких.</summary>
        public static double RaToRz(double ra) => ra >= RaRuleBorder ? ra * 4 : ra * 5;

        /// <summary>Приближённый пересчёт Rz → Ra, обратный <see cref="RaToRz"/>.</summary>
        public static double RzToRa(double rz) => rz >= RaRuleBorder * 4 ? rz / 4 : rz / 5;

        /// <summary>Класс чистоты по ГОСТ 2789-59 с наибольшими Ra и Rz этого класса, мкм.</summary>
        public sealed record Class(int Number, double Ra, double Rz);

        /// <summary>Классы чистоты ∇1–∇14 (ГОСТ 2789-59).</summary>
        public static readonly Class[] Classes =
        {
            new(1, 80, 320), new(2, 40, 160), new(3, 20, 80), new(4, 10, 40), new(5, 5, 20),
            new(6, 2.5, 10), new(7, 1.25, 6.3), new(8, 0.63, 3.2), new(9, 0.32, 1.6), new(10, 0.16, 0.8),
            new(11, 0.08, 0.4), new(12, 0.04, 0.2), new(13, 0.02, 0.1), new(14, 0.01, 0.05),
        };

        /// <summary>Степень шероховатости ISO 1302 (N1–N12) и её Ra, мкм.</summary>
        public sealed record Grade(string Name, double Ra);

        /// <summary>Степени N12–N1 по ISO 1302, от грубой к тонкой.</summary>
        public static readonly Grade[] Grades =
        {
            new("N12", 50), new("N11", 25), new("N10", 12.5), new("N9", 6.3), new("N8", 3.2), new("N7", 1.6),
            new("N6", 0.8), new("N5", 0.4), new("N4", 0.2), new("N3", 0.1), new("N2", 0.05), new("N1", 0.025),
        };
    }
}
