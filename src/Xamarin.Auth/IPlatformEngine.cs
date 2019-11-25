using System;
using System.Threading.Tasks;

namespace Xamarin.Auth
{
    public interface IPlatformEngine
    {
        IAccountStore Create(char[] password = null);
        Task InvokeOnMainThread(Action action);
        IDisposable Disable100();
        Task ClearCookiesAsync();
    }
}
