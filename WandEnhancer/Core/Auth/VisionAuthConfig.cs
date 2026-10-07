using System;

namespace WandEnhancer.Core.Auth
{
    internal static class VisionAuthConfig
    {
        public const string ApiUrl = "https://visionstore.discloud.app/api/v1/license/activate";
        public const string LoaderVersion = "1.0.0";

        public static string LoaderToken
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(VisionAuthBuildConfig.LoaderToken))
                    return VisionAuthBuildConfig.LoaderToken.Trim();

                string runtime = Environment.GetEnvironmentVariable("WG_VISION_LOADER_TOKEN");
                return string.IsNullOrWhiteSpace(runtime) ? string.Empty : runtime.Trim();
            }
        }

        public static bool IsConfigured => !string.IsNullOrWhiteSpace(LoaderToken);
    }
}
