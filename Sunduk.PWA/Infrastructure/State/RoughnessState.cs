namespace Sunduk.PWA.Infrastructure.State
{
    /// <summary>
    /// Состояние расчёта шероховатости (RoughnessComponent), фрезерных расчётов и конвертеров Ra ↔ Rz и твёрдости
    /// на время жизни приложения (singleton в WASM).
    /// </summary>
    public class RoughnessState
    {
        /// <summary>Что вводит пользователь в расчёте по подаче: остальное считается от этого поля.</summary>
        public enum Source { Feed, Ra, Rz }

        public Source Input { get; set; } = Source.Feed;
        public string InsertRadius { get; set; } = "0.8";
        public string Feed { get; set; } = string.Empty;
        public string Ra { get; set; } = string.Empty;
        public string Rz { get; set; } = string.Empty;

        // конвертер Ra ↔ Rz
        public bool ConvertFromRz { get; set; }
        public string ConverterRa { get; set; } = string.Empty;
        public string ConverterRz { get; set; } = string.Empty;

        // конвертер твёрдости
        public Hardness.Scale HardnessScale { get; set; } = Hardness.Scale.HRC;
        public string HardnessValue { get; set; } = string.Empty;

        // фрезерные расчёты (MillingComponent)
        public string ThinningDiameter { get; set; } = string.Empty;
        public string ThinningWidth { get; set; } = string.Empty;
        public string ThinningFeed { get; set; } = string.Empty;
        public string BallDiameter { get; set; } = string.Empty;
        public string BallDepth { get; set; } = string.Empty;
        public string BallSpeed { get; set; } = string.Empty;
        public string ScallopDiameter { get; set; } = string.Empty;
        public string ScallopStep { get; set; } = string.Empty;
        public string ScallopHeight { get; set; } = string.Empty;
        public bool ScallopFromHeight { get; set; }
    }
}
