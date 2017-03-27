using System;
using Xamarin.Auth.Forms;

namespace Xamarin.Auth
{
    public static class AuthenticatorExtensions
    {

        /// <summary>
        /// Gets the UI for this authenticator.
        /// </summary>
        /// <returns>
        /// The UI that needs to be presented.
        /// </returns>
        public static Xamarin.Forms.Page GetUI (this Authenticator authenticator)
		{
            var wa = authenticator as WebAuthenticator;
            if(wa != null)
            {
                var page = new WebAuthenticatorPage();
                page.BindingContext = authenticator;
                return page;
            }
            
            var fa = authenticator as FormAuthenticator;
            if (fa != null)
            {
                throw new NotSupportedException("FormsAuthenticator is not yet supported on Xamarin.Forms platform");
            }

            throw new NotSupportedException("No UI is defined for this authenticator type");
        }
    }
}