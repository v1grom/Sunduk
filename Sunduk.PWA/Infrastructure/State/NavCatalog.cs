using System;
using System.Collections.Generic;
using System.Linq;
using MudBlazor;

namespace Sunduk.PWA.Infrastructure.State
{
    /// <summary> Группа разделов в меню и на главной странице. </summary>
    public enum NavGroup
    {
        /// <summary> Зашёл, выбрал случай — получил числа и пример программы. </summary>
        Calculations,
        /// <summary> Таблицы и справочные материалы. </summary>
        Reference,
        /// <summary> Генерация управляющих программ. </summary>
        Programming,
    }

    /// <summary>
    /// Пункт навигации: раздел верхнего уровня или его вложенная вкладка. Любой пункт можно
    /// вынести в нижнюю панель, поэтому у каждого есть собственный адрес.
    /// </summary>
    /// <param name="Href">Адрес. У вкладки это адрес раздела + «/» + ключ вкладки.</param>
    /// <param name="Title">Полное название (боковое меню, вкладки, настройки).</param>
    /// <param name="ShortTitle">Короткая подпись под значком в нижней панели.</param>
    /// <param name="Group">Группа раздела; у вкладки — группа её раздела.</param>
    /// <param name="ParentHref">Адрес раздела, которому принадлежит вкладка; у разделов верхнего уровня — null.</param>
    public record NavSection(string Href, string Title, string ShortTitle, string Icon, NavGroup Group, string? ParentHref = null)
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

        private static NavSection Root(NavGroup group, string href, string title, string shortTitle, string icon) =>
            new(href, title, shortTitle, icon, group);

        private static NavSection Tab(NavSection parent, string key, string title, string shortTitle, string icon) =>
            new($"{parent.Href}/{key}", title, shortTitle, icon, parent.Group, parent.Href);

        public const string Calculator = "/calculator";
        public const string Turning = "/turning";
        public const string Milling = "/milling";
        public const string Geometry = "/geometry";
        public const string Tolerance = "/tolerance";
        public const string TimeCalc = "/timecalc";
        public const string Knowledge = "/knowledge";

        private static readonly NavSection TurningRoot = Root(NavGroup.Calculations, Turning, "Токарка", "Токарка", Icons.Material.Filled.Loop);
        private static readonly NavSection MillingRoot = Root(NavGroup.Calculations, Milling, "Фрезеровка", "Фрезерка", Icons.Material.Filled.Grain);
        private static readonly NavSection GeometryRoot = Root(NavGroup.Calculations, Geometry, "Геометрия", "Геометрия", Icons.Material.Filled.RoundedCorner);
        private static readonly NavSection TimeCalcRoot = Root(NavGroup.Calculations, TimeCalc, "Время", "Время", Icons.Material.Filled.Timer);
        private static readonly NavSection KnowledgeRoot = Root(NavGroup.Reference, Knowledge, "Справочник", "Справочник", Icons.Material.Filled.MenuBook);

        /// <summary> Все пункты: сначала раздел, сразу за ним его вкладки. </summary>
        public static readonly IReadOnlyList<NavSection> All = new NavSection[]
        {
            Root(NavGroup.Calculations, Calculator, "Режимы", "Режимы", Icons.Material.Filled.Calculate),

            TurningRoot,
            Tab(TurningRoot, "thread", "Резьба", "Резьба", Icons.Material.Filled.Tune),
            Tab(TurningRoot, "chamfer", "Фаска", "Фаска", Icons.Material.Filled.Crop),
            Tab(TurningRoot, "arcs", "Дуги", "Дуги", Icons.Material.Filled.Timeline),
            Tab(TurningRoot, "bore", "Расточка", "Расточка", Icons.Material.Filled.PanoramaFishEye),
            Tab(TurningRoot, "roughness", "Шероховатость", "Ra по f", Icons.Material.Filled.Texture),

            MillingRoot,
            Tab(MillingRoot, "chamfer", "Фаска", "Фаска", Icons.Material.Filled.ChangeHistory),
            Tab(MillingRoot, "chip-thinning", "Утонение стружки", "Утонение", Icons.Material.Filled.CompareArrows),
            Tab(MillingRoot, "high-feed", "High feed", "High feed", Icons.Material.Filled.FastForward),
            Tab(MillingRoot, "ball", "Сферическая фреза", "Сфера", Icons.Material.Filled.Circle),

            GeometryRoot,
            Tab(GeometryRoot, "drill-angle", "Конус сверла", "Сверло", Icons.Material.Filled.ArrowDownward),
            Tab(GeometryRoot, "point", "Координаты точки", "Точка", Icons.Material.Filled.MyLocation),
            Tab(GeometryRoot, "angles", "Углы", "Углы", Icons.Material.Filled.Architecture),

            Root(NavGroup.Calculations, Tolerance, "Допуски", "Допуски", Icons.Material.Filled.Straighten),

            TimeCalcRoot,
            Tab(TimeCalcRoot, "turning", "Точение", "Точение", Icons.Material.Filled.Loop),
            Tab(TimeCalcRoot, "sawing", "Распиловка", "Пила", Icons.Material.Filled.ContentCut),

            KnowledgeRoot,
            Tab(KnowledgeRoot, "bspp-thread", "Резьба BSPP (G)", "BSPP", Icons.Material.Filled.Info),
            Tab(KnowledgeRoot, "npt-thread", "Резьба NPT (K)", "NPT", Icons.Material.Filled.Info),
            Tab(KnowledgeRoot, "center-length", "Глубина центровки", "Центровка", Icons.Material.Filled.VerticalAlignBottom),
            Tab(KnowledgeRoot, "hardness", "Твёрдость", "Твёрдость", Icons.Material.Filled.FitnessCenter),
            Tab(KnowledgeRoot, "ra-rz", "Ra ↔ Rz", "Ra↔Rz", Icons.Material.Filled.SwapHoriz),
            Tab(KnowledgeRoot, "formulas", "Формулы", "Формулы", Icons.Material.Filled.Functions),
            Tab(KnowledgeRoot, "vectors", "Векторы", "Векторы", Icons.Material.Filled.NorthEast),
            Tab(KnowledgeRoot, "macro-b", "Fanuc: Macro B", "Macro B", Icons.Material.Filled.DataObject),

            Root(NavGroup.Programming, "/dynamic-templates", "Шаблоны", "Шаблоны", Icons.Material.Outlined.Code),
            Root(NavGroup.Programming, "/sunducam", "SunduCAM", "CAM", Icons.Material.Outlined.FeaturedPlayList),
        };

        /// <summary> Разделы верхнего уровня в порядке меню. </summary>
        public static readonly IReadOnlyList<NavSection> Roots = All.Where(s => s.ParentHref is null).ToList();

        /// <summary> Разделы верхнего уровня по группам, в порядке меню. </summary>
        public static readonly IReadOnlyList<IGrouping<NavGroup, NavSection>> Groups = Roots.GroupBy(s => s.Group).ToList();

        /// <summary> Подпись группы над её разделами в меню и на главной. </summary>
        public static string GroupTitle(NavGroup group) => group switch
        {
            NavGroup.Calculations => "Расчёты",
            NavGroup.Reference => "Справочник",
            NavGroup.Programming => "Программирование",
            _ => string.Empty,
        };

        /// <summary> Набор нижней панели по умолчанию. </summary>
        public static readonly IReadOnlyList<string> DefaultSlots = new[]
        {
            Calculator, Turning, Milling, Tolerance,
        };

        /// <summary>
        /// Куда переехали старые адреса. По этой таблице работают перенаправления страниц и перенос
        /// сохранённых пунктов нижней панели и счётчика заходов, чтобы выбор пользователя не терялся.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> Moved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["/stuff"] = Turning,
            ["/converters"] = Geometry,
            ["/material"] = Tolerance,
            ["/material/tolerance"] = Tolerance,
            ["/material/hardness"] = Knowledge + "/hardness",
            ["/material/ra-rz"] = Knowledge + "/ra-rz",
            ["/geometry/chamfer"] = Turning + "/chamfer",
            ["/geometry/arcs"] = Turning + "/arcs",
            ["/knowledge/custom-victor-cycles"] = Knowledge,
        };

        /// <summary> Новый адрес для переехавшего; актуальный адрес возвращается как есть. </summary>
        public static string Migrate(string href)
        {
            var path = href.TrimEnd('/');
            return Moved.TryGetValue(path, out var target) ? target : href;
        }

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
        /// Переносит переехавшие адреса, оставляет только существующие неповторяющиеся и дополняет набор
        /// до <see cref="Slots"/> пунктами по умолчанию: сохранённые адреса остаются в хранилище.
        /// </summary>
        public static List<string> Normalize(IEnumerable<string>? hrefs)
        {
            var result = new List<string>();
            foreach (var href in (hrefs ?? []).Select(Migrate).Concat(DefaultSlots).Concat(All.Select(s => s.Href)))
            {
                if (result.Count == Slots) break;
                var section = Get(href);
                if (section is not null && !result.Contains(section.Href)) result.Add(section.Href);
            }
            return result;
        }
    }
}
