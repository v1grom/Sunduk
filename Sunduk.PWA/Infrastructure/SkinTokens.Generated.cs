// Сгенерировано tools/build.py из tokens.json. Руками не править.
#nullable enable
namespace Sunduk.PWA.Infrastructure;

/// <summary> Токены одной темы скина. Имена — как в tokens.json. </summary>
public sealed record SkinTokens(
    string Bg, string Surface, string Hover, string Border,
    string Text, string Text2, string Text3,
    string Accent, string AccentText,
    string Danger, string Warning, string Success, string Info,
    string Accent30, string Accent12);

public static class SkinCatalog
{
    /// <summary> Идентификаторы скинов в порядке tokens.json. Совпадают со значением data-skin. </summary>
    public static readonly string[] Names = ["personal", "areopag", "sunduk"];

    public static SkinTokens Get(string skin, bool dark) => (skin, dark) switch
    {
        ("personal", false) => PersonalLight,
        ("personal", true) => PersonalDark,
        ("areopag", false) => AreopagLight,
        ("areopag", true) => AreopagDark,
        ("sunduk", false) => SundukLight,
        ("sunduk", true) => SundukDark,
        _ => PersonalDark,
    };

    public static readonly SkinTokens PersonalLight = new("#F6F6F4", "#FFFFFF", "#EAEAE7", "#D9D9D5", "#17181A", "#55585E", "#86898F", "#E90765", "#121316", "#DB3C24", "#A26C02", "#20884B", "#0774E9", "rgba(233,7,101,0.3)", "rgba(233,7,101,0.12)");
    public static readonly SkinTokens PersonalDark = new("#121316", "#191B1F", "#21242A", "#2E323A", "#F2F2F0", "#9A9DA3", "#6B6E75", "#E90765", "#121316", "#DB3C24", "#A26C02", "#20884B", "#0774E9", "rgba(233,7,101,0.3)", "rgba(233,7,101,0.12)");
    public static readonly SkinTokens AreopagLight = new("#F0F2F3", "#FFFFFF", "#E6E9EC", "#CCD3D8", "#00243D", "#4C6677", "#7B8D9A", "#E63C2F", "#FFFFFF", "#C32258", "#84620B", "#007547", "#1668C5", "rgba(230,60,47,0.3)", "rgba(230,60,47,0.12)");
    public static readonly SkinTokens AreopagDark = new("#00243D", "#0A2F4C", "#123A59", "#2A4E68", "#FFFFFF", "#B7C4CE", "#7F92A0", "#E63C2F", "#FFFFFF", "#EA809A", "#D09A11", "#00AD68", "#63A4EE", "rgba(230,60,47,0.3)", "rgba(230,60,47,0.12)");
    public static readonly SkinTokens SundukLight = new("#F3F3F3", "#FFFFFF", "#E8E8E8", "#D4D4D4", "#1E1E1E", "#555555", "#808080", "#E64A19", "#FFFFFF", "#C32258", "#8A5A00", "#16794A", "#0F66C2", "rgba(230,74,25,0.3)", "rgba(230,74,25,0.12)");
    public static readonly SkinTokens SundukDark = new("#1E1E1E", "#252526", "#2D2D30", "#3E3E42", "#D2D2D2", "#A5A5A5", "#7C7C7C", "#FF5722", "#1E1E1E", "#F0647E", "#E0A30B", "#3DB57F", "#5AA9F5", "rgba(255,87,34,0.3)", "rgba(255,87,34,0.12)");
}
