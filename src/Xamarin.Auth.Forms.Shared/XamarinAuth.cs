using System;
using System.Collections.Generic;
using System.Text;

namespace Xamarin.Auth.Forms
{
    public static class XamarinAuth
    {
        /// <summary>
        /// Initializes XamarinAuth forms plugin
        /// </summary>
        /// <returns>
        /// The UI that needs to be presented.
        /// </returns>
#if PLATFORM_IOS
		public static void Init()
        {
        }
#elif PLATFORM_ANDROID
		public static void Init()
        {
        }
#elif WINDOWS_UWP
        public static void Init()
        {

        }
#endif
    }
}
