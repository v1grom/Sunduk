using Sunduk.Geometry;
using Sunduk.Geometry.ContourElements;
using Sunduk.Geometry.ContourElements.Base;
using Sunduk.PWA.Infrastructure;
using Sunduk.PWA.Infrastructure.Sequences.Base;
using Sunduk.PWA.Infrastructure.Sequences.Turning;
using Sunduk.PWA.Infrastructure.CAM;
using Sunduk.PWA.Infrastructure.Tools.Base;
using Sunduk.PWA.Infrastructure.Tools.Turning;
using Sunduk.PWA.Infrastructure.Tools.Turning.Base;

namespace Sunduk.Tests;

/// <summary>Состояние заготовки после контурного точения (превью в SunduCAM).</summary>
public class StockProfileTests
{
    private static List<Sequence> Program(List<Element> contour)
    {
        var machine = new Machine { MachineType = MachineType.Turning };
        var tool = new TurningExternalTool(3, TurningExternalTool.Types.Bar, 80, 0.8);
        return new List<Sequence> { new FinishTurningSequence(machine, Material.Steel, tool, contour, 1, 2, 0.2, 200, 0.1) };
    }

    [Fact]
    public void ContourWithArcsAndCones_DoesNotThrow_AndKeepsProfile()
    {
        // раньше падало: «Collection was modified» — список менялся во время перебора
        var contour = new List<Element>
        {
            new Point(20, 0),
            new Line(20, -5),
            new Arc(28, -9, 6, Direction.CCW),
            new Arc(36, -13, 6, Direction.CW),
            new Line(36, -30),
            new Line(44, -34),
            new Line(44, -50),
        };

        var profile = StockProfile.Build(Program(contour), 1, 50, 0);

        Assert.Equal(20, profile.ExternalDiameterAt(-2.5), 3);
        Assert.Equal(36, profile.ExternalDiameterAt(-20), 3);
        Assert.Equal(44, profile.ExternalDiameterAt(-45), 3);
        // на конусе (36→44 при Z −30…−34) диаметр растёт вместе с глубиной
        Assert.InRange(profile.ExternalDiameterAt(-32), 38, 42);
    }

    [Fact]
    public void Cone_IsApproximatedBySteps_NotByEndDiameter()
    {
        var contour = new List<Element> { new Point(20, 0), new Line(40, -20) };

        var profile = StockProfile.Build(Program(contour), 1, 50, 0);

        // середина конуса — около 30, а не 40 (прежний расчёт брал диаметр конца на весь участок)
        Assert.InRange(profile.ExternalDiameterAt(-10), 29, 31);
    }
}

/// <summary>Общее окно SunduCAM: заготовка должна отражать все виды обработки.</summary>
public class StockProfileOperationsTests
{
    private static readonly Machine Machine = new() { MachineType = MachineType.Turning };

    private static StockProfile Build(params Sequence[] program) => StockProfile.Build(program, program.Length, 50, 0, 2);

    [Fact]
    public void Facing_RemovesAllowanceOnTheFront()
    {
        var facing = (FacingSequence)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(FacingSequence));

        var profile = Build(facing);

        Assert.Equal(0, profile.ExternalDiameterAt(1), 3);   // перед торцом материала нет
        Assert.Equal(50, profile.ExternalDiameterAt(-5), 3); // за торцом — заготовка
    }

    [Fact]
    public void Drilling_MakesAHole()
    {
        var drill = new TurningHighSpeedDrillingSequence(Machine, CoordinateSystem.G54, Material.Steel, new TurningDrillingTool(5, DrillingTool.Types.Solid, 12, 140), 2, -30, 100, 0.1);

        var profile = Build(drill);

        Assert.Equal(12, profile.InternalDiameterAt(-10), 3);
        Assert.Equal(0, profile.InternalDiameterAt(-40), 3); // глубже сверла отверстия нет
    }

    [Fact]
    public void Turning_ReducesExternalDiameter()
    {
        var tool = new TurningExternalTool(3, TurningExternalTool.Types.Bar, 80, 0.8);
        var contour = new List<Element> { new Point(20, 0), new Line(30, -10), new Line(30, -30) };

        var profile = Build(new FinishTurningSequence(Machine, Material.Steel, tool, contour, 1, 2, 0.2, 200, 0.1));

        Assert.Equal(30, profile.ExternalDiameterAt(-20), 3);
        Assert.InRange(profile.ExternalDiameterAt(-5), 22, 28);
    }

    [Fact]
    public void ExternalGrooving_CutsAGroove()
    {
        var tool = new GroovingExternalTool(12, GroovingExternalTool.Types.Grooving, 3, TurningGroovingTool.Point.Right);
        var groove = new TurningExternalGroovingSequence(Machine, CoordinateSystem.G54, Material.Steel, tool, -20, 50, 40, 3, 1, 0.1, 0, 0, Blunt.Radius, Blunt.Radius, 100, 120, 0.05, 0.05);

        var profile = Build(groove);

        Assert.Equal(40, profile.ExternalDiameterAt(-21.5), 3);
        Assert.Equal(50, profile.ExternalDiameterAt(-10), 3);
    }

    [Fact]
    public void ThreadCutting_External_SetsNominalDiameterOnTheThreadedLength()
    {
        var thread = (ThreadCuttingSequence)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThreadCuttingSequence));
        thread.Type = CuttingType.External;
        thread.ThreadDiameter = 42;
        thread.StartZ = -5;
        thread.EndZ = -25;

        var profile = Build(thread);

        Assert.Equal(42, profile.ExternalDiameterAt(-15), 3);
        Assert.Equal(50, profile.ExternalDiameterAt(-30), 3);
    }

    [Fact]
    public void ThreadCutting_Internal_EnlargesTheHoleToThreadDiameter()
    {
        var drill = new TurningHighSpeedDrillingSequence(Machine, CoordinateSystem.G54, Material.Steel, new TurningDrillingTool(5, DrillingTool.Types.Solid, 10, 140), 2, -30, 100, 0.1);
        var thread = (ThreadCuttingSequence)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThreadCuttingSequence));
        thread.Type = CuttingType.Internal;
        thread.ThreadDiameter = 12;
        thread.StartZ = -2;
        thread.EndZ = -20;

        var profile = Build(drill, thread);

        Assert.Equal(12, profile.InternalDiameterAt(-10), 3);
        Assert.Equal(10, profile.InternalDiameterAt(-25), 3);
    }

    [Fact]
    public void CutOff_SeparatesThePart_AtTheRightEdgeOfTheSlot()
    {
        var tool = new GroovingExternalTool(12, GroovingExternalTool.Types.Cutting, 3, TurningGroovingTool.Point.Right);
        var cutOff = new TurningCutOffSequence(Machine, CoordinateSystem.G54, Material.Steel, tool, -40, 50, 0, 0.3, 1, Blunt.Radius, 0, 0, 100, 0.05);

        var profile = Build(cutOff);

        Assert.Equal(50, profile.ExternalDiameterAt(-39), 3); // деталь до прорези
        Assert.Equal(0, profile.ExternalDiameterAt(-41), 3);  // прутка за отрезкой нет
    }

    [Fact]
    public void ThreadCutting_External_NeverGrowsTheDiameter()
    {
        var tool = new TurningExternalTool(3, TurningExternalTool.Types.Bar, 80, 0.8);
        var contour = new List<Element> { new Point(20, 0), new Line(20, -30) };
        var turning = new FinishTurningSequence(Machine, Material.Steel, tool, contour, 1, 2, 0.2, 200, 0.1);
        var thread = (ThreadCuttingSequence)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThreadCuttingSequence));
        thread.Type = CuttingType.External;
        thread.ThreadDiameter = 36;
        thread.StartZ = 2;
        thread.EndZ = -18;

        var profile = Build(turning, thread);

        Assert.Equal(20, profile.ExternalDiameterAt(-10), 3);
    }

    [Fact]
    public void ThreadCutting_Internal_WithoutHole_DoesNothing()
    {
        var thread = (ThreadCuttingSequence)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThreadCuttingSequence));
        thread.Type = CuttingType.Internal;
        thread.ThreadDiameter = 12;
        thread.StartZ = -2;
        thread.EndZ = -20;

        var profile = Build(thread);

        Assert.Equal(0, profile.InternalDiameterAt(-10), 3);
    }

    [Fact]
    public void Hole_IsClippedToTheOuterProfile_AfterTurningRemovedTheMaterial()
    {
        // сверло Ø10 до Z−35, затем сфера: на передней части наружный диаметр меньше 10 —
        // отверстие там не может быть шире детали, иначе в отрисовке «появляется» материал
        var drill = new TurningHighSpeedDrillingSequence(Machine, CoordinateSystem.G54, Material.Steel, new TurningDrillingTool(5, DrillingTool.Types.Solid, 10, 140), 2, -35, 100, 0.1);
        var tool = new TurningExternalTool(3, TurningExternalTool.Types.Bar, 80, 0.8);
        var contour = new List<Element> { new Point(0, 0), new Arc(24, -12, 12, Direction.CCW), new Line(24, -30) };

        var profile = Build(drill, new FinishTurningSequence(Machine, Material.Steel, tool, contour, 1, 2, 0.2, 200, 0.1));

        foreach (var hole in profile.EffectiveInternalSegments)
        {
            var mid = (hole.FromZ + hole.ToZ) / 2;
            Assert.True(hole.Diameter <= profile.ExternalDiameterAt(mid) + 1e-6, $"отверстие Ø{hole.Diameter} шире детали Ø{profile.ExternalDiameterAt(mid)} при Z{mid}");
        }
        Assert.Contains(profile.EffectiveInternalSegments, h => h.Diameter == 10);
    }

    [Fact]
    public void Operations_NeverAddMaterial()
    {
        var tool = new TurningExternalTool(3, TurningExternalTool.Types.Bar, 80, 0.8);
        // сначала точим до Ø20, потом «контур» Ø40 на том же месте — диаметр не должен вырасти
        var small = new List<Element> { new Point(20, 0), new Line(20, -30) };
        var large = new List<Element> { new Point(40, 0), new Line(40, -30) };

        var profile = Build(
            new FinishTurningSequence(Machine, Material.Steel, tool, small, 1, 2, 0.2, 200, 0.1),
            new FinishTurningSequence(Machine, Material.Steel, tool, large, 1, 2, 0.2, 200, 0.1));

        Assert.Equal(20, profile.ExternalDiameterAt(-15), 3);
    }

    [Fact]
    public void Hole_NeverShrinks_ByALaterNarrowerWrite()
    {
        var wide = new TurningHighSpeedDrillingSequence(Machine, CoordinateSystem.G54, Material.Steel, new TurningDrillingTool(5, DrillingTool.Types.Solid, 20, 140), 2, -30, 100, 0.1);
        var narrow = new TurningHighSpeedDrillingSequence(Machine, CoordinateSystem.G54, Material.Steel, new TurningDrillingTool(6, DrillingTool.Types.Solid, 8, 140), 2, -30, 100, 0.1);

        var profile = Build(wide, narrow);

        Assert.Equal(20, profile.InternalDiameterAt(-10), 3);
    }

    [Fact]
    public void Groove_DeeperThanTheCurrentDiameter_ChangesNothing()
    {
        var tool = new GroovingExternalTool(12, GroovingExternalTool.Types.Grooving, 3, TurningGroovingTool.Point.Right);
        var turned = new FinishTurningSequence(Machine, Material.Steel, new TurningExternalTool(3, TurningExternalTool.Types.Bar, 80, 0.8),
            new List<Element> { new Point(30, 0), new Line(30, -40) }, 1, 2, 0.2, 200, 0.1);
        // канавка «до Ø45» на детали Ø30 — это не канавка, материал не появляется
        var groove = new TurningExternalGroovingSequence(Machine, CoordinateSystem.G54, Material.Steel, tool, -20, 50, 45, 3, 1, 0.1, 0, 0, Blunt.Radius, Blunt.Radius, 100, 120, 0.05, 0.05);

        var profile = Build(turned, groove);

        Assert.Equal(30, profile.ExternalDiameterAt(-18.5), 3);
    }
}

public class ThreadZoneTests
{
    private static readonly Machine Machine = new() { MachineType = MachineType.Turning };

    [Fact]
    public void ExternalThread_IsRecorded_WithMeanDiameter()
    {
        var thread = (ThreadCuttingSequence)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThreadCuttingSequence));
        thread.Type = CuttingType.External;
        thread.ThreadDiameter = 36;
        thread.ThreadPitch = 1.5;
        thread.StartZ = -5;
        thread.EndZ = -25;

        var profile = StockProfile.Build(new[] { (Sequence)thread }, 1, 50, 0, 2);

        var zone = Assert.Single(profile.Threads);
        Assert.Equal(-25, zone.FromZ, 3);
        Assert.Equal(-5, zone.ToZ, 3);
        Assert.Equal(36 - 0.6495 * 1.5, zone.MeanDiameter, 3);
        Assert.True(zone.External);
    }

    [Fact]
    public void Thread_IsRemovedWithTheMaterial_ByACutOff()
    {
        var thread = (ThreadCuttingSequence)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThreadCuttingSequence));
        thread.Type = CuttingType.External;
        thread.ThreadDiameter = 36;
        thread.ThreadPitch = 1.5;
        thread.StartZ = -5;
        thread.EndZ = -25;
        var tool = new GroovingExternalTool(12, GroovingExternalTool.Types.Cutting, 3, TurningGroovingTool.Point.Right);
        var cutOff = new TurningCutOffSequence(Machine, CoordinateSystem.G54, Material.Steel, tool, -15, 50, 0, 0.3, 1, Blunt.Radius, 0, 0, 100, 0.05);

        var profile = StockProfile.Build(new Sequence[] { thread, cutOff }, 2, 50, 0, 2);

        var zone = Assert.Single(profile.Threads);
        Assert.Equal(-15, zone.FromZ, 3); // отрезанная часть резьбы пропала
    }
}

public class FaceGrooveTests
{
    private static readonly Machine Machine = new() { MachineType = MachineType.Turning };

    [Fact]
    public void FaceGroove_IsARingVoid_NotAReductionOfTheOuterDiameter()
    {
        var tool = new GroovingFaceTool(12, 4, TurningGroovingTool.Point.Bottom);
        // кольцо Ø30…Ø40 глубиной 8 от торца (Z0…−8) в заготовке Ø50
        var groove = new TurningFaceGroovingSequence(Machine, CoordinateSystem.G54, Material.Steel, tool, -8, 40, 30, 0, 1, 0.1, 0, 0, Blunt.Radius, Blunt.Radius, 100, 120, 0.05, 0.05);

        var profile = StockProfile.Build(new Sequence[] { groove }, 1, 50, 0, 0);

        Assert.Equal(50, profile.ExternalDiameterAt(-4), 3); // наружный диаметр не изменился
        var ring = Assert.Single(profile.EffectiveRings);
        Assert.Equal(15, ring.InnerRadius, 3);
        Assert.Equal(20, ring.OuterRadius, 3);
        Assert.Equal(-8, ring.FromZ, 3);
        Assert.Equal(0, ring.ToZ, 3);
    }

    [Fact]
    public void Ring_IsClippedToTheMaterial_AfterTheOuterDiameterShrinks()
    {
        var tool = new GroovingFaceTool(12, 4, TurningGroovingTool.Point.Bottom);
        var groove = new TurningFaceGroovingSequence(Machine, CoordinateSystem.G54, Material.Steel, tool, -8, 40, 30, 0, 1, 0.1, 0, 0, Blunt.Radius, Blunt.Radius, 100, 120, 0.05, 0.05);
        var turning = new FinishTurningSequence(Machine, Material.Steel, new TurningExternalTool(3, TurningExternalTool.Types.Bar, 80, 0.8),
            new List<Element> { new Point(35, 0), new Line(35, -20) }, 1, 2, 0.2, 200, 0.1);

        var profile = StockProfile.Build(new Sequence[] { groove, turning }, 2, 50, 0, 0);

        var ring = Assert.Single(profile.EffectiveRings);
        Assert.Equal(17.5, ring.OuterRadius, 3); // деталь обточена до Ø35 — кольцо не шире детали
    }
}
