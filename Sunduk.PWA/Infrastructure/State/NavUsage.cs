using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.LocalStorage;

namespace Sunduk.PWA.Infrastructure.State
{
    /// <summary>
    /// Счётчик заходов в разделы и их вкладки. По нему нижняя панель в режиме «по использованию»
    /// подбирает самые частые пункты пользователя. Данные хранятся только в LocalStorage этого устройства.
    /// </summary>
    public class NavUsage
    {
        private const string StorageKey = "nav-usage";

        /// <summary> Сколько раз нужно зайти в пункт, чтобы он начал влиять на порядок. </summary>
        private const int MinVisitsToChange = 3;

        private readonly ILocalStorageService _storage;
        private Dictionary<string, int> _visits = new();
        private string? _lastHref;

        public NavUsage(ILocalStorageService storage) => _storage = storage;

        public async Task LoadAsync()
        {
            try
            {
                _visits = await _storage.GetItemAsync<Dictionary<string, int>>(StorageKey) ?? new();
            }
            catch
            {
                _visits = new();
            }
        }

        /// <summary> Засчитывает заход в самый глубокий пункт, которому принадлежит путь. Повторная загрузка того же пункта не считается. </summary>
        public async Task RecordAsync(string relativePath)
        {
            var section = NavCatalog.Find(relativePath);
            if (section is null || section.Href == _lastHref) return;
            _lastHref = section.Href;

            _visits[section.Href] = _visits.GetValueOrDefault(section.Href) + 1;
            try
            {
                await _storage.SetItemAsync(StorageKey, _visits);
            }
            catch
            {
                // Счётчик не критичен: без хранилища панель просто останется по умолчанию
            }
        }

        /// <summary>
        /// Пункты для панели: по убыванию заходов, при равенстве — сначала набор по умолчанию, затем остальные.
        /// Пункт учитывается, только если зашли в него не меньше <see cref="MinVisitsToChange"/> раз,
        /// чтобы случайные переходы не меняли панель.
        /// </summary>
        public IReadOnlyList<NavSection> Top()
        {
            int Score(NavSection s)
            {
                var visits = _visits.GetValueOrDefault(s.Href);
                return visits >= MinVisitsToChange ? visits : 0;
            }

            var ordered = NavCatalog.Resolve(NavCatalog.DefaultSlots)
                .Concat(NavCatalog.All.Where(s => !NavCatalog.DefaultSlots.Contains(s.Href)));

            return ordered
                .Select((s, i) => (Section: s, Index: i, Score: Score(s)))
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Index)
                .Take(NavCatalog.Slots)
                .Select(x => x.Section)
                .ToList();
        }
    }
}
