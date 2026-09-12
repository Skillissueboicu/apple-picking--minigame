using System.Threading.Tasks;
using FarmerQuest.Core;
using FarmerQuest.Net;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerQuest.UI
{
    /// <summary>
    /// Lobby. UI refreshes dynamically
    /// </summary>
    public sealed class LobbyController : MonoBehaviour
    {
        [Header("Join panel")]
        [SerializeField] private GameObject _joinPanel;
        [SerializeField] private InputField _joinCodeField;
        [SerializeField] private Button _btnJoinSubmit;
        [SerializeField] private Button _btnJoinBack;
        [SerializeField] private Text _joinStatusLabel;

        [Header("Lobby panel")]
        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private Text _codeLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private RectTransform _playersBox;
        [SerializeField] private Text _playersHeader;
        [SerializeField] private Text[] _playerNameLabels;
        [SerializeField] private GameObject _hostPanel;
        [SerializeField] private InputField _inviteField;
        [SerializeField] private Button _btnInvite;
        [SerializeField] private Button _btnStart;
        [SerializeField] private GameObject _guestWait;
        [SerializeField] private Button _btnLeave;

        private INetworkSession _session;
        private bool _alive = true;
        private bool _navigated;

        private SessionInfo Info => _session?.Current;
        private string MyId => App.Auth?.User?.id;
        private bool IsHost => Info != null && Info.hostUserId == MyId;

        private void Start()
        {
            App.Init();
            if (_lobbyPanel == null || _joinPanel == null)
            {
                Debug.LogError("[Lobby] UI missing in scene. Run FarmQuest Online/Configure Project.");
                return;
            }

            Wire();

            // Show field for entering a code when a player tries to join 
            bool needCode = GameContext.Intent == LobbyIntent.Join &&
                            string.IsNullOrEmpty(GameContext.PendingJoinCode);
            ShowJoin(needCode);
            if (!needCode) _ = InitAsync();
        }

        private void OnDestroy()
        {
            _alive = false;
            if (_session == null) return;
            _session.OnSessionUpdated -= OnUpdated;
            _session.OnGameStarted -= OnStarted;
            _session.OnError -= OnErr;
        }

        private void Wire()
        {
            Bind(_btnJoinSubmit, () =>
            {
                if (_joinCodeField == null) return;
                GameContext.PendingJoinCode = _joinCodeField.text.Trim().ToUpperInvariant();
                ShowJoin(false);
                _ = InitAsync();
            });
            Bind(_btnJoinBack, () => SceneFlow.GoMainMenu());
            Bind(_btnInvite, OnInvite);
            Bind(_btnStart, OnStart);
            Bind(_btnLeave, OnLeave);
        }

        private static void Bind(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        private void ShowJoin(bool join)
        {
            _joinPanel.SetActive(join);
            _lobbyPanel.SetActive(!join);
            if (!join && _statusLabel != null)
                _statusLabel.text = GameContext.Intent == LobbyIntent.Create ? "Opretter..." : "Deltager...";
        }

        // Connects, then creates or joins a session based on GameContext.Intent
        private async Task InitAsync()
        {
            try
            {
                if (App.Auth == null || !App.Auth.IsAuthenticated)
                {
                    SetStatus("Du skal være logget ind.");
                    return;
                }

                _session = new PollingNetworkSession(App.Sessions);
                await _session.ConnectAsync(App.Auth.Token);
                _session.OnSessionUpdated += OnUpdated;
                _session.OnGameStarted += OnStarted;
                _session.OnError += OnErr;

                if (GameContext.Intent == LobbyIntent.Create)
                    await _session.CreateAsync();
                else
                    await _session.JoinByCodeAsync(GameContext.PendingJoinCode);

                if (!_alive) return;
                GameContext.Session = _session;
                GameContext.CurrentSession = Info;
                ApplyLobby();
            }
            catch (ApiException ex)
            {
                SetStatus($"Fejl ({ex.StatusCode}): {ex.Message}");
            }
            catch (System.Exception ex)
            {
                SetStatus(ex.Message);
            }
        }

        private void ApplyLobby()
        {
            if (_codeLabel != null) _codeLabel.text = Info?.code ?? "------";
            if (_hostPanel != null) _hostPanel.SetActive(IsHost);
            if (_guestWait != null) _guestWait.SetActive(!IsHost);
            RefreshPlayers();
            SetStatus("");
        }

        /// <summary>Updates pre-placed player list labels (no runtime spawn/destroy).</summary>
        private void RefreshPlayers()
        {
            EnsurePlayerSlotsFromBox();
            if (Info?.players == null) return;

            if (_playersHeader != null)
            {
                _playersHeader.gameObject.SetActive(true);
                _playersHeader.text = $"Spillere online ({Info.players.Count})";
            }

            if (_playerNameLabels == null) return;

            int shown = Mathf.Min(Info.players.Count, _playerNameLabels.Length);
            for (int i = 0; i < _playerNameLabels.Length; i++)
            {
                Text slot = _playerNameLabels[i];
                if (slot == null) continue;

                if (i < shown)
                {
                    SessionPlayer p = Info.players[i];
                    bool host = p.isHost || p.userId == Info.hostUserId;
                    string tag = host ? " (host)" : "";
                    string self = p.userId == MyId ? " (dig)" : "";
                    slot.text = p.name + tag + self;
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    slot.text = "";
                    slot.gameObject.SetActive(false);
                }
            }
        }

        // Resolves header/slots from serialized fields or existing children under PlayersBox
        private void EnsurePlayerSlotsFromBox()
        {
            if (_playersHeader != null && _playerNameLabels != null && _playerNameLabels.Length > 0)
                return;
            if (_playersBox == null) return;

            var texts = _playersBox.GetComponentsInChildren<Text>(true);
            if (texts == null || texts.Length == 0) return;

            if (_playersHeader == null)
                _playersHeader = texts[0];

            if (_playerNameLabels == null || _playerNameLabels.Length == 0)
            {
                int count = texts.Length - 1;
                if (count <= 0) return;
                _playerNameLabels = new Text[count];
                for (int i = 0; i < count; i++)
                    _playerNameLabels[i] = texts[i + 1];
            }
        }

        private void OnUpdated(SessionInfo info)
        {
            if (!_alive) return;
            GameContext.CurrentSession = info;
            if (_codeLabel != null) _codeLabel.text = info.code;
            if (_hostPanel != null) _hostPanel.SetActive(IsHost);
            if (_guestWait != null) _guestWait.SetActive(!IsHost);
            RefreshPlayers();
        }

        private void OnStarted(string _) => GoFarm();
        private void OnErr(string msg) => SetStatus(msg);

        private async void OnInvite()
        {
            if (_session == null || _inviteField == null || string.IsNullOrWhiteSpace(_inviteField.text))
                return;
            try
            {
                await _session.InviteByUsernameAsync(_inviteField.text.Trim());
                _inviteField.text = "";
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }

        private async void OnStart()
        {
            if (_session == null) return;
            try
            {
                await _session.StartAsync();
                if (_alive) GoFarm();
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }

        private async void OnLeave()
        {
            _alive = false;
            try { await _session?.LeaveAsync(); } catch { /* ignore */ }
            try { await _session?.DisconnectAsync(); } catch { /* ignore */ }
            GameContext.Session = null;
            GameContext.CurrentSession = null;
            SceneFlow.GoMainMenu();
        }

        private void GoFarm()
        {
            if (_navigated) return;
            _navigated = true;
            SceneFlow.GoFarmerQuestScene();
        }

        private void SetStatus(string t)
        {
            if (_lobbyPanel != null && _lobbyPanel.activeSelf && _statusLabel != null)
                _statusLabel.text = t;
            else if (_joinStatusLabel != null)
                _joinStatusLabel.text = t;
            if (!string.IsNullOrEmpty(t)) Debug.Log("[Lobby] " + t);
        }
    }
}
