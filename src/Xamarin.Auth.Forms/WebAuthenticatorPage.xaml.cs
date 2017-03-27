using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Xamarin.Auth.Forms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WebAuthenticatorPage : ContentPage
    {
        private WebAuthenticator _auth;

        public WebAuthenticatorPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            _auth = (WebAuthenticator)BindingContext;

            if (_auth == null)
                throw new InvalidOperationException("Expected WebAuthenticator as NavigationEventArgs.Parameter");

            _auth.Completed += OnAuthCompleted;
            _auth.Error += OnAuthError;

            Uri uri = await _auth.GetInitialUrlAsync();
            this.browser.Source = uri;
            

            this.browser.Navigating += Browser_Navigating;
            this.browser.Navigated += Browser_Navigated;

            if (_auth.ClearCookiesBeforeLogin)
            {
                
            }

            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            _auth.Completed -= OnAuthCompleted;
            _auth.Error -= OnAuthError;

            base.OnDisappearing();
        }


        private void Browser_Navigating(object sender, WebNavigatingEventArgs e)
        {
            _auth.OnPageLoading(new Uri(e.Url));
        }
        private void Browser_Navigated(object sender, WebNavigatedEventArgs e)
        {
            _auth.OnPageLoaded(new Uri(e.Url));;
        }

        private async void OnAuthError(object sender, AuthenticatorErrorEventArgs e)
        {
            await NavigateBack();
        }

        private async void OnAuthCompleted(object sender, AuthenticatorCompletedEventArgs e)
        {
            await NavigateBack();
        }

        private async void BackButton_Click(object sender, EventArgs eventArgs)
        {
            await NavigateBack();
        }

        private async Task NavigateBack()
        {
            try
            {
                await this.Navigation.PopAsync();
            }
            catch (Exception x)
            {
                System.Diagnostics.Debug.WriteLine($"{x.GetType().FullName}: {x.Message}\n{x.StackTrace}");
            }
        }
    }
}
