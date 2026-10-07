using WandEnhancer.View.Auth;

namespace WandEnhancer.Core.Auth
{
    internal static class AuthGate
    {
        public static bool EnsureAuthenticated()
        {
            AuthSession.Clear();

            string savedLicense = LicenseStore.Load();
            if (!string.IsNullOrWhiteSpace(savedLicense))
            {
                var service = new VisionAuthService();
                AuthResult result = service
                    .ValidateLicenseAsync(savedLicense)
                    .GetAwaiter()
                    .GetResult();

                if (result.Success)
                {
                    AuthSession.SetAuthenticated(savedLicense, result);
                    return true;
                }

                if (result.PermanentFailure)
                    LicenseStore.Clear();
            }

            var login = new LoginWindow();
            bool? accepted = login.ShowDialog();
            return accepted == true && AuthSession.IsAuthenticated;
        }
    }
}
