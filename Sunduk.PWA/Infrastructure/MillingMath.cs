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
        /// Наибольшая толщина стружки фрезы с малым углом в плане (high feed), мм: fz·sin κ,
        /// а при ширине резания меньше половины диаметра ещё и с радиальным утонением.
        /// NaN при некорректных данных.
        /// </summary>
        public static double HighFeedChipThickness(double diameter, double width, double leadAngle, double feedPerTooth)
        {
            if (leadAngle <= 0 || leadAngle > 90 || feedPerTooth <= 0) return double.NaN;
            var radial = width > 0 ? ChipThinningFactor(diameter, width) : 1;
            return double.IsNaN(radial) ? double.NaN : feedPerTooth * Math.Sin(leadAngle * Math.PI / 180) * radial;
        }

        /// <summary>
        /// Подача на зуб high feed фрезы, мм, дающая ту же толщину стружки, что паспортная подача
        /// при полной ширине резания: паспортная / радиальное утонение (угол в плане уже учтён в паспорте).
        /// </summary>
        public static double HighFeedCorrectedFeed(double diameter, double width, double catalogFeed)
        {
            if (catalogFeed <= 0) return double.NaN;
            var radial = width > 0 ? ChipThinningFactor(diameter, width) : 1;
            return double.IsNaN(radial) ? double.NaN : catalogFeed / radial;
        }

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

        /// <summary>
        /// Фаска по кромке контура фасочной фрезой.
        /// </summary>
        /// <param name="Depth">Глубина вершины фрезы от верхней грани, мм (в программе — Z со знаком минус).</param>
        /// <param name="Offset">Расстояние от оси фрезы до стенки: значение корректора радиуса D, мм.</param>
        /// <param name="Height">Высота фаски по стенке, мм.</param>
        /// <param name="MaxDiameter">Наибольший диаметр фрезы в работе (у верхней грани), мм.</param>
        /// <param name="MeanDiameter">Диаметр посередине фаски — по нему считают обороты, мм.</param>
        public readonly record struct EdgeChamferResult(double Depth, double Offset, double Height, double MaxDiameter, double MeanDiameter);

        /// <summary>
        /// Фаска по кромке фасочной фрезой с углом при вершине <paramref name="toolAngle"/> (90° для фаски 45°)
        /// и диаметром вершины <paramref name="tipDiameter"/> (0 — острая). Ширина фаски <paramref name="width"/>
        /// меряется по верхней грани, вершина уходит ниже фаски на <paramref name="overtravel"/>, чтобы не оставить
        /// заусенец. Ось фрезы стоит от стенки на d0/2 + Δ·tg(φ/2), вершина — на глубине b/tg(φ/2) + Δ.
        /// null при некорректных данных.
        /// </summary>
        public static EdgeChamferResult? EdgeChamfer(double toolAngle, double tipDiameter, double width, double overtravel)
        {
            if (toolAngle <= 0 || toolAngle >= 180 || tipDiameter < 0 || width <= 0 || overtravel < 0) return null;
            var tan = Math.Tan(toolAngle / 2 * Math.PI / 180);
            var height = width / tan;
            var offset = tipDiameter / 2 + overtravel * tan;
            return new EdgeChamferResult(height + overtravel, offset, height, 2 * (offset + width), 2 * offset + width);
        }

        /// <summary>
        /// Глубина, мм, на которую нужно опустить вершину фасочной фрезы (зенковки) по оси отверстия
        /// диаметра <paramref name="holeDiameter"/>, чтобы получить фаску шириной <paramref name="width"/> на сторону:
        /// (Dотв + 2·b − d0) / (2·tg(φ/2)). NaN, если вершина не входит в отверстие или данные некорректны.
        /// </summary>
        public static double HoleChamferDepth(double toolAngle, double tipDiameter, double holeDiameter, double width)
        {
            if (toolAngle <= 0 || toolAngle >= 180 || tipDiameter < 0 || holeDiameter <= 0 || width <= 0) return double.NaN;
            if (tipDiameter >= holeDiameter) return double.NaN;
            var tan = Math.Tan(toolAngle / 2 * Math.PI / 180);
            return (holeDiameter + 2 * width - tipDiameter) / (2 * tan);
        }

        /// <summary>Шаг между проходами сферической фрезы под заданную высоту гребешка, мм.</summary>
        public static double StepForScallop(double diameter, double height)
        {
            if (diameter <= 0 || height <= 0 || height >= diameter / 2) return double.NaN;
            return 2 * Math.Sqrt(height * (diameter - height));
        }
    }
}
