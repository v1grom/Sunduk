using Sunduk.PWA.Components.Stuff;

namespace Sunduk.PWA.Infrastructure.State
{
    /// <summary>Ввод дуги: центр (X — диаметр) и радиус.</summary>
    public class ArcAdvancedArcInput
    {
        public string CenterX { get; set; } = string.Empty;
        public string CenterZ { get; set; } = string.Empty;
        public string Radius { get; set; } = string.Empty;
    }

    /// <summary>Ввод прямой одним из четырёх способов (см. <see cref="ArcAdvancedComponent.LineKind"/>).</summary>
    public class ArcAdvancedLineInput
    {
        public ArcAdvancedComponent.LineKind Kind { get; set; } = ArcAdvancedComponent.LineKind.Angle;
        /// <summary>Z торца (Kind = Face).</summary>
        public string FaceZ { get; set; } = string.Empty;
        /// <summary>Диаметр цилиндра (Kind = Cylinder).</summary>
        public string Diameter { get; set; } = string.Empty;
        /// <summary>Точка 1 (Kind = Angle / TwoPoints), X — диаметр.</summary>
        public string X1 { get; set; } = string.Empty;
        public string Z1 { get; set; } = string.Empty;
        /// <summary>Точка 2 (Kind = TwoPoints), X — диаметр.</summary>
        public string X2 { get; set; } = string.Empty;
        public string Z2 { get; set; } = string.Empty;
        /// <summary>Угол к оси Z, градусы (Kind = Angle).</summary>
        public string Angle { get; set; } = string.Empty;
    }

    /// <summary>
    /// Состояние ArcAdvancedComponent («Пересечения и касание») на время жизни приложения
    /// (singleton в WASM).
    /// </summary>
    public class ArcAdvancedState
    {
        public ArcAdvancedComponent.Mode Mode { get; set; } = ArcAdvancedComponent.Mode.ArcArc;

        public ArcAdvancedArcInput ArcA { get; set; } = new();
        public ArcAdvancedArcInput ArcB { get; set; } = new();
        public ArcAdvancedLineInput LineA { get; set; } = new();
        public ArcAdvancedLineInput LineB { get; set; } = new();

        /// <summary>Внешняя точка для касательной к дуге, X — диаметр.</summary>
        public string PointX { get; set; } = string.Empty;
        public string PointZ { get; set; } = string.Empty;

        /// <summary>Радиус сопрягающей дуги.</summary>
        public string FilletRadius { get; set; } = string.Empty;
        public ArcAdvancedComponent.ElementKind FilletKindA { get; set; } = ArcAdvancedComponent.ElementKind.Arc;
        public ArcAdvancedComponent.ElementKind FilletKindB { get; set; } = ArcAdvancedComponent.ElementKind.Arc;

        /// <summary>Выбранный пользователем результат (подсвечивается на чертеже).</summary>
        public int Selected { get; set; }

        /// <summary>Масштабировать чертёж по выбранному результату, а не по всей схеме.</summary>
        public bool ZoomResult { get; set; } = true;
    }
}
