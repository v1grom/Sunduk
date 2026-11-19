namespace Sunduk.PWA.Infrastructure.State
{
    /// <summary>
    /// Состояние расчёта фрезерной фаски (MillingChamferComponent) на время жизни приложения (singleton в WASM).
    /// </summary>
    public class MillingChamferState
    {
        /// <summary> Фаска по кромке контура (с коррекцией на радиус) или на отверстии врезанием по оси. </summary>
        public enum Kind { Edge, Hole }

        public Kind Mode { get; set; } = Kind.Edge;
        public string ToolAngle { get; set; } = "90";
        public string TipDiameter { get; set; } = "0";
        public string ToolDiameter { get; set; } = string.Empty;
        public string Width { get; set; } = string.Empty;
        public string Overtravel { get; set; } = "0.5";
        public string HoleDiameter { get; set; } = string.Empty;
    }
}
