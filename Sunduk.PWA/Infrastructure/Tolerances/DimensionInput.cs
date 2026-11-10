using System.Globalization;
using System.Text.RegularExpressions;

namespace Sunduk.PWA.Infrastructure.Tolerances
{
    /// <summary>
    /// Ввод размера с допуском: в поле координаты можно написать размер так, как он стоит на чертеже,
    /// а в координату попадёт середина поля допуска — то, что и надо программировать.
    /// <list type="bullet">
    /// <item><c>30h7</c>, <c>30H7</c>, <c>12js6</c> — посадка по ЕСДП;</item>
    /// <item><c>30+0.1/-0.05</c>, <c>30-0.02/-0.05</c>, <c>30+0.1/0</c> — два отклонения через дробь;</item>
    /// <item><c>45±0.1</c>, <c>45+-0.1</c> — симметричный допуск (середина — сам номинал).</item>
    /// </list>
    /// Знак размера сохраняется: <c>-25h11</c> — это размер 25h11, отложенный в минус (координата Z).
    /// </summary>
    public static class DimensionInput
    {
        private const string Number = @"\d+(?:\.\d+)?";

        private static readonly Regex Fit = new($@"^(-?)({Number})\s*([A-Za-z]{{1,2}}\d{{1,2}})$", RegexOptions.Compiled);
        private static readonly Regex Symmetric = new($@"^(-?{Number})\s*(?:±|\+-|\+/-)\s*{Number}$", RegexOptions.Compiled);
        private static readonly Regex Deviations = new($@"^(-?)({Number})\s*([+-]{Number})\s*/\s*([+-]?{Number})$", RegexOptions.Compiled);
        private static readonly Regex Leading = new($@"^-?{Number}", RegexOptions.Compiled);

        /// <summary>
        /// Значение координаты по введённому тексту. Пустая строка и одиночный минус — null (поле не
        /// заполнено). Недописанный допуск (<c>30h</c>, <c>30+0.1/</c>) даёт число в начале строки,
        /// чтобы контур не прыгал в ноль, пока текст набирается; строка без числа — 0.
        /// </summary>
        public static double? Parse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var s = text.Trim().Replace(',', '.');
            if (s is "-") return null;

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var plain)) return plain;

            var match = Fit.Match(s);
            if (match.Success && Tolerance.TryCalculate(Value(match.Groups[2]), match.Groups[3].Value, out var result, out _))
                return Sign(match.Groups[1]) * result.MiddleSize;

            match = Symmetric.Match(s);
            if (match.Success) return Value(match.Groups[1]);

            match = Deviations.Match(s);
            if (match.Success)
                return Sign(match.Groups[1]) * (Value(match.Groups[2]) + (Value(match.Groups[3]) + Value(match.Groups[4])) / 2);

            match = Leading.Match(s);
            return match.Success ? double.Parse(match.Value, CultureInfo.InvariantCulture) : 0;
        }

        private static double Value(Group group) => double.Parse(group.Value, NumberStyles.Float, CultureInfo.InvariantCulture);

        private static int Sign(Group group) => group.Value == "-" ? -1 : 1;
    }
}
