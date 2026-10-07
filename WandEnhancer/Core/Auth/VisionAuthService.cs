using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WandEnhancer.Core.Auth
{
    internal sealed class VisionAuthService
    {
        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        public async Task<AuthResult> ValidateLicenseAsync(string licenseKey)
        {
            if (!VisionAuthConfig.IsConfigured)
                return AuthResult.Fail("Vision Auth ainda não foi configurado neste build.");

            if (string.IsNullOrWhiteSpace(licenseKey))
                return AuthResult.Fail("Digite sua License Key.", true);

            try
            {
                var payload = new JObject
                {
                    ["licenseKey"] = licenseKey.Trim(),
                    ["hwid"] = HardwareId.Get(),
                    ["loaderVersion"] = VisionAuthConfig.LoaderVersion
                };

                using (var request = new HttpRequestMessage(HttpMethod.Post, VisionAuthConfig.ApiUrl))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", VisionAuthConfig.LoaderToken);
                    request.Content = new StringContent(
                        payload.ToString(Formatting.None),
                        Encoding.UTF8,
                        "application/json");

                    using (HttpResponseMessage response = await Http.SendAsync(request).ConfigureAwait(false))
                    {
                        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        JObject json = ParseJson(body);

                        bool success =
                            ReadBool(json, "ok") ??
                            ReadBool(json, "success") ??
                            ReadBool(json, "valid") ??
                            ReadBool(json, "active") ??
                            ReadBool(json, "authorized") ??
                            response.IsSuccessStatusCode;

                        string message =
                            ReadString(json, "message") ??
                            ReadString(json, "msg") ??
                            ReadString(json, "detail");

                        if (!success || !response.IsSuccessStatusCode)
                        {
                            string friendly = FriendlyMessage(message, response.StatusCode);
                            return AuthResult.Fail(
                                friendly,
                                IsPermanentFailure(message, response.StatusCode));
                        }

                        var result = AuthResult.Ok(
                            string.IsNullOrWhiteSpace(message) ? "Licença válida." : message);

                        long days;
                        if (TryReadLong(json, out days,
                            "daysRemaining",
                            "days_remaining",
                            "remainingDays",
                            "daysLeft"))
                        {
                            result.TimeLeftSeconds = days < 0 ? (long?)null : days * 86400L;
                        }

                        string expires =
                            ReadString(json, "expiresAt") ??
                            ReadString(json, "expires_at") ??
                            ReadString(json, "expiration") ??
                            ReadString(json, "expires");

                        DateTimeOffset parsedExpiry;
                        if (!string.IsNullOrWhiteSpace(expires) &&
                            DateTimeOffset.TryParse(expires, out parsedExpiry))
                        {
                            result.Expiry = parsedExpiry;
                            if (!result.TimeLeftSeconds.HasValue)
                            {
                                double seconds = (parsedExpiry - DateTimeOffset.UtcNow).TotalSeconds;
                                result.TimeLeftSeconds = seconds > 0 ? (long)seconds : 0;
                            }
                        }

                        result.Subscription =
                            ReadString(json, "plan") ??
                            ReadString(json, "subscription") ??
                            ReadString(json, "licenseType") ??
                            ReadString(json, "license_type");

                        result.Username =
                            ReadString(json, "username") ??
                            ReadString(json, "user");

                        return result;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return AuthResult.Fail("Tempo esgotado ao conectar ao Vision Auth.");
            }
            catch (HttpRequestException)
            {
                return AuthResult.Fail("Não foi possível conectar ao Vision Auth.");
            }
            catch
            {
                return AuthResult.Fail("Falha inesperada ao validar a licença.");
            }
        }

        private static JObject ParseJson(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return new JObject();

            try
            {
                return JObject.Parse(body);
            }
            catch
            {
                return new JObject
                {
                    ["message"] = body.Trim()
                };
            }
        }

        private static bool? ReadBool(JObject json, string name)
        {
            JToken token = FindToken(json, name);
            if (token == null)
                return null;

            bool value;
            if (bool.TryParse(token.ToString(), out value))
                return value;

            int number;
            if (int.TryParse(token.ToString(), out number))
                return number != 0;

            return null;
        }

        private static string ReadString(JObject json, string name)
        {
            JToken token = FindToken(json, name);
            return token == null ? null : token.ToString();
        }

        private static bool TryReadLong(JObject json, out long value, params string[] names)
        {
            value = 0;

            foreach (string name in names)
            {
                JToken token = FindToken(json, name);
                if (token != null && long.TryParse(token.ToString(), out value))
                    return true;
            }

            return false;
        }

        private static JToken FindToken(JObject json, string name)
        {
            if (json == null)
                return null;

            JToken direct;
            if (json.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out direct))
                return direct;

            foreach (JProperty property in json.Properties())
            {
                JObject nested = property.Value as JObject;
                if (nested == null)
                    continue;

                JToken found = FindToken(nested, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static bool IsPermanentFailure(string message, HttpStatusCode status)
        {
            string lower = (message ?? string.Empty).ToLowerInvariant();

            return status == HttpStatusCode.Unauthorized ||
                   status == HttpStatusCode.Forbidden ||
                   lower.Contains("invalid") ||
                   lower.Contains("expired") ||
                   lower.Contains("already linked") ||
                   lower.Contains("hwid") ||
                   lower.Contains("ban");
        }

        private static string FriendlyMessage(string message, HttpStatusCode status)
        {
            string lower = (message ?? string.Empty).ToLowerInvariant();

            if (lower.Contains("hwid") || lower.Contains("already linked"))
                return "Licença já vinculada a outro HWID ou HWID incompatível.";
            if (lower.Contains("expired"))
                return "Licença expirada.";
            if (lower.Contains("ban"))
                return "Licença bloqueada.";
            if (lower.Contains("invalid") || lower.Contains("not found"))
                return "Key inválida.";
            if (status == HttpStatusCode.Unauthorized || status == HttpStatusCode.Forbidden)
                return "Vision Auth recusou a autenticação do loader.";

            return string.IsNullOrWhiteSpace(message)
                ? "Vision Auth recusou a licença."
                : message;
        }
    }
}
