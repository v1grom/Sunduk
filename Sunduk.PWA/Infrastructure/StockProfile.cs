using System;
using Sunduk.PWA.Infrastructure.CAM;
using System.Linq;
using Sunduk.Geometry;
using System.Collections.Generic;
using Sunduk.PWA.Infrastructure.Sequences.Base;
using Sunduk.PWA.Infrastructure.Sequences.Turning;
using Sunduk.PWA.Infrastructure.Tools.Base;
using Sunduk.PWA.Infrastructure.Tools.Turning;
using Sunduk.PWA.Infrastructure.Tools.Turning.Base;

namespace Sunduk.PWA.Infrastructure
{
    /// <summary>
    /// Профиль заготовки токарной детали — наружный и внутренний диаметр как функция Z, начиная
    /// от исходной заготовки и с учётом изменений от предыдущих переходов программы (см.
    /// <see cref="Build"/>). Не учитывает фрезерные переходы и не хранит настоящую 2D/3D
    /// геометрию — только по одному диаметру на каждый Z, этого достаточно, чтобы следующий
    /// переход мог спросить «какой сейчас диаметр в этом месте», не сканируя программу вручную
    /// (как раньше делал MachineTime-независимый GetCurrentInternalDiameter).
    /// </summary>
    public sealed class StockProfile
    {
        private readonly List<(double FromZ, double ToZ, double Diameter)> _external = new();
        private readonly List<(double FromZ, double ToZ, double Diameter)> _internal = new();

        /// <summary><paramref name="rawStockToZ"/> — реальная правая граница сырой заготовки (в
        /// сторону +Z, откуда торчит необработанный припуск под торцовку), если известна (см.
        /// <see cref="Build"/>) — до неё материал есть безусловно, дальше его нет вообще, даже если
        /// в программе ещё не было ни одной торцовки. По умолчанию (+∞) — прежнее поведение для
        /// вызовов, которым эта граница не важна (например, точечные запросы диаметра на Z=0).</summary>
        public StockProfile(double externalDiameter, double internalDiameter, double rawStockToZ = double.PositiveInfinity)
        {
            _external.Add((double.NegativeInfinity, rawStockToZ, externalDiameter));
            _internal.Add((double.NegativeInfinity, rawStockToZ, internalDiameter));
        }

        public double ExternalDiameterAt(double z) => DiameterAt(_external, z);
        public double InternalDiameterAt(double z) => DiameterAt(_internal, z);

        /// <summary>Сегменты наружного диаметра как есть (крайние — от/до бесконечности, пока
        /// заготовка не обрезана переходом с этой стороны) — для отрисовки силуэта.</summary>
        public IReadOnlyList<(double FromZ, double ToZ, double Diameter)> ExternalSegments => _external;

        /// <summary>Сегменты внутреннего диаметра как есть — см. <see cref="ExternalSegments"/>.</summary>
        public IReadOnlyList<(double FromZ, double ToZ, double Diameter)> InternalSegments => _internal;

        private readonly List<ThreadZone> _threads = new();

        /// <summary>Кольцевые полости внутри детали (торцевые канавки): диаметры внутреннего и наружного
        /// краёв и диапазон Z. Сам профиль «диаметр по Z» кольцо не выражает.</summary>
        private readonly List<(double FromZ, double ToZ, double InnerDiameter, double OuterDiameter)> _rings = new();

        /// <summary>Кольцевые полости, ограниченные имеющимся материалом: по каждому участку Z, где
        /// есть наружный материал, кольцо лежит между большим из (его внутренний край, отверстие) и
        /// меньшим из (его наружный край, наружный диаметр). Радиусы, не диаметры. Кольцо ничего не
        /// создаёт: вне материала его нет.</summary>
        public IReadOnlyList<(double FromZ, double ToZ, double InnerRadius, double OuterRadius)> EffectiveRings
        {
            get
            {
                var result = new List<(double, double, double, double)>();
                foreach (var ring in _rings)
                {
                    var cuts = new SortedSet<double> { ring.FromZ, ring.ToZ };
                    foreach (var s in _external.Concat(_internal))
                    {
                        if (s.FromZ > ring.FromZ && s.FromZ < ring.ToZ) cuts.Add(s.FromZ);
                        if (s.ToZ > ring.FromZ && s.ToZ < ring.ToZ) cuts.Add(s.ToZ);
                    }
                    var points = cuts.ToList();
                    for (var i = 0; i + 1 < points.Count; i++)
                    {
                        var mid = (points[i] + points[i + 1]) / 2;
                        var outer = Math.Min(ring.OuterDiameter, ExternalDiameterAt(mid));
                        var inner = Math.Max(ring.InnerDiameter, InternalDiameterAt(mid));
                        if (ExternalDiameterAt(mid) > 0 && outer > inner) result.Add((points[i], points[i + 1], inner / 2, outer / 2));
                    }
                }
                return result;
            }
        }

        private void AddRing(double z1, double z2, double d1, double d2)
        {
            var from = Math.Min(z1, z2);
            var to = Math.Max(z1, z2);
            if (from < to && Math.Abs(d1 - d2) > 1e-9) _rings.Add((from, to, Math.Min(d1, d2), Math.Max(d1, d2)));
        }

        /// <summary>Нарезанные резьбы (условно) — для отрисовки линией по среднему диаметру.</summary>
        public IReadOnlyList<ThreadZone> Threads => _threads;

        /// <summary>Участок резьбы: [FromZ, ToZ], средний диаметр (диаметр резьбы − 0.6495·шаг), снаружи или внутри.</summary>
        public readonly record struct ThreadZone(double FromZ, double ToZ, double MeanDiameter, bool External);

        /// <summary>Отверстия, ограниченные наружным профилем: там, где наружный диаметр не больше
        /// диаметра отверстия (деталь ниже срезана — например, сферой после сверления), от отверстия
        /// остаётся ровно наружный контур, а где материала нет совсем, отверстия нет. Нужно для
        /// отрисовки: заливка по правилу чётности иначе закрашивает «отверстие в воздухе» и материал
        /// появляется там, где его нет. Только сегменты с диаметром &gt; 0.</summary>
        public IReadOnlyList<(double FromZ, double ToZ, double Diameter)> EffectiveInternalSegments
        {
            get
            {
                var result = new List<(double FromZ, double ToZ, double Diameter)>();
                foreach (var hole in _internal)
                {
                    if (hole.Diameter <= 0) continue;
                    foreach (var outer in _external)
                    {
                        if (outer.Diameter <= 0) continue;
                        var from = Math.Max(hole.FromZ, outer.FromZ);
                        var to = Math.Min(hole.ToZ, outer.ToZ);
                        if (from < to) result.Add((from, to, Math.Min(hole.Diameter, outer.Diameter)));
                    }
                }
                result.Sort((a, b) => a.FromZ.CompareTo(b.FromZ));
                return result;
            }
        }

        /// <summary>Резать/сверлить можно только там, где материал физически уже есть — переходы
        /// часто задают Z как безопасную точку подвода (например, сверловка "от Z2", когда торец
        /// уже обработан до 0.2), а не как реальную границу материала. Без клипа такая запись
        /// создавала бы "дырку в воздухе" за пределами текущего наружного профиля — в отрисовке
        /// (заливка через fill-rule=evenodd) это выглядит как ПОЯВИВШИЙСЯ из ниоткуда материал
        /// (нечётное число пересечений вне охватывающего наружного контура = закрашено), хотя
        /// реально там ничего не должно меняться.</summary>
        /// <summary>Правило профиля: переход ТОЛЬКО убирает материал. Наружный диаметр в зоне записи
        /// становится меньшим из текущего и нового (контур, канавка или резьба больше заготовки её не
        /// наращивают), и только там, где материал уже есть.</summary>
        private void WriteExternal(double z1, double z2, double diameter)
        {
            var fromZ = Math.Min(z1, z2);
            var toZ = Math.Max(z1, z2);
            // ToList: Write перезаписывает _external, а диапазоны лениво читают его же
            foreach (var (segFrom, segTo, existing) in ExistingMaterialRanges(fromZ, toZ).ToList())
                Write(_external, segFrom, segTo, Math.Min(existing, diameter));
        }

        /// <summary>Отверстие только расширяется: там, где материал есть, диаметр становится большим из
        /// текущего (0 — отверстия не было) и нового. Позднее более узкое растачивание/сверло отверстие
        /// не «заращивает».</summary>
        private void WriteInternal(double z1, double z2, double diameter)
        {
            var fromZ = Math.Min(z1, z2);
            var toZ = Math.Max(z1, z2);
            foreach (var (segFrom, segTo, _) in ExistingMaterialRanges(fromZ, toZ).ToList())
            {
                foreach (var hole in _internal.ToList())
                {
                    var from = Math.Max(segFrom, hole.FromZ);
                    var to = Math.Min(segTo, hole.ToZ);
                    if (from < to) Write(_internal, from, to, Math.Max(hole.Diameter, diameter));
                }
            }
        }

        /// <summary>Части [fromZ, toZ], пересекающиеся с уже существующим наружным материалом
        /// (Diameter &gt; 0), вместе с его текущим наружным диаметром.</summary>
        private IEnumerable<(double FromZ, double ToZ, double Diameter)> ExistingMaterialRanges(double fromZ, double toZ)
        {
            foreach (var segment in _external)
            {
                if (segment.Diameter <= 0) continue;
                var segFrom = Math.Max(segment.FromZ, fromZ);
                var segTo = Math.Min(segment.ToZ, toZ);
                if (segFrom < segTo) yield return (segFrom, segTo, segment.Diameter);
            }
        }

        private static double DiameterAt(List<(double FromZ, double ToZ, double Diameter)> segments, double z)
        {
            foreach (var segment in segments)
            {
                if (z >= segment.FromZ && z <= segment.ToZ) return segment.Diameter;
            }
            return 0;
        }

        private static void Write(List<(double FromZ, double ToZ, double Diameter)> segments, double z1, double z2, double diameter)
        {
            var fromZ = Math.Min(z1, z2);
            var toZ = Math.Max(z1, z2);
            if (fromZ == toZ) return;
            var result = new List<(double FromZ, double ToZ, double Diameter)>(segments.Count + 2);
            foreach (var segment in segments)
            {
                if (segment.ToZ <= fromZ || segment.FromZ >= toZ)
                {
                    result.Add(segment);
                    continue;
                }
                if (segment.FromZ < fromZ) result.Add((segment.FromZ, fromZ, segment.Diameter));
                if (segment.ToZ > toZ) result.Add((toZ, segment.ToZ, segment.Diameter));
            }
            result.Add((fromZ, toZ, diameter));
            segments.Clear();
            segments.AddRange(result);
        }

        /// <summary>
        /// Строит профиль по программе от исходной заготовки, применяя изменения от переходов
        /// <c>program[0..uptoIndex)</c> по порядку — последняя запись в конкретный участок Z
        /// побеждает, что соответствует реальности (последний рез в этом месте детали — тот, что
        /// остался). Переходы без вычислимой геометрии (произвольный G-код и т.п.) пропускаются,
        /// профиль в этом месте не меняется. <paramref name="rawStockToZ"/> — см. конструктор.
        /// </summary>
        public static StockProfile Build(IReadOnlyList<Sequence> program, int uptoIndex, double externalDiameter, double internalDiameter, double rawStockToZ = double.PositiveInfinity)
        {
            var profile = new StockProfile(externalDiameter, internalDiameter, rawStockToZ);
            for (var i = 0; i < uptoIndex && i < program.Count; i++)
            {
                Apply(profile, program[i]);
            }
            return profile;
        }

        private static void Apply(StockProfile profile, Sequence sequence)
        {
            switch (sequence)
            {
                // Торцовка обрезает заготовку по Z: всё, что было дальше фактического нового
                // торца (в сторону +Z, откуда торчит необработанный припуск), физически снято.
                // Реальный конечный Z зависит от конкретного вида торцовки — он НЕ всегда равен
                // полю ProfStockAllow: у объединённой чёрное+чистовое (FacingSequence) и у G70
                // чистовой по циклу (FinishFacingCycleSequence) профиль P/Q в самом G-коде всегда
                // целится в Z=0 (см. FacingOperation.Facing/FinishFacingCycle, endZ = cycleProfStockAllow
                // ? 0 : profStockAllow, а FinishFacingCycle всегда перезапускает тот же P/Q блок) —
                // ProfStockAllow там участвует только как параметр W у цикла G72 (сколько оставить
                // под последующую чистовую), не как реальная координата. У самостоятельной черновой
                // без запланированного G70 (RoughFacingSequence, RoughFacingCycleSequence до своей
                // чистовой) и у самостоятельной чистовой (FinishFacingSequence) ProfStockAllow —
                // это и есть реальный конечный Z.
                case FacingSequence facing:
                    ClipFace(profile, 0);
                    break;
                case RoughFacingSequence roughFacing:
                    ClipFace(profile, roughFacing.ProfStockAllow);
                    break;
                case RoughFacingCycleSequence roughFacingCycle:
                    ClipFace(profile, roughFacingCycle.ProfStockAllow);
                    break;
                case FinishFacingSequence finishFacing:
                    ClipFace(profile, finishFacing.ProfStockAllow);
                    break;
                case FinishFacingCycleSequence:
                    ClipFace(profile, 0);
                    break;
                case RoughTurningSequence roughTurning:
                    ApplyContour(profile, roughTurning.Tool, roughTurning.Contour);
                    break;
                case FinishTurningSequence finishTurning:
                    ApplyContour(profile, finishTurning.Tool, finishTurning.Contour);
                    break;
                case HighSpeedDrillingSequence highSpeedDrilling:
                    profile.WriteInternal(highSpeedDrilling.StartZ, highSpeedDrilling.EndZ, highSpeedDrilling.Tool.Diameter);
                    break;
                case PeckDrillingSequence peckDrilling:
                    profile.WriteInternal(peckDrilling.StartZ, peckDrilling.EndZ, peckDrilling.Tool.Diameter);
                    break;
                case PeckDeepDrillingSequence peckDeepDrilling:
                    profile.WriteInternal(peckDeepDrilling.StartZ, peckDeepDrilling.EndZ, peckDeepDrilling.Tool.Diameter);
                    break;
                case TurningInternalGroovingSequence internalGrooving:
                    ApplyGrooving(profile, internalGrooving.Tool, internalGrooving.CuttingPoint, internalGrooving.Width, internalGrooving.InternalDiameter, isExternal: false);
                    break;
                case TurningInternalRoughGroovingSequence internalRoughGrooving:
                    ApplyGrooving(profile, internalRoughGrooving.Tool, internalRoughGrooving.CuttingPoint, internalRoughGrooving.Width, internalRoughGrooving.InternalDiameter, isExternal: false);
                    break;
                case TurningExternalGroovingSequence externalGrooving:
                    ApplyGrooving(profile, externalGrooving.Tool, externalGrooving.CuttingPoint, externalGrooving.Width, externalGrooving.InternalDiameter, isExternal: true);
                    break;
                case TurningExternalRoughGroovingSequence externalRoughGrooving:
                    ApplyGrooving(profile, externalRoughGrooving.Tool, externalRoughGrooving.CuttingPoint, externalRoughGrooving.Width, externalRoughGrooving.InternalDiameter, isExternal: true);
                    break;
                // Отрезка: деталь (сторона +Z от прорези) отделена от прутка — материала левее
                // правой кромки прорези больше нет. Показываем отрезанную деталь с её длиной.
                case TurningCutOffSequence cutOff:
                    ClipBack(profile, cutOff.CuttingPoint);
                    break;
                // Резьба резцом (условно): наружная — вершины витков дают номинальный диаметр
                // резьбы на длине нарезания, внутренняя — отверстие становится диаметром резьбы.
                case ThreadCuttingSequence threadCutting:
                    profile.ApplyThread(threadCutting.Type == CuttingType.External, threadCutting.StartZ, threadCutting.EndZ, threadCutting.ThreadDiameter, threadCutting.ThreadPitch);
                    break;
                // Метчик: отверстие в зоне нарезания становится номинальным диаметром резьбы.
                case TappingSequence tapping:
                    profile.ApplyThread(false, tapping.StartZ, tapping.EndZ, tapping.Tool.Diameter, tapping.Tool.Pitch);
                    break;
                case TurningFaceGroovingSequence faceGrooving:
                    // торцевая канавка — кольцевая полость: Width/CuttingPoint — Z-начало/Z-конец, диаметры —
                    // наружный и внутренний край кольца
                    profile.AddRing(faceGrooving.Width, faceGrooving.CuttingPoint, faceGrooving.ExternalDiameter, faceGrooving.InternalDiameter);
                    break;
                case TurningFaceRoughGroovingSequence faceRoughGrooving:
                    profile.AddRing(faceRoughGrooving.Width, faceRoughGrooving.CuttingPoint, faceRoughGrooving.ExternalDiameter, faceRoughGrooving.InternalDiameter);
                    break;
            }
        }

        /// <summary>Обрезает и наружный, и внутренний профиль от фактического нового торца
        /// <paramref name="faceZ"/> до +∞ (материала физически больше нет). Вызывает низкоуровневый
        /// <see cref="Write"/> напрямую, а не <see cref="WriteExternal"/>/<see cref="WriteInternal"/> —
        /// им обеим нужно обнулиться БЕЗУСЛОВНО (это они и есть источник границы материала), а не
        /// клипаться друг относительно друга: наружный вызов первым же обнулил бы область, и
        /// внутреннему после него было бы не с чем пересекаться.</summary>
        private static void ClipFace(StockProfile profile, double faceZ)
        {
            profile.TrimThreads(faceZ, double.PositiveInfinity);
            Write(profile._external, faceZ, double.PositiveInfinity, 0);
            Write(profile._internal, faceZ, double.PositiveInfinity, 0);
        }

        /// <summary>
        /// Резьба (условно, без профиля витков). Наружная: в зоне нарезания диаметр становится
        /// номинальным диаметром резьбы, но не больше, чем был (резьба не наращивает металл).
        /// Внутренняя/метчик: отверстие расширяется до диаметра резьбы, но только там, где отверстие
        /// уже есть и меньше (в сплошном металле метчик/резец без отверстия ничего не делает).
        /// </summary>
        private void ApplyThread(bool external, double z1, double z2, double diameter, double pitch)
        {
            var fromZ = Math.Min(z1, z2);
            var toZ = Math.Max(z1, z2);
            var list = external ? _external : _internal;
            foreach (var segment in list.ToList())
            {
                if (segment.Diameter <= 0) continue;
                var from = Math.Max(segment.FromZ, fromZ);
                var to = Math.Min(segment.ToZ, toZ);
                if (from >= to) continue;
                if (external) WriteExternal(from, to, diameter);
                else if (segment.Diameter > 0) WriteInternal(from, to, diameter);
                // средний диаметр метрической резьбы: d − 0.6495·P
                if (external || segment.Diameter > 0) _threads.Add(new ThreadZone(from, to, diameter - 0.6495 * pitch, external));
            }
        }

        /// <summary>Убирает из зон резьбы участок [from, to], где материала больше нет.</summary>
        private void TrimThreads(double from, double to)
        {
            for (var i = _rings.Count - 1; i >= 0; i--)
            {
                var r = _rings[i];
                var parts = new List<(double FromZ, double ToZ, double InnerDiameter, double OuterDiameter)>();
                if (r.FromZ < from) parts.Add(r with { ToZ = Math.Min(r.ToZ, from) });
                if (r.ToZ > to) parts.Add(r with { FromZ = Math.Max(r.FromZ, to) });
                _rings.RemoveAt(i);
                _rings.InsertRange(i, parts.Where(p => p.FromZ < p.ToZ));
            }
            for (var i = _threads.Count - 1; i >= 0; i--)
            {
                var t = _threads[i];
                var parts = new List<ThreadZone>();
                if (t.FromZ < from) parts.Add(t with { ToZ = Math.Min(t.ToZ, from) });
                if (t.ToZ > to) parts.Add(t with { FromZ = Math.Max(t.FromZ, to) });
                _threads.RemoveAt(i);
                _threads.InsertRange(i, parts.Where(p => p.FromZ < p.ToZ));
            }
        }

        /// <summary>Отрезка: убирает материал левее <paramref name="edgeZ"/> (в сторону −Z, к патрону)
        /// — и наружный, и внутренний профиль. Как и <see cref="ClipFace"/>, пишет напрямую.</summary>
        private static void ClipBack(StockProfile profile, double edgeZ)
        {
            profile.TrimThreads(double.NegativeInfinity, edgeZ);
            Write(profile._external, double.NegativeInfinity, edgeZ, 0);
            Write(profile._internal, double.NegativeInfinity, edgeZ, 0);
        }

        private static void ApplyGrooving(StockProfile profile, TurningGroovingTool tool, double cuttingPoint, double width, double diameter, bool isExternal)
        {
            // Правый край канавки — в точке резания (от какого угла резца отсчитан нуль, влияет только
            // на программируемый Z), протяжённость — полная ширина канавки, а не путь, который проходит
            // кромка резца (он короче на ширину самого резца).
            var endPoint = cuttingPoint - width;
            if (isExternal) profile.WriteExternal(cuttingPoint, endPoint, diameter);
            else profile.WriteInternal(cuttingPoint, endPoint, diameter);
        }

        /// <summary>Шагов на одну дугу и на один наклонный участок: профиль хранится ступенями
        /// постоянного диаметра, поэтому конус/дугу приходится аппроксимировать.</summary>
        private const int ContourSteps = 48;

        /// <summary>Длина ступени по Z, мм: мельче на рисунке не разглядеть.</summary>
        private const double ContourStepLength = 0.1;

        /// <summary>Число ступеней для участка длиной <paramref name="lengthZ"/> по Z: фаске в 1 мм
        /// хватает десятка, длинному конусу — не больше <see cref="ContourSteps"/>. Число сегментов
        /// профиля прямо определяет скорость перерисовки заготовки.</summary>
        private static int StepsFor(double lengthZ)
            => Math.Clamp((int)Math.Ceiling(Math.Abs(lengthZ) / ContourStepLength), 2, ContourSteps);

        private static void ApplyContour(StockProfile profile, TurningTool tool, List<Element> contour)
        {
            if (contour is null || contour.Count < 2 || tool is null) return;
            var isExternal = tool is TurningExternalTool;
            if (!isExternal && tool is not TurningInternalTool) return;

            // Чертёжный контур вместе со скруглениями и фасками на узлах — тот же, что рисует превью
            // перехода: заготовка после перехода должна совпадать с тем, что реально снимет G-код.
            var path = Visualization.TurningToolpath.Build(contour, isExternal);
            if (path is null) return;

            foreach (var move in path.Nominal)
            {
                // сэмпл вдоль участка: (Z, радиус); прямая делится на те же шаги, что и дуга
                var samples = new List<Visualization.Pt>();
                if (move.IsArc)
                {
                    samples.AddRange(move.Sample(StepsFor(move.To.Z - move.From.Z)));
                }
                else
                {
                    if (Math.Abs(move.To.Z - move.From.Z) < 1e-9) continue; // торцевой участок: диаметр по Z не меняется
                    var steps = Math.Abs(move.To.R - move.From.R) > 1e-9 ? StepsFor(move.To.Z - move.From.Z) : 1;
                    for (var k = 0; k <= steps; k++)
                        samples.Add(new Visualization.Pt(move.From.Z + (move.To.Z - move.From.Z) * k / steps, move.From.R + (move.To.R - move.From.R) * k / steps));
                }

                for (var k = 0; k + 1 < samples.Count; k++)
                {
                    if (Math.Abs(samples[k + 1].Z - samples[k].Z) < 1e-9) continue;
                    var diameter = samples[k].R + samples[k + 1].R; // среднее двух радиусов × 2
                    if (isExternal) profile.WriteExternal(samples[k].Z, samples[k + 1].Z, diameter);
                    else profile.WriteInternal(samples[k].Z, samples[k + 1].Z, diameter);
                }
            }
        }
    }
}
