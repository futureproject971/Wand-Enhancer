using System;

namespace WandEnhancer.Core.Auth
{
    public sealed class AuthResult
    {
        public bool Success { get; set; }
        public bool PermanentFailure { get; set; }
        public string Message { get; set; }
        public string Username { get; set; }
        public string Subscription { get; set; }
        public DateTimeOffset? Expiry { get; set; }
        public long? TimeLeftSeconds { get; set; }

        public static AuthResult Ok(string message)
        {
            return new AuthResult
            {
                Success = true,
                PermanentFailure = false,
                Message = message
            };
        }

        public static AuthResult Fail(string message, bool permanentFailure = false)
        {
            return new AuthResult
            {
                Success = false,
                PermanentFailure = permanentFailure,
                Message = message
            };
        }
    }
}
