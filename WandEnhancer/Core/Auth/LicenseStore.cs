using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WandEnhancer.Core.Auth
{
    internal static class LicenseStore
    {
        private static readonly byte[] Entropy =
            Encoding.UTF8.GetBytes("WG Enhancer|WORLD GAMES|LicenseStore|v1");

        private static string DirectoryPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WGEnhancer");

        private static string FilePath => Path.Combine(DirectoryPath, "license.dat");

        public static string Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return null;

                byte[] protectedData = File.ReadAllBytes(FilePath);
                byte[] clearData = ProtectedData.Unprotect(
                    protectedData,
                    Entropy,
                    DataProtectionScope.CurrentUser);

                string key = Encoding.UTF8.GetString(clearData).Trim();
                return string.IsNullOrWhiteSpace(key) ? null : key;
            }
            catch
            {
                return null;
            }
        }

        public static void Save(string licenseKey)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
                throw new ArgumentException("License key is required.", nameof(licenseKey));

            Directory.CreateDirectory(DirectoryPath);

            byte[] clearData = Encoding.UTF8.GetBytes(licenseKey.Trim());
            byte[] protectedData = ProtectedData.Protect(
                clearData,
                Entropy,
                DataProtectionScope.CurrentUser);

            File.WriteAllBytes(FilePath, protectedData);
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch
            {
                // Authentication still fails closed if stale data cannot be removed.
            }
        }
    }
}
