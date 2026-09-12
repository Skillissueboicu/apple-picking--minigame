using System;
using Newtonsoft.Json;
using UnityEngine;

namespace FarmerQuest.Net
{
    /// <summary>
    /// Stores token + user. With "Remember me", also email/password for auto-login for X days
    /// </summary>
    public sealed class AuthSession
    {
        // Scope keys to this project dataPath so parallel checkouts do not clash
        private static readonly string ScopeKey = Application.dataPath.GetHashCode().ToString("X8");
        private static readonly string TokenKey = "@fqo_token_" + ScopeKey;
        private static readonly string UserKey = "@fqo_user_" + ScopeKey;
        private static readonly string TokenExpiryKey = "@fqo_token_expiry_" + ScopeKey;
        private static readonly string RememberUntilKey = "@fqo_remember_until_" + ScopeKey;
        private static readonly string EmailKey = "@fqo_email_" + ScopeKey;
        private static readonly string PasswordKey = "@fqo_pass_" + ScopeKey;
        private static readonly string RememberKey = "@fqo_remember_" + ScopeKey;

        public string Token { get; private set; }
        public UserInfo User { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(Token) && !IsTokenExpired;
        public int RememberDays { get; set; } = 7;

        private bool IsTokenExpired
        {
            get
            {
                long ticks = ParseTicks(TokenExpiryKey);
                return ticks > 0 && DateTime.UtcNow.Ticks > ticks;
            }
        }

        private bool IsRememberExpired
        {
            get
            {
                long ticks = ParseTicks(RememberUntilKey);
                return ticks <= 0 || DateTime.UtcNow.Ticks > ticks;
            }
        }

        // Persists auth token/user and optionally remembered credentials
        public void Set(string token, UserInfo user, bool rememberCredentials = false,
            string email = null, string password = null)
        {
            Token = token;
            User = user;

            PlayerPrefs.SetString(TokenKey, token ?? string.Empty);
            PlayerPrefs.SetString(UserKey, user != null ? JsonConvert.SerializeObject(user) : string.Empty);

            // Keep local token for the same window as "remember me" (server JWT may expire sooner)
            long until = DateTime.UtcNow.AddDays(Mathf.Max(1, RememberDays)).Ticks;
            PlayerPrefs.SetString(TokenExpiryKey, until.ToString());

            if (rememberCredentials && !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                PlayerPrefs.SetInt(RememberKey, 1);
                PlayerPrefs.SetString(EmailKey, email);
                PlayerPrefs.SetString(PasswordKey, password);
                PlayerPrefs.SetString(RememberUntilKey, until.ToString());
            }
            else if (!rememberCredentials)
            {
                ClearCredentials();
            }

            PlayerPrefs.Save();
        }

        // Loads token/user from PlayerPrefs, clearing them if the local expiry passed
        public void LoadFromPrefs()
        {
            Token = PlayerPrefs.GetString(TokenKey, string.Empty);
            string u = PlayerPrefs.GetString(UserKey, string.Empty);

            if (string.IsNullOrEmpty(Token) || IsTokenExpired)
            {
                Token = null;
                User = null;
                PlayerPrefs.DeleteKey(TokenKey);
                return;
            }

            User = string.IsNullOrEmpty(u) ? null : JsonConvert.DeserializeObject<UserInfo>(u);
        }

        // Returns saved email/password when 'remember me' is still valid
        public bool TryGetSavedCredentials(out string email, out string password)
        {
            email = null;
            password = null;
            if (PlayerPrefs.GetInt(RememberKey, 0) != 1) return false;
            if (IsRememberExpired)
            {
                ClearCredentials();
                return false;
            }

            email = PlayerPrefs.GetString(EmailKey, string.Empty);
            password = PlayerPrefs.GetString(PasswordKey, string.Empty);
            return !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password);
        }

        // Removes remembered email/password keys only
        public void ClearCredentials()
        {
            PlayerPrefs.DeleteKey(RememberKey);
            PlayerPrefs.DeleteKey(EmailKey);
            PlayerPrefs.DeleteKey(PasswordKey);
            PlayerPrefs.DeleteKey(RememberUntilKey);
        }

        // Clears token, user, and remembered credentials
        public void Clear()
        {
            Token = null;
            User = null;
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.DeleteKey(UserKey);
            PlayerPrefs.DeleteKey(TokenExpiryKey);
            ClearCredentials();
            PlayerPrefs.Save();
        }

        private static long ParseTicks(string key) =>
            long.TryParse(PlayerPrefs.GetString(key, "0"), out long t) ? t : 0;
    }
}
