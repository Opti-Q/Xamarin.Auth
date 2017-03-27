using System;
using Xamarin.Forms;

namespace Xamarin.Auth.Sample.Forms
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            var vm = this.BindingContext as MainViewModel;
            if (vm != null)
            {
                vm.LoginRequested += OnLoginRequested;
            }
        }

        private async void OnLoginRequested(object sender, EventArgs e)
        {
            var vm = (MainViewModel) sender;
            var authenticator = vm.GetAuthenticator();
            authenticator.Completed += Authenticator_Completed;

            // show authentication dialog
            var page = authenticator.GetUI();
            await Navigation.PushModalAsync(page);
        }

        private async void Authenticator_Completed(object sender, AuthenticatorCompletedEventArgs e)
        {
            // unsubscribe eventhandler to avoid memory leak
            var auth = (Authenticator)sender;
            auth.Completed -= Authenticator_Completed;
            
            // close dialog
            await Navigation.PopModalAsync();
        }
    }
}
