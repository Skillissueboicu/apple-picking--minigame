using System.Text;
using FarmerQuest.Core;
using FarmerQuest.Net;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmerQuest.UI
{
    /// <summary>
    /// HUD for Farmer Quest Scene
    /// </summary>
    public sealed class FarmerQuestController : MonoBehaviour
    {
        [SerializeField] private Text _playersLabel;
        [SerializeField] private Text _codeLabel;
        [SerializeField] private Button _btnMenu;
        [SerializeField] private GameObject _menuPanel;
        [SerializeField] private Button _btnResume;
        [SerializeField] private Button _btnMainMenu;

        [SerializeField] private Button _btnLeave;

        private bool _alive = true;
        private float _poll;
        private bool _refreshInFlight;
        private readonly StringBuilder _playersSb = new StringBuilder(256);

        private void Start()
        {
            App.Init();

            // No active session --> back to main menu
            if (GameContext.Session?.Current == null && GameContext.CurrentSession == null)
            {
                SceneFlow.GoMainMenu();
                return;
            }

            // Fall back to leave button if menu was not wired
            if (_btnMenu == null)
                _btnMenu = _btnLeave;

            if (_playersLabel == null || _btnMenu == null ||
                _menuPanel == null || _btnResume == null || _btnMainMenu == null)
            {
                Debug.LogError("[FarmerQuest] Menu UI missing in scene. Wire PauseMenu refs (or run FarmQuest Online/Configure Project).");
                return;
            }

            _btnMenu.onClick.RemoveAllListeners();
            _btnMenu.onClick.AddListener(OpenMenu);

            _btnResume.onClick.RemoveAllListeners();
            _btnResume.onClick.AddListener(CloseMenu);

            _btnMainMenu.onClick.RemoveAllListeners();
            _btnMainMenu.onClick.AddListener(OnLeaveToMainMenu);

            _menuPanel.SetActive(false);

            Refresh(GameContext.Session?.Current ?? GameContext.CurrentSession);
            if (GameContext.Session != null)
                GameContext.Session.OnSessionUpdated += OnUpdated;
        }

        private void OnDestroy()
        {
            _alive = false;
            if (GameContext.Session != null)
                GameContext.Session.OnSessionUpdated -= OnUpdated;
        }

        private void Update()
        {
            // Escape closes the pause menu when it is open
            if (_menuPanel != null && _menuPanel.activeSelf)
            {
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame)
                {
                    CloseMenu();
                    return;
                }
            }

            // Session refresh (skip if a previous request is still running)
            _poll += Time.deltaTime;
            if (_poll < 2f || _refreshInFlight) return;
            _poll = 0f;
            _ = RefreshFromApiAsync();
        }

        // Shows the pause menu overlay
        private void OpenMenu()
        {
            if (_menuPanel == null) return;
            _menuPanel.SetActive(true);
            _menuPanel.transform.SetAsLastSibling();
        }

        // Hides the pause menu overlay
        private void CloseMenu()
        {
            if (_menuPanel != null)
                _menuPanel.SetActive(false);
        }

        // Leaves session and returns to the main menu
        private async void OnLeaveToMainMenu()
        {
            _alive = false;
            CloseMenu();
            try { await GameContext.Session?.LeaveAsync(); } catch { /* ignore leave errors */ }
            try { await GameContext.Session?.DisconnectAsync(); } catch { /* ignore disconnect errors */ }
            GameContext.Session = null;
            GameContext.CurrentSession = null;
            SceneFlow.GoMainMenu();
        }

        // Fetches the latest session by join code and refreshes the HUD
        private async System.Threading.Tasks.Task RefreshFromApiAsync()
        {
            SessionInfo current = GameContext.CurrentSession ?? GameContext.Session?.Current;
            if (current == null || string.IsNullOrEmpty(current.code)) return;

            _refreshInFlight = true;
            try
            {
                SessionInfo info = await App.Sessions.GetByCodeAsync(current.code);
                if (!_alive || info == null) return;
                GameContext.CurrentSession = info;
                Refresh(info);
            }
            catch { /* ignore temp poll failures */ }
            finally
            {
                _refreshInFlight = false;
            }
        }

        private void OnUpdated(SessionInfo info)
        {
            if (!_alive) return;
            GameContext.CurrentSession = info;
            Refresh(info);
        }

        // Updates code/status and the player list labels
        private void Refresh(SessionInfo info)
        {
            if (info == null) return;

            if (_codeLabel != null)
                _codeLabel.text = $"Kode {info.code} · {info.status}";

            if (_playersLabel == null) return;

            // Reuse StringBuilder to avoid per-poll allocations
            _playersSb.Clear();
            int n = info.players?.Count ?? 0;
            _playersSb.Append("Spillere (").Append(n).AppendLine(")");
            string myId = App.Auth?.User?.id;
            if (info.players != null)
            {
                foreach (SessionPlayer p in info.players)
                {
                    bool host = p.isHost || p.userId == info.hostUserId;
                    string tag = host ? "vaert" : "spiller";
                    string self = p.userId == myId ? " · dig" : "";
                    _playersSb.Append("• ").Append(p.name).Append(" (").Append(tag).Append(self).AppendLine(")");
                }
            }
            _playersLabel.text = _playersSb.ToString();
        }
    }
}
