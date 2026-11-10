using System;

namespace Sunduk.PWA.Infrastructure
{
    /// <summary>
    /// Фрезерные расчёты: утонение стружки при малой ширине резания, эффективный диаметр сферической
    /// фрезы и высота гребешка между проходами. Размеры — в миллиметрах.
    /// </summary>
    public static class MillingMath
    {
        /// <summary>
        /// Во сколько раз наибольшая толщина стружки меньше подачи на зуб при ширине резания
        /// <paramref name="width"/> фрезой диаметра <paramref name="diameter"/>: 2·√(ae·(D − ae)) / D.
        /// От половины диаметра и шире утонения нет — 1. NaN при некорректных данных.
        /// </summary>
        public static double ChipThinningFactor(double diameter, double width)
        {
            if (diameter <= 0 || width <= 0 || width > diameter) return double.NaN;
            if (width >= diameter / 2) return 1;
            return 2 * Math.Sqrt(width * (diameter - width)) / diameter;
        }

        /// <summary>
        /// Подача на зуб, при которой наибольшая толщина стружки равна <paramref name="chipThickness"/>
        /// (обычно это паспортная подача на зуб для полной ширины резания).
        /// </summary>
        public static double FeedForChipThickness(double diameter, double width, double chipThickness)
            => chipThickness / ChipThinningFactor(diameter, width);

        /// <summary>
        /// Диаметр, которым реально режет сферическая фреза при глубине <paramref name="depth"/>:
        /// 2·√(ap·(D − ap)); от половины диаметра и глубже — полный диаметр.
        /// </summary>
        public static double BallEffectiveDiameter(double diameter, double depth)
        {
            if (diameter <= 0 || depth <= 0) return double.NaN;
            if (depth >= diameter / 2) return diameter;
            return 2 * Math.Sqrt(depth * (diameter - depth));
        }

        /// <summary>Обороты шпинделя для скорости резания, м/мин, на диаметре, мм.</summary>
        public static double SpindleSpeed(double cuttingSpeed, double diameter)
            => diameter > 0 && cuttingSpeed > 0 ? 1000 * cuttingSpeed / (Math.PI * diameter) : double.NaN;

        /// <summary>
        /// Высота гребешка, мм, между проходами сферической фрезы диаметра <paramref name="diameter"/>
        /// с шагом <paramref name="step"/> по плоской поверхности: R − √(R² − шаг²/4).
        /// </summary>
        public static double ScallopHeight(double diameter, double step)
        {
            var radius = diameter / 2;
            if (radius <= 0 || step <= 0 || step >= diameter) return double.NaN;
            return radius - Math.Sqrt(radius * radius - step * step / 4);
        }

        /// <summary>Шаг между проходами сферической фрезы под заданную высоту гребешка, мм.</summary>
        public static double StepForScallop(double diameter, double height)
        {
            if (diameter <= 0 || height <= 0 || height >= diameter / 2) return double.NaN;
            return 2 * Math.Sqrt(height * (diameter - height));
        }
    }
}
