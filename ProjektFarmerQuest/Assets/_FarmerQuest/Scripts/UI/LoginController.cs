using FarmerQuest.Core;
using FarmerQuest.Net;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerQuest.UI
{
    /// <summary>
    /// Login / register
    /// </summary>
    public sealed class LoginController : MonoBehaviour
    {
        [SerializeField] private Text _subtitleLabel;
        [SerializeField] private GameObject _nameRow;
        [SerializeField] private InputField _nameField;
        [SerializeField] private InputField _emailField;
        [SerializeField] private InputField _passwordField;
        [SerializeField] private Toggle _rememberToggle;
        [SerializeField] private Button _submitButton;
        [SerializeField] private Button _toggleModeButton;
        [SerializeField] private Text _statusLabel;

        private bool _registerMode;
        private bool _busy;

        private void Start()
        {
            App.Init();
            if (_emailField == null || _submitButton == null)
            {
                Debug.LogError("[Login] UI missing in scene. Run FarmQuest Online/Configure Project.");
                return;
            }

            Wire();
            Prefill();
            ApplyModeUi();
        }

        private void Wire()
        {
            _submitButton.onClick.RemoveAllListeners();
            _submitButton.onClick.AddListener(OnSubmit);

            if (_toggleModeButton == null) return;
            _toggleModeButton.onClick.RemoveAllListeners();
            _toggleModeButton.onClick.AddListener(ToggleMode);
        }

        // Fills email/password from remembered credentials 
        private void Prefill()
        {
            if (App.Auth == null || !App.Auth.TryGetSavedCredentials(out string email, out string password))
                return;
            _emailField.text = email;
            if (_passwordField != null) _passwordField.text = password;
            if (_rememberToggle != null) _rememberToggle.isOn = true;
        }

        private void ToggleMode()
        {
            _registerMode = !_registerMode;
            ApplyModeUi();
        }

        // Switches labels and name field visibility between login and register 
        private void ApplyModeUi()
        {
            if (_subtitleLabel != null)
                _subtitleLabel.text = _registerMode ? "Opret konto" : "Log ind";
            if (_nameRow != null) _nameRow.SetActive(_registerMode);
            SetButtonText(_submitButton, _registerMode ? "Opret og log ind" : "Log ind");
            SetButtonText(_toggleModeButton,
                _registerMode ? "Har du en konto? Log ind" : "Ny bruger? Opret konto");
            SetStatus("");
        }

        private async void OnSubmit()
        {
            if (_busy) return;
            if (_passwordField == null) return;

            _busy = true;
            try
            {
                SetStatus("Forbinder...");
                bool remember = _rememberToggle == null || _rememberToggle.isOn;
                string email = _emailField.text.Trim();
                string password = _passwordField.text;

                AuthResponse res = _registerMode
                    ? await App.Sessions.RegisterAsync(_nameField?.text?.Trim(), email, password)
                    : await App.Sessions.LoginAsync(email, password);

                if (res == null || string.IsNullOrEmpty(res.token))
                {
                    SetStatus("Login mislykkedes.");
                    return;
                }

                App.Auth.Set(res.token, res.user, remember, email, password);
                SceneFlow.GoMainMenu();
            }
            catch (ApiException ex)
            {
                SetStatus($"{ex.StatusCode}: {ex.Message}");
            }
            finally
            {
                _busy = false;
            }
        }

        private void SetStatus(string t)
        {
            if (_statusLabel != null) _statusLabel.text = t;
        }

        private static void SetButtonText(Button btn, string text)
        {
            if (btn == null) return;
            Text label = btn.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }
    }
}
