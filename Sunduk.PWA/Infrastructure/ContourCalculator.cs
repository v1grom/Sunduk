using System;
using System.Collections.Generic;
using Sunduk.Geometry;
using Sunduk.Geometry.ContourElements;
using Sunduk.Geometry.ContourElements.Base;

namespace Sunduk.PWA.Infrastructure
{
    /// <summary>
    /// Кнопка «Рассчитать» в редакторе контура: дополняет пустые поля элемента. X в контуре —
    /// диаметр, поэтому в геометрии он делится пополам. Угол линии — к оси Z, как у фасок во всём
    /// приложении: tg угла = Δ радиуса / ΔZ.
    /// </summary>
    public static class ContourCalculator
    {
        /// <summary>Дополняет элемент <paramref name="index"/>. Возвращает предупреждение для
        /// пользователя или null, если всё посчитано.</summary>
        public static string? Calculate(List<Element> contour, int index)
        {
            if (contour is null || index < 0 || index >= contour.Count) return "Нечего считать";
            var element = contour[index];
            var prev = index > 0 ? contour[index - 1] : null;
            var next = index + 1 < contour.Count ? contour[index + 1] : null;

            switch (element)
            {
                case Point point:
                    return CalculatePoint(point, prev, next);
                case Line line:
                    return CalculateLine(line, prev);
                case Arc arc:
                    return CalculateArc(arc, prev);
                default:
                    return null;
            }
        }

        private static string? CalculatePoint(Point point, Element? prev, Element? next)
        {
            // Стартовая точка без координат берётся по следующему узлу: угол детали — это его X на
            // торце (Z = 0). Скругление R относится к самому узлу и на его координаты не влияет.
            if (point.X is null && prev?.X is not null) point.X = prev.X;
            if (point.Z is null && prev?.Z is not null) point.Z = prev.Z;
            if (point.X is null && next?.X is not null) point.X = next.X;
            if (point.Z is null) point.Z = prev?.Z ?? 0;
            return null;
        }

        private static string? CalculateLine(Line line, Element? prev)
        {
            var oneKnown = (line.X is null) != (line.Z is null);
            if (line.Angle != 0 && oneKnown)
            {
                var angle = Math.Abs(line.Angle);
                if (angle >= 90) return "Угол должен быть в диапазоне от −90° до 90° (кроме 0)";
                if (prev?.X is null || prev.Z is null) return "Для расчёта по углу нужна предыдущая точка с координатами X и Z";

                // Δрадиуса = ΔZ · tg(угла). Знак угла — направление: положительный — диаметр растёт
                // при движении к −Z (как у фаски на торце), отрицательный — убывает.
                var tan = Math.Tan(angle.Radians());
                var grows = line.Angle > 0 ? -1.0 : 1.0; // знак dZ, при котором диаметр растёт
                if (line.X is not null)
                {
                    var dRadius = (line.X.Value - prev.X.Value) / 2;
                    if (Math.Abs(dRadius) < 1e-12) return "Диаметр не меняется — угол не определяет Z";
                    var dZ = Math.Abs(dRadius / tan);
                    // диаметр растёт → идём в сторону grows, убывает → в противоположную
                    line.Z = prev.Z.Value + (dRadius > 0 ? grows : -grows) * dZ;
                }
                else
                {
                    var dZ = line.Z!.Value - prev.Z.Value;
                    if (Math.Abs(dZ) < 1e-12) return "Z не меняется — угол не определяет X";
                    var dRadius = Math.Abs(dZ * tan);
                    var growing = Math.Sign(dZ) == Math.Sign(grows);
                    line.X = prev.X.Value + 2 * (growing ? dRadius : -dRadius);
                }
                return null;
            }

            if (line.X is null && prev?.X is not null) line.X = prev.X;
            if (line.Z is null && prev?.Z is not null) line.Z = prev.Z;
            return null;
        }

        private static string? CalculateArc(Arc arc, Element? prev)
        {
            if (arc.X is null && prev?.X is not null) arc.X = prev.X;
            if (arc.Z is null && prev?.Z is not null) arc.Z = prev.Z;
            if (prev?.X is null || prev.Z is null || arc.X is null || arc.Z is null) return "Для расчёта центра нужна предыдущая точка с координатами";
            if (arc.Radius <= 0) return "Радиус дуги должен быть больше нуля";

            var center = ArcConstructions.ArcCenter(prev.X.Value / 2, prev.Z.Value, arc.X.Value / 2, arc.Z.Value, arc.Radius, arc.Direction);
            if (center is null) return "Радиус дуги меньше половины хорды";
            arc.CenterX = center.Value.X * 2; // в контуре X — диаметр
            arc.CenterZ = center.Value.Z;
            return null;
        }
    }
}
