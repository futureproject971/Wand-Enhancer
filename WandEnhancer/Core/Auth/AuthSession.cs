namespace WandEnhancer.Core.Auth
{
    internal static class AuthSession
    {
        public static bool IsAuthenticated { get; private set; }
        public static string LicenseKey { get; private set; }
        public static AuthResult Current { get; private set; }

        public static void SetAuthenticated(string licenseKey, AuthResult result)
        {
            LicenseKey = licenseKey;
            Current = result;
            IsAuthenticated = result != null && result.Success;
        }

        public static void Clear()
        {
            LicenseKey = null;
            Current = null;
            IsAuthenticated = false;
        }
    }
}
