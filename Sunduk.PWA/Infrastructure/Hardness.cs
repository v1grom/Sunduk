using System;
using System.Linq;

namespace Sunduk.PWA.Infrastructure
{
    /// <summary>
    /// Приближённый перевод твёрдости сталей между шкалами Виккерса, Бринелля, Роквелла C и пределом
    /// прочности — линейная интерполяция по сравнительной таблице (DIN EN ISO 18265, нелегированные и
    /// низколегированные стали). Перевод справочный: для других материалов и состояний он не годится.
    /// </summary>
    public static class Hardness
    {
        public enum Scale { HRC, HB, HV, Strength }

        /// <summary>Строка таблицы; null — для этой твёрдости шкала не применяется.</summary>
        public sealed record Row(double HV, double? HB, double? HRC, double? Strength)
        {
            public double? this[Scale scale] => scale switch
            {
                Scale.HV => HV,
                Scale.HB => HB,
                Scale.HRC => HRC,
                _ => Strength,
            };
        }

        public static readonly Row[] Table =
        {
            new(80, 76, null, 255), new(90, 85.5, null, 285), new(100, 95, null, 320), new(110, 105, null, 350),
            new(120, 114, null, 385), new(130, 124, null, 415), new(140, 133, null, 450), new(150, 143, null, 480),
            new(160, 152, null, 510), new(170, 162, null, 545), new(180, 171, null, 575), new(190, 181, null, 610),
            new(200, 190, null, 640), new(210, 199, null, 675), new(220, 209, null, 705), new(230, 219, null, 740),
            new(240, 228, 20.3, 770), new(250, 238, 22.2, 800), new(260, 247, 24.0, 835), new(270, 257, 25.6, 865),
            new(280, 266, 27.1, 900), new(290, 276, 28.5, 930), new(300, 285, 29.8, 965), new(310, 295, 31.0, 995),
            new(320, 304, 32.2, 1030), new(330, 314, 33.3, 1060), new(340, 323, 34.4, 1095), new(350, 333, 35.5, 1125),
            new(360, 342, 36.6, 1155), new(370, 352, 37.7, 1190), new(380, 361, 38.8, 1220), new(390, 371, 39.8, 1255),
            new(400, 380, 40.8, 1290), new(410, 390, 41.8, 1320), new(420, 399, 42.7, 1350), new(430, 409, 43.6, 1385),
            new(440, 418, 44.5, 1420), new(450, 428, 45.3, 1455), new(460, 437, 46.1, 1485), new(470, 447, 46.9, 1520),
            new(480, 456, 47.7, 1555), new(490, 466, 48.4, 1595), new(500, 475, 49.1, 1630), new(510, 485, 49.8, 1665),
            new(520, 494, 50.5, 1700), new(530, 504, 51.1, 1740), new(540, 513, 51.7, 1775), new(550, 523, 52.3, 1810),
            new(560, 532, 53.0, 1845), new(570, 542, 53.6, 1880), new(580, 551, 54.1, 1920), new(590, 561, 54.7, 1955),
            new(600, 570, 55.2, 1995), new(610, 580, 55.7, 2030), new(620, 589, 56.3, 2070), new(630, 599, 56.8, 2105),
            new(640, 608, 57.3, 2145), new(650, 618, 57.8, 2180), new(660, null, 58.3, null), new(670, null, 58.8, null),
            new(680, null, 59.2, null), new(690, null, 59.7, null), new(700, null, 60.1, null), new(720, null, 61.0, null),
            new(740, null, 61.8, null), new(760, null, 62.5, null), new(780, null, 63.3, null), new(800, null, 64.0, null),
            new(820, null, 64.7, null), new(840, null, 65.3, null), new(860, null, 65.9, null), new(880, null, 66.4, null),
            new(900, null, 67.0, null), new(920, null, 67.5, null), new(940, null, 68.0, null),
        };

        /// <summary>Наименьшее и наибольшее значение шкалы в таблице.</summary>
        public static (double Min, double Max) Range(Scale scale)
        {
            var values = Table.Select(r => r[scale]).Where(v => v is not null).Select(v => v!.Value).ToArray();
            return (values[0], values[^1]);
        }

        /// <summary>
        /// Значение в шкале <paramref name="to"/> для твёрдости <paramref name="value"/> в шкале
        /// <paramref name="from"/>; null — значение вне таблицы или целевая шкала там не применяется.
        /// </summary>
        public static double? Convert(Scale from, Scale to, double value)
        {
            if (from == to) return value;
            for (var i = 0; i + 1 < Table.Length; i++)
            {
                var a = Table[i][from];
                var b = Table[i + 1][from];
                if (a is null || b is null || value < a || value > b) continue;
                var ta = Table[i][to];
                var tb = Table[i + 1][to];
                if (ta is null || tb is null)
                {
                    // на границе применимости шкалы годится только точное попадание в строку
                    if (ta is not null && Math.Abs(value - a.Value) < 1e-9) return ta;
                    if (tb is not null && Math.Abs(value - b.Value) < 1e-9) return tb;
                    return null;
                }
                var t = (value - a.Value) / (b.Value - a.Value);
                return ta + (tb - ta) * t;
            }
            return null;
        }
    }
}
