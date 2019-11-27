using System;
using System.Threading.Tasks;
#if MONOANDROID
using Android.OS;
using Android.App;
using Android.Webkit;
#elif WINDOWS_UWP
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
#elif XAMARIN_IOS
using Foundation;
using UIKit;
#endif

namespace Xamarin.Auth
{
    public class Platform : IPlatformEngine
    {
        public static IPlatformEngine Engine = new Platform();

#if MONOANDROID
        public IAccountStore Create(char[] password = null)
        {
            return new AndroidAccountStore(password);
        }
        
        public Task InvokeOnMainThread(Action action)
        {
            // http://stackoverflow.com/questions/12850143/android-basics-running-code-in-the-ui-thread
            new Handler(Looper.MainLooper).Post(action);
            return Task.FromResult(true);
        }

        public IDisposable Disable100()
        {
            return new ServicePointManagerDispabler();
        }

        public Task ClearCookiesAsync()
        {
            CookieSyncManager.CreateInstance(Application.Context);
            CookieManager.Instance.RemoveAllCookie();
            return Task.FromResult(true);
        }
#elif XAMARIN_IOS
        public IAccountStore Create(char[] password = null)
        {
            return new KeyChainAccountStore(password);
        }

        public Task InvokeOnMainThread(Action action)
        {
            UIApplication.SharedApplication.BeginInvokeOnMainThread(delegate {
                action();
            });
            return Task.FromResult(true);
        }

        public IDisposable Disable100()
        {
            return new ServicePointManagerDispabler();
        }
        
        public Task ClearCookiesAsync()
        {
            var store = NSHttpCookieStorage.SharedStorage;
            var cookies = store.Cookies;
            foreach (var c in cookies)
            {
                store.DeleteCookie(c);
            }
            return Task.FromResult(true);
        }

#elif WINDOWS_UWP
        public IAccountStore Create(char[] password = null)
        {
            return new UwpAccountStore(password);
        }

        public async Task InvokeOnMainThread(Action action)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { action(); });
        }

        public IDisposable Disable100()
        {
            return new ServicePointManagerDispabler();
        }

        public async Task ClearCookiesAsync()
        {
            await Windows.UI.Xaml.Controls.WebView.ClearTemporaryWebDataAsync();
        }
#else
        public IAccountStore Create(char[] password = null)
        {
            throw new NotImplementedException();
        }

        public Task InvokeOnMainThread(Action action)
        {
            throw new NotImplementedException();
        }

        public IDisposable Disable100()
        {
            throw new NotImplementedException();
        }
        public Task ClearCookiesAsync()
        {
            throw new NotImplementedException();
        }
#endif
    }
}
