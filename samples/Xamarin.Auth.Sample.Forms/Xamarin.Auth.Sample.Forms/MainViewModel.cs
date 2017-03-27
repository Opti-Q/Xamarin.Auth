using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Json;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Xamarin.Auth.Sample.Forms
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ICommand _loginCommand;
        private string _loginResult;

        public ICommand LoginCommand
        {
            get { return _loginCommand ?? (_loginCommand = new Command(Login)); }
        }

        public string LoginResult
        {
            get { return _loginResult; }
            set
            {
                _loginResult = value;
                OnPropertyChanged();
            }
        }

        public event EventHandler LoginRequested;

        internal Authenticator GetAuthenticator()
        {
            var auth = new OAuth2Authenticator(
                clientId: Constants.KEY,
                scope: "",
                authorizeUrl: new Uri("https://m.facebook.com/dialog/oauth/"),
                redirectUrl: new Uri("http://www.facebook.com/connect/login_success.html"));

            auth.Title = "Please login in this sample app";
            auth.BackbuttonText = "< Back";
            auth.AllowCancel = true;

            // If authorization succeeds or is canceled, .Completed will be fired.
            auth.Completed += async (s, ee) => {
                if (!ee.IsAuthenticated)
                {
                    LoginResult = "An unknown error occurred ";
                    return;
                }

                // Now that we're logged in, make a OAuth2 request to get the user's info.
                var request = new OAuth2Request("GET", new Uri("https://graph.facebook.com/me"), null, ee.Account);
                try
                {
                    var result = await request.GetResponseAsync();

                    var obj = JsonValue.Parse(result.GetResponseText());
                    LoginResult = $"user '{obj["name"]}' successfully logged in";
                }
                catch (TaskCanceledException x)
                {
                    LoginResult = "User cancelled";
                }
                catch (Exception x)
                {
                    LoginResult = $"An error occurred: " + x.Message;
                }
            };

            return auth;
        }

        private void Login()
        {
            LoginRequested?.Invoke(this, EventArgs.Empty);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
