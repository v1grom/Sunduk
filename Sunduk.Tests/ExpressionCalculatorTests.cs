using Sunduk.PWA.Infrastructure;

namespace Sunduk.Tests;

public class ExpressionCalculatorTests
{
    [Fact]
    public void Evaluate_RespectsOperatorPrecedence()
    {
        Assert.True(ExpressionCalculator.TryEvaluate("2+3*4", out var result, out _));
        Assert.Equal("14", result);
    }

    [Fact]
    public void Evaluate_UnderstandsPiSquareAndPower()
    {
        Assert.True(ExpressionCalculator.TryEvaluate("3²", out var square, out _));
        Assert.Equal("9", square);
        Assert.True(ExpressionCalculator.TryEvaluate("2^3", out var power, out _));
        Assert.Equal("8", power);
        Assert.True(ExpressionCalculator.TryEvaluate("𝜋", out var pi, out _));
        Assert.StartsWith("3", pi);
    }

    [Fact]
    public void Evaluate_DegreeAndRadianFunctions_AreInverse()
    {
        Assert.True(ExpressionCalculator.TryEvaluate("Deg(Rad(45))", out var result, out _));
        Assert.Equal("45", result);
    }

    [Fact]
    public void Evaluate_Garbage_ReturnsError()
    {
        Assert.False(ExpressionCalculator.TryEvaluate("2+*", out var result, out var error));
        Assert.Equal(string.Empty, result);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void Trigonometric_AppliesToNumberInDegrees()
    {
        Assert.True(ExpressionCalculator.TryApplyTrigonometric(TrigonometricFunction.Sin, "30", out var expression, out var answer));
        Assert.Equal("Sin(30)", expression);
        Assert.Equal("0.5", answer);
    }

    [Fact]
    public void Trigonometric_NotANumber_Fails()
    {
        Assert.False(ExpressionCalculator.TryApplyTrigonometric(TrigonometricFunction.Cos, "abc", out _, out _));
    }
}
