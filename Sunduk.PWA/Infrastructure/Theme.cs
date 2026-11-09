using MudBlazor;
using System;
using System.Collections.Generic;

namespace Sunduk.PWA.Infrastructure;

/// <summary>
/// Темы MudBlazor. Цвета не задаются здесь: они берутся из <see cref="SkinCatalog"/>, который
/// генерируется из <c>tokens.json</c> репозитория design (<c>python tools/build.py mudblazor</c>).
/// Здесь только сопоставление токенов с ролями MudBlazor.
/// </summary>
public static class AppTheme
{
    /// <summary> Скин по умолчанию — оригинальные цвета Sunduk. </summary>
    public const string DefaultSkin = "sunduk";

    private static readonly Dictionary<string, MudTheme> Cache = [];

    /// <summary> Тема скина сразу для обеих схем: светлая и тёмная переключаются через IsDarkMode. </summary>
    public static MudTheme Get(string skin)
    {
        skin = Normalize(skin);
        if (!Cache.TryGetValue(skin, out var theme))
            Cache[skin] = theme = Build(skin);
        return theme;
    }

    /// <summary> Неизвестное имя (например, из старого или повреждённого хранилища) заменяется скином по умолчанию. </summary>
    public static string Normalize(string? skin) =>
        skin is not null && Array.IndexOf(SkinCatalog.Names, skin) >= 0 ? skin : DefaultSkin;

    /// <summary> Названия палитр в параметрах: v1grom (идентификатор personal в design), areopag, sunduk. </summary>
    public static string Title(string skin) => skin switch
    {
        "personal" => "v1grom",
        _ => skin,
    };

    private static MudTheme Build(string skin) => new()
    {
        Typography = BuildTypography(),
        // Прямые углы: тени и скругления в дизайн-системе не используются
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "0px" },
        PaletteLight = ToLight(SkinCatalog.Get(skin, dark: false)),
        PaletteDark = ToDark(SkinCatalog.Get(skin, dark: true)),
    };

    private static PaletteLight ToLight(SkinTokens t)
    {
        var p = new PaletteLight();
        Apply(p, t);
        return p;
    }

    private static PaletteDark ToDark(SkinTokens t)
    {
        var p = new PaletteDark();
        Apply(p, t);
        return p;
    }

    private static void Apply(Palette p, SkinTokens t)
    {
        p.Primary = t.Accent;
        p.PrimaryContrastText = t.AccentText;
        p.Secondary = t.Success;
        p.SecondaryContrastText = t.Bg;
        p.Tertiary = t.Info;
        p.TertiaryContrastText = t.Bg;
        p.Info = t.Info;
        p.Success = t.Success;
        p.Warning = t.Warning;
        p.Error = t.Danger;
        p.Dark = t.Bg;
        p.Black = t.Bg;

        p.Background = t.Bg;
        p.BackgroundGray = t.Hover;
        p.Surface = t.Surface;
        p.DrawerBackground = t.Surface;
        p.DrawerText = t.Text2;
        p.DrawerIcon = t.Text2;
        p.AppbarBackground = t.Surface;
        p.AppbarText = t.Text;

        p.TextPrimary = t.Text;
        p.TextSecondary = t.Text2;
        p.TextDisabled = t.Text3;
        p.ActionDefault = t.Text2;
        p.ActionDisabled = t.Text3;
        p.ActionDisabledBackground = t.Hover;

        p.Divider = t.Border;
        p.DividerLight = t.Border;
        p.LinesDefault = t.Border;
        p.LinesInputs = t.Border;
        p.TableLines = t.Border;
        p.TableHover = t.Hover;
        p.TableStriped = t.Hover;

        p.GrayDefault = t.Text3;
        p.GrayLight = t.Border;
        p.GrayDark = t.Text2;
        p.GrayDarker = t.Hover;
        p.OverlayDark = "rgba(0,0,0,0.50)";
        p.OverlayLight = "rgba(255,255,255,0.05)";
    }

    private static Typography BuildTypography()
    {
        var t = new Typography();
        t.Default.FontFamily = ["Fira Sans", "system-ui", "Segoe UI", "sans-serif"];
        t.Default.LetterSpacing = "0";
        t.Button.TextTransform = "none";
        t.Button.LetterSpacing = "0.01em";
        return t;
    }
}
