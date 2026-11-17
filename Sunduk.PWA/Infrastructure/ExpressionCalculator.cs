using System;
using System.Collections.Generic;
using System.Linq;
using NCalc;
using Sunduk.PWA.Infrastructure.Extensions;

namespace Sunduk.PWA.Infrastructure
{
    /// <summary>Тригонометрические кнопки калькулятора выражений.</summary>
    public enum TrigonometricFunction
    {
        Sin,
        Cos,
        Tan,
        Atan
    }

    /// <summary>
    /// Вычисление выражений калькулятора: общая логика страницы «Режимы» и окна калькулятора
    /// в верхней панели.
    /// </summary>
    public static class ExpressionCalculator
    {
        private const string RadianToDegreeFunction = "Deg";
        private const string DegreeToRadianFunction = "Rad";

        /// <summary>
        /// Вычисляет выражение. При успехе возвращает true и результат в виде строки,
        /// иначе false и текст ошибки.
        /// </summary>
        public static bool TryEvaluate(string expression, out string result, out string error)
        {
            result = string.Empty;
            error = string.Empty;
            List<string> calcErrors = new();
            try
            {
                var userExpr = expression.Trim().Replace("𝜋", Math.PI.ToString()).Replace("²", "**2").Replace("^", "**").Replace(',', '.');
                var expr = new Expression(userExpr, ExpressionOptions.IgnoreCaseAtBuiltInFunctions | ExpressionOptions.CaseInsensitiveStringComparer);
                expr.Functions[DegreeToRadianFunction] = (args) =>
                {
                    if (args.Count() > 1) Util.ThrowLogged<ArgumentOutOfRangeException>($"Слишком много аргументов для функции {DegreeToRadianFunction}", calcErrors);
                    return ToDouble(args.Evaluate(0)) * Math.PI / 180;
                };
                expr.Functions[RadianToDegreeFunction] = (args) =>
                {
                    if (args.Count() > 1) Util.ThrowLogged<ArgumentOutOfRangeException>($"Слишком много аргументов для функции {RadianToDegreeFunction}", calcErrors);
                    return ToDouble(args.Evaluate(0)) * 180 / Math.PI;
                };
                if (expr.HasErrors())
                {
                    error = expr.Error.Message;
                    return false;
                }
                var r = expr.Evaluate();
                if (r is null)
                {
                    error = "Не удалось вычислить";
                    return false;
                }
                result = r is double d ? d.ToPrettyString(stringOption: Util.PrettyStringOption.AsIs) :
                         r is int i ? i.ToString() :
                         r is long l ? l.ToString() :
                         r?.ToString() ?? "[?]";
                return true;
            }
            catch
            {
                error = $"Ошибки при вычислении: {string.Join(", ", calcErrors)}";
                return false;
            }
        }

        private static double ToDouble(object? value) => value switch
        {
            double d => d,
            int i => i,
            long l => l,
            string s => Util.ToDouble(s.Replace(',', '.')),
            _ => throw new ArgumentException($"Неподдерживаемый тип: {value?.GetType()}"),
        };

        /// <summary>
        /// Применяет тригонометрическую функцию к числу в выражении (угол в градусах). Если в выражении
        /// не одно число, возвращает false: вызывающий очищает выражение и ответ.
        /// </summary>
        public static bool TryApplyTrigonometric(TrigonometricFunction function, string expression, out string newExpression, out string answer)
        {
            newExpression = string.Empty;
            answer = string.Empty;
            if (!double.TryParse(expression
                .TrimStart("Sin(")
                .TrimStart("Cos(")
                .TrimStart("Tan(")
                .TrimStart("Atan(")
                .TrimEnd(')')
                .Replace(".", ","), out var value))
            {
                return false;
            }

            var (name, calculated) = function switch
            {
                TrigonometricFunction.Sin => ("Sin", Math.Sin(value.Radians())),
                TrigonometricFunction.Cos => ("Cos", Math.Cos(value.Radians())),
                TrigonometricFunction.Tan => ("Tan", Math.Tan(value.Radians())),
                _ => ("Atan", Math.Atan(value).Degrees()),
            };
            newExpression = $"{name}({value})".Replace(',', '.');
            answer = calculated.ToPrettyString(stringOption: Util.PrettyStringOption.AsIs);
            return true;
        }
    }
}
