using Sunduk.Geometry.ContourElements;
using Sunduk.Geometry.ContourElements.Base;
using Sunduk.PWA.Infrastructure;
using Sunduk.PWA.Infrastructure.Templates;
using Sunduk.PWA.Infrastructure.Tools.Turning;

namespace Sunduk.Tests;

/// <summary>
/// Структурная сверка цикла G71 чернового точения: проверяем, что цикл эмитится, содержит
/// P/Q-строки профиля и что профиль использует скомпенсированный контур. Точные координаты и
/// диалект G71 сверяются с конкретным станком (как и прочие циклы программы).
/// </summary>
public class RoughTurningTests
{
    [Fact]
    public void RoughTurning_EmitsG71CycleWithProfileBlock()
    {
        var machine = new Machine { MachineType = MachineType.Turning };
        var tool = new TurningExternalTool(1, TurningExternalTool.Types.Bar, 80, 0.8);
        var contour = new List<Element>
        {
            new Point(40, 0),
            new Point(20, -10),
            new Point(20, -20),
        };

        var gcode = Operation.RoughTurning(
            machine,
            CoordinateSystem.G54,
            tool,
            contour,
            stepOver: 1,
            roughStockAllow: 2,
            profStockAllow: 0.2,
            seqNo: (1, 2),
            speedRough: 200,
            feedRough: 0.15,
            coolant: Coolant.None);

        Assert.Contains("G71 U1", gcode);
        Assert.Contains("G71 P1 Q2", gcode);
        Assert.Contains("N1 ", gcode);
        Assert.Contains("N2 ", gcode);
        Assert.Contains("G1 X", gcode);
    }
}

public class ContourStartApproachTests
{
    [Fact]
    public void ExternalFinish_StartRounding_ApproachesAlongTheFaceFromBelow()
    {
        var machine = new Machine { MachineType = MachineType.Turning };
        var tool = new TurningExternalTool(1, TurningExternalTool.Types.Bar, 80, 0.8);
        var contour = new List<Element> { new Point(18, 0, 1), new Line(30, -8), new Line(30, -20) };

        var gcode = Operation.ContourTurning(machine, CoordinateSystem.G54, tool, contour, 200, 0.1, Coolant.None);

        // Узел у конуса — пересечение скомпенсированных сторон: X18 − 2·0.8·(1 − tg(θ/2)) = 17.2,
        // θ = 53.13° — поворот от торца к конусу. R1 + радиус пластины 0.8 = 1.8; «мнимая линия» по
        // торцу начинается там, откуда пойдёт скругление: 17.2 − 2·1.8·tg(θ/2) = 15.4.
        Assert.Contains("G0 X15.4 Z2.", gcode);
        Assert.Contains("G1 Z0.", gcode);
        Assert.Contains("G1 X17.2 Z0. R1.8", gcode);
        Assert.DoesNotContain("G0 X21.6", gcode);
    }

    [Fact]
    public void BluntSize_AddsToolRadiusOnConvexCorner_SubtractsOnConcave()
    {
        var machine = new Machine { MachineType = MachineType.Turning };
        var tool = new TurningExternalTool(1, TurningExternalTool.Types.Bar, 80, 0.8);
        var contour = new List<Element>
        {
            new Point(20, 0, 1), new Line(20, -12, 0, 1.5, Sunduk.Geometry.Blunt.Radius), new Line(32, -12, 0, 1, Sunduk.Geometry.Blunt.SimpleChamfer),
            new Line(32, -25, 0, 1.5, Sunduk.Geometry.Blunt.SimpleChamfer), new Line(44, -25, 0, 2, Sunduk.Geometry.Blunt.Radius),
            new Line(44, -38, 0, 0.5, Sunduk.Geometry.Blunt.Radius), new Line(52, -38), new Line(52, -50),
        };

        var gcode = Operation.ContourTurning(machine, CoordinateSystem.G54, tool, contour, 200, 0.1, Coolant.None);

        Assert.Contains("G1 X20. Z0. R1.8", gcode);     // выпуклый: 1 + 0.8
        Assert.Contains("G1 X20. Z-12. R0.7", gcode);   // вогнутый: 1.5 − 0.8
        Assert.Contains("G1 X32. Z-12. C1.4", gcode);   // выпуклая фаска: 1 + 0.8/2
        Assert.Contains("G1 X32. Z-25. C1.1", gcode);   // вогнутая фаска: 1.5 − 0.8/2
        Assert.Contains("G1 X44. Z-25. R2.8", gcode);
        Assert.Contains("G1 X44. Z-38.\n", gcode);      // вогнутый R0.5 меньше радиуса пластины — не пишется
    }

    [Fact]
    public void RoughCycle_WritesBluntsAndReturnsToApproachDiameter()
    {
        var machine = new Machine { MachineType = MachineType.Turning };
        var tool = new TurningExternalTool(1, TurningExternalTool.Types.Bar, 80, 0.8);
        var contour = new List<Element>
        {
            new Point(20, 0, 1), new Line(20, -12, 0, 1.5, Sunduk.Geometry.Blunt.Radius), new Line(32, -12), new Line(32, -25),
        };

        var gcode = Operation.RoughTurning(machine, CoordinateSystem.G54, tool, contour, 1.5, 2, 0.3, (10, 20), 200, 0.25, Coolant.None, stockDiameter: 56);

        Assert.Contains("G0 X60. Z2.", gcode);
        Assert.Contains("N10 G1 X16.4 Z0.", gcode);
        Assert.Contains("G1 X20. Z0. R1.8", gcode);
        Assert.Contains("G1 X20. Z-12. R0.7", gcode);
        Assert.Contains("N20 G1 X60.", gcode);           // последняя строка цикла — возврат в X подвода
    }
}

public class InternalRoughTurningTests
{
    [Fact]
    public void Boring_UsesNegativeFinishAllowance_AndStartsInsideTheHole()
    {
        var machine = new Machine { MachineType = MachineType.Turning };
        var tool = new TurningInternalTool(3, 10, 55, 0.4);
        var contour = new List<Element> { new Point(34, 0), new Line(32, -2), new Line(32, -20), new Line(36, -22), new Line(36, -40) };

        var gcode = Operation.RoughTurning(machine, CoordinateSystem.G54, tool, contour, 1, 1, 0.3, (10, 20), 150, 0.15, Coolant.None, stockDiameter: 25);

        Assert.Contains("U-0.3", gcode);            // припуск под чистовую — к оси
        Assert.Contains("G0 X21. Z2.", gcode);       // Ø25 отверстие − 2·2 зазор
    }

    [Fact]
    public void ExternalTurning_KeepsPositiveFinishAllowance()
    {
        var machine = new Machine { MachineType = MachineType.Turning };
        var tool = new TurningExternalTool(3, TurningExternalTool.Types.Bar, 80, 0.8);
        var contour = new List<Element> { new Point(20, 0), new Line(30, -10), new Line(30, -30) };

        var gcode = Operation.RoughTurning(machine, CoordinateSystem.G54, tool, contour, 1, 2, 0.3, (10, 20), 150, 0.15, Coolant.None, stockDiameter: 50);

        Assert.Contains("U0.3", gcode);
        Assert.DoesNotContain("U-0.3", gcode);
    }
}

public class ModalGCodeTests
{
    private static readonly List<Element> Contour = new()
    {
        new Point(20, 0, 1), new Line(20, -12, 0, 1.5, Sunduk.Geometry.Blunt.Radius), new Line(32, -12), new Line(32, -25),
        new Arc(40, -29, 4, Sunduk.Geometry.Direction.CCW), new Line(40, -40),
    };

    [Fact]
    public void ModalMachine_DoesNotRepeatMotionCodeAndUnchangedCoordinates()
    {
        var machine = new Machine { MachineType = MachineType.Turning, ModalMotion = true };
        var tool = new TurningExternalTool(1, TurningExternalTool.Types.Bar, 80, 0.8);

        var gcode = Operation.ContourTurning(machine, CoordinateSystem.G54, tool, Contour, 200, 0.1, Coolant.None);

        Assert.Contains("G0 X16.4 Z2.", gcode);
        var lines = gcode.Split('\n');
        var start = Array.IndexOf(lines, "G1 Z0. F0.1");
        Assert.True(start > 0, gcode);
        Assert.Equal(new[] { "X20. R1.8", "Z-12. R0.7", "X32." }, lines.Skip(start + 1).Take(3));
        Assert.StartsWith("Z-", lines[start + 4]);       // цилиндр до дуги: только Z
        Assert.StartsWith("G3 X", lines[start + 5]);     // смена кода перемещения пишется
        Assert.Equal("G1 Z-40.", lines[start + 6]);      // после дуги G1 нужен снова, X не изменился
    }

    [Fact]
    public void NonModalMachine_WritesEveryBlockInFull()
    {
        var machine = new Machine { MachineType = MachineType.Turning };
        var tool = new TurningExternalTool(1, TurningExternalTool.Types.Bar, 80, 0.8);

        var gcode = Operation.ContourTurning(machine, CoordinateSystem.G54, tool, Contour, 200, 0.1, Coolant.None);

        Assert.Contains("G1 X20. Z-12. R0.7\nG1 X32. Z-12.\n", gcode);
    }

    [Fact]
    public void ModalRoughCycle_KeepsFirstAndLastProfileBlocksInFull()
    {
        var machine = new Machine { MachineType = MachineType.Turning, ModalMotion = true };
        var tool = new TurningExternalTool(1, TurningExternalTool.Types.Bar, 80, 0.8);

        var gcode = Operation.RoughTurning(machine, CoordinateSystem.G54, tool, Contour, 1.5, 2, 0.3, (10, 20), 200, 0.25, Coolant.None, stockDiameter: 56);

        Assert.Contains("N10 G1 X16.4 Z0.\nX20. R1.8\nZ-12. R0.7\nX32.\n", gcode);
        Assert.Contains("\nN20 G1 X60.\n", gcode);
    }

    [Fact]
    public void Modal_DropsRepeatedRapidAndItsUnchangedCoordinates()
    {
        var lines = GCodeBuilder.Modal(new[] { "G0 X50. Z2.", "G0 X20. Z2.", "G1 Z-10. F0.1", "G1 X24. Z-10.", "G0 X24. Z2.", "G0 X100. Z2.", "G28 U0.", "G0 X100. Z50." });

        Assert.Equal(new[] { "G0 X50. Z2.", "X20.", "G1 Z-10. F0.1", "X24.", "G0 Z2.", "X100.", "G28 U0.", "G0 X100. Z50." }, lines);
    }
}
