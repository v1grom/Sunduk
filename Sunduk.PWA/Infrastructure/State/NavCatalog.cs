using System;
using System.Collections.Generic;
using System.Linq;
using MudBlazor;

namespace Sunduk.PWA.Infrastructure.State
{
    /// <summary>
    /// Пункт навигации: раздел верхнего уровня или его вложенная вкладка. Любой пункт можно
    /// вынести в нижнюю панель, поэтому у каждого есть собственный адрес.
    /// </summary>
    /// <param name="Href">Адрес. У вкладки это адрес раздела + «/» + ключ вкладки.</param>
    /// <param name="Title">Полное название (боковое меню, вкладки, настройки).</param>
    /// <param name="ShortTitle">Короткая подпись под значком в нижней панели.</param>
    /// <param name="ParentHref">Адрес раздела, которому принадлежит вкладка; у разделов верхнего уровня — null.</param>
    public record NavSection(string Href, string Title, string ShortTitle, string Icon, string? ParentHref = null)
    {
        /// <summary> Последний сегмент адреса: ключ вкладки внутри раздела. </summary>
        public string Key => Href[(Href.LastIndexOf('/') + 1)..];
    }

    /// <summary>
    /// Единое дерево разделов приложения: из него строятся боковое меню, главная страница,
    /// вкладки страниц и выбор пунктов нижней панели.
    /// </summary>
    public static class NavCatalog
    {
        /// <summary> Сколько пунктов в нижней панели (ещё одна кнопка «Ещё» есть всегда). </summary>
        public const int Slots = 4;

        private static NavSection Root(string href, string title, string shortTitle, string icon) => new(href, title, shortTitle, icon);

        private static NavSection Tab(string parent, string key, string title, string shortTitle, string icon) =>
            new($"{parent}/{key}", title, shortTitle, icon, parent);

        public const string Calculator = "/calculator";
        public const string Turning = "/turning";
        public const string Milling = "/milling";
        public const string Geometry = "/geometry";
        public const string Material = "/material";
        public const string TimeCalc = "/timecalc";
        public const string Knowledge = "/knowledge";

        /// <summary> Все пункты: сначала раздел, сразу за ним его вкладки. </summary>
        public static readonly IReadOnlyList<NavSection> All = new NavSection[]
        {
            Root(Calculator, "Режимы", "Режимы", Icons.Material.Filled.Calculate),

            Root(Turning, "Токарка", "Токарка", Icons.Material.Filled.Loop),
            Tab(Turning, "thread", "Резьба", "Резьба", Icons.Material.Filled.Tune),
            Tab(Turning, "bore", "Расточка", "Расточка", Icons.Material.Filled.PanoramaFishEye),
            Tab(Turning, "roughness", "Шероховатость", "Ra по f", Icons.Material.Filled.Texture),

            Root(Milling, "Фрезеровка", "Фрезы", Icons.Material.Filled.Grain),
            Tab(Milling, "chip-thinning", "Утонение стружки", "Утонение", Icons.Material.Filled.CompareArrows),
            Tab(Milling, "high-feed", "High feed", "High feed", Icons.Material.Filled.FastForward),
            Tab(Milling, "ball", "Сферическая фреза", "Сфера", Icons.Material.Filled.Circle),

            Root(Geometry, "Геометрия", "Геометрия", Icons.Material.Filled.RoundedCorner),
            Tab(Geometry, "chamfer", "Фаска", "Фаска", Icons.Material.Filled.Crop),
            Tab(Geometry, "arcs", "Дуги", "Дуги", Icons.Material.Filled.Timeline),
            Tab(Geometry, "drill-angle", "Конус сверла", "Сверло", Icons.Material.Filled.ArrowDownward),
            Tab(Geometry, "point", "Координаты точки", "Точка", Icons.Material.Filled.MyLocation),
            Tab(Geometry, "angles", "Углы", "Углы", Icons.Material.Filled.Architecture),

            Root(Material, "Допуски и материал", "Допуски", Icons.Material.Filled.Straighten),
            Tab(Material, "tolerance", "Допуски", "Допуски", Icons.Material.Filled.Straighten),
            Tab(Material, "hardness", "Твёрдость", "Твёрдость", Icons.Material.Filled.FitnessCenter),
            Tab(Material, "ra-rz", "Ra ↔ Rz", "Ra↔Rz", Icons.Material.Filled.SwapHoriz),

            Root(TimeCalc, "Расчёты времени", "Время", Icons.Material.Filled.Timer),
            Tab(TimeCalc, "turning", "Точение", "Точение", Icons.Material.Filled.Loop),
            Tab(TimeCalc, "sawing", "Распиловка", "Пила", Icons.Material.Filled.ContentCut),

            Root(Knowledge, "База знаний", "База", Icons.Material.Filled.MenuBook),
            Tab(Knowledge, "formulas", "Формулы", "Формулы", Icons.Material.Filled.Functions),
            Tab(Knowledge, "center-length", "Глубина центровки", "Центровка", Icons.Material.Filled.VerticalAlignBottom),
            Tab(Knowledge, "bspp-thread", "Резьба BSPP (G)", "BSPP", Icons.Material.Filled.Info),
            Tab(Knowledge, "npt-thread", "Резьба NPT (K)", "NPT", Icons.Material.Filled.Info),
            Tab(Knowledge, "vectors", "Векторы", "Векторы", Icons.Material.Filled.NorthEast),
            Tab(Knowledge, "macro-b", "Fanuc: Macro B", "Macro B", Icons.Material.Filled.DataObject),
            Tab(Knowledge, "custom-victor-cycles", "Fanuc: циклы Victor", "Victor", Icons.Material.Filled.Autorenew),

            Root("/dynamic-templates", "Шаблонник", "Шаблоны", Icons.Material.Outlined.Code),
            Root("/sunducam", "SunduCAM", "CAM", Icons.Material.Outlined.FeaturedPlayList),
        };

        /// <summary> Разделы верхнего уровня в порядке меню. </summary>
        public static readonly IReadOnlyList<NavSection> Roots = All.Where(s => s.ParentHref is null).ToList();

        /// <summary> Набор нижней панели по умолчанию. </summary>
        public static readonly IReadOnlyList<string> DefaultSlots = new[]
        {
            Calculator, Turning, Milling, Material + "/tolerance",
        };

        public static NavSection? Get(string href) =>
            All.FirstOrDefault(s => s.Href.Equals(href, StringComparison.OrdinalIgnoreCase));

        /// <summary> Вкладки раздела в порядке показа. </summary>
        public static IReadOnlyList<NavSection> ChildrenOf(string href) =>
            All.Where(s => string.Equals(s.ParentHref, href, StringComparison.OrdinalIgnoreCase)).ToList();

        /// <summary> Пункт с самым длинным адресом, которому принадлежит путь (например, «milling/high-feed»). </summary>
        public static NavSection? Find(string relativePath)
        {
            var path = "/" + relativePath.Split('?', '#')[0].Trim('/');
            return All
                .Where(s => path.Equals(s.Href, StringComparison.OrdinalIgnoreCase)
                         || path.StartsWith(s.Href + "/", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.Href.Length)
                .FirstOrDefault();
        }

        /// <summary> Полное название для списка выбора: у вкладки — вместе с названием раздела. </summary>
        public static string Label(NavSection section) =>
            section.ParentHref is { } parent && Get(parent) is { } owner ? $"{owner.Title} › {section.Title}" : section.Title;

        public static IReadOnlyList<NavSection> Resolve(IEnumerable<string> hrefs) =>
            Normalize(hrefs).Select(h => Get(h)!).ToList();

        /// <summary>
        /// Оставляет только существующие неповторяющиеся адреса и дополняет набор до <see cref="Slots"/>
        /// пунктами по умолчанию: разделы переименовываются, а сохранённые адреса остаются в хранилище.
        /// </summary>
        public static List<string> Normalize(IEnumerable<string>? hrefs)
        {
            var result = new List<string>();
            foreach (var href in (hrefs ?? []).Concat(DefaultSlots).Concat(All.Select(s => s.Href)))
            {
                if (result.Count == Slots) break;
                var section = Get(href);
                if (section is not null && !result.Contains(section.Href)) result.Add(section.Href);
            }
            return result;
        }
    }
}
