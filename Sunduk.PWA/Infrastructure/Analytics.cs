using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Sunduk.PWA.Infrastructure
{
    /// <summary>
    /// Анонимная статистика посещений (Umami на stats.sunduk.one). Вся работа в <c>wwwroot/js/analytics.js</c>:
    /// там же проверяется, включена ли статистика в параметрах и на каком адресе открыто приложение.
    /// Ошибки отправки никогда не доходят до пользователя.
    /// </summary>
    public class Analytics
    {
        private readonly IJSRuntime _js;

        public Analytics(IJSRuntime js) => _js = js;

        /// <summary> Просмотр страницы. <paramref name="relativePath"/> — путь относительно базового адреса. </summary>
        public async Task PageAsync(string relativePath)
        {
            try { await _js.InvokeVoidAsync("sundukAnalytics.pageview", relativePath); }
            catch { /* статистика не критична */ }
        }

        /// <summary> Именованное событие, например использование какой-нибудь функции. </summary>
        public async Task EventAsync(string name)
        {
            try { await _js.InvokeVoidAsync("sundukAnalytics.track", name); }
            catch { /* статистика не критична */ }
        }
    }
}
