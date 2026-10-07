using System;
using System.Windows;
using WandEnhancer.Core.Auth;

namespace WandEnhancer.View.Auth
{
    public partial class LoginWindow : Window
    {
        private readonly VisionAuthService _visionAuth = new VisionAuthService();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LicenseKeyBox.Focus();

            if (!VisionAuthConfig.IsConfigured)
                StatusText.Text = "Vision Auth pendente de configuração.";
        }

        private async void OnLogin(object sender, RoutedEventArgs e)
        {
            string licenseKey = (LicenseKeyBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                StatusText.Text = "Digite sua License Key.";
                return;
            }

            LoginButton.IsEnabled = false;
            LicenseKeyBox.IsEnabled = false;
            StatusText.Text = "Verificando licença...";

            try
            {
                AuthResult result = await _visionAuth.ValidateLicenseAsync(licenseKey);
                if (!result.Success)
                {
                    StatusText.Text = result.Message;
                    return;
                }

                LicenseStore.Save(licenseKey);
                AuthSession.SetAuthenticated(licenseKey, result);
                StatusText.Text = "Licença válida.";
                DialogResult = true;
            }
            catch (Exception)
            {
                StatusText.Text = "Não foi possível concluir a autenticação.";
            }
            finally
            {
                if (!AuthSession.IsAuthenticated)
                {
                    LoginButton.IsEnabled = true;
                    LicenseKeyBox.IsEnabled = true;
                }
            }
        }
    }
}
