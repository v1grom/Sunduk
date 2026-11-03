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

        // R1 + радиус пластины 0.8 = 1.8; подвод по торцу снизу: X18 − 2·1.8 = 14.4, затем на узел с R
        Assert.Contains("G0 X14.4 Z2.", gcode);
        Assert.Contains("G1 Z0.", gcode);
        Assert.Contains("R1.8", gcode);
        Assert.DoesNotContain("G0 X21.6", gcode);
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

