using System;
using System.Collections.Generic;

namespace Sunduk.PWA.Infrastructure.Sequences
{
    /// <summary>
    /// Переход с отверстиями, которые могут быть заданы сеткой. Сами координаты лежат в списке
    /// отверстий перехода; <see cref="Grid"/> хранит параметры, из которых список получен, чтобы при
    /// правке перехода (и после открытия файла работы) сетка оставалась сеткой. null — отверстия
    /// заданы иначе (в старых файлах работы свойства нет вовсе).
    /// </summary>
    public interface IHoleGridSequence
    {
        HoleGrid Grid { get; set; }
    }

    /// <summary>
    /// Отверстия сеткой: первое отверстие, шаги и количество по X и Y, поворот сетки вокруг первого
    /// отверстия. Раскладывается в обычный список координат <see cref="Hole"/>.
    /// </summary>
    public class HoleGrid
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double StepX { get; set; }
        public double StepY { get; set; }
        public int CountX { get; set; } = 2;
        public int CountY { get; set; } = 2;

        /// <summary>Поворот сетки вокруг первого отверстия, градусы против часовой стрелки.</summary>
        public double Angle { get; set; }

        /// <summary>Обход змейкой: каждый следующий ряд идёт в обратную сторону — меньше холостых перемещений.</summary>
        public bool Zigzag { get; set; } = true;

        public int Count => Math.Max(CountX, 1) * Math.Max(CountY, 1);

        public HoleGrid Clone() => (HoleGrid)MemberwiseClone();

        public List<Hole> Build()
        {
            var result = new List<Hole>();
            var countX = Math.Max(CountX, 1);
            var countY = Math.Max(CountY, 1);
            var cos = Math.Cos(Angle * Math.PI / 180);
            var sin = Math.Sin(Angle * Math.PI / 180);
            for (var row = 0; row < countY; row++)
            {
                for (var k = 0; k < countX; k++)
                {
                    var column = Zigzag && row % 2 == 1 ? countX - 1 - k : k;
                    var dx = column * StepX;
                    var dy = row * StepY;
                    // округление убирает хвосты вида 29.999999999999996 после поворота
                    result.Add(new Hole(Math.Round(X + dx * cos - dy * sin, 4), Math.Round(Y + dx * sin + dy * cos, 4)));
                }
            }
            return result;
        }
    }
}
