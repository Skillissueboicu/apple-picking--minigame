using System.Collections.Generic;
using System.Threading.Tasks;
using FarmerQuest.Core;
using FarmerQuest.Net;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerQuest.UI
{
    /// <summary>
    /// Hovedmenu: fortsæt / nyt / load save, join via kode, profil, log ud.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Text _greetingLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private Button _btnContinue;
        [SerializeField] private Button _btnNewGame;
        [SerializeField] private Button _btnLoad;
        [SerializeField] private Button _btnJoinCode;
        [SerializeField] private Button _btnProfile;
        [SerializeField] private Button _btnLogout;

        [Header("Load panel")]
        [SerializeField] private GameObject _loadPanel;
        [SerializeField] private RectTransform _loadList;
        [SerializeField] private Button _btnLoadClose;

        // Bagudkompatibilitet hvis gammel scene kun har Farmer Quest-knap.
        [SerializeField] private Button _btnFarmerQuest;

        private void Start()
        {
            App.Init();
            if (App.Auth == null || !App.Auth.IsAuthenticated)
            {
                SceneFlow.GoLogin();
                return;
            }

            if (_btnContinue == null && _btnFarmerQuest == null && _btnNewGame == null)
            {
                Debug.LogError("[MainMenu] UI mangler i scenen. Koer FarmQuest Online/Configure Project.");
                return;
            }

            if (_greetingLabel != null)
                _greetingLabel.text = $"Hej, {App.Auth.User?.name ?? "Spiller"}";

            //InviteNotificationService.Instance?.StartMonitoring();

            //Bind(_btnContinue, () => _ = ContinueAsync());
            Bind(_btnNewGame, StartNewGame);
            Bind(_btnLoad, () => _ = OpenLoadAsync());
            Bind(_btnLoadClose, CloseLoad);
            Bind(_btnJoinCode, () =>
            {
                GameContext.SelectedGameKind = "farm";
                GameContext.Intent = LobbyIntent.Join;
                GameContext.PendingJoinCode = null;
                //GameContext.PendingFarmSaveId = null;
                SceneFlow.GoLobby();
            });
            Bind(_btnFarmerQuest, StartNewGame);
            Bind(_btnProfile, SceneFlow.GoProfile);
            Bind(_btnLogout, () =>
            {
                //InviteNotificationService.Instance?.StopMonitoring();
                App.Auth.Clear();
                GameContext.Session = null;
                GameContext.CurrentSession = null;
                //GameContext.PendingFarmSaveId = null;
                SceneFlow.GoLogin();
            });

            if (_loadPanel != null)
                _loadPanel.SetActive(false);

            //_ = RefreshContinueAsync();
        }

        /*private async Task RefreshContinueAsync()
        {
            if (_btnContinue == null) return;
            try
            {
                FarmSaveInfo latest = await App.FarmSaves.GetLatestAsync();
                _btnContinue.interactable = latest != null && !string.IsNullOrEmpty(latest.saveId);
                SetStatus(latest != null
                    ? $"Seneste: {latest.displayName} · {latest.money} kr"
                    : "Ingen gemte gårde endnu");
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                _btnContinue.interactable = false;
                SetStatus("Ingen gemte gårde endnu");
            }
            catch (System.Exception ex)
            {
                _btnContinue.interactable = false;
                SetStatus(ex.Message);
            }
        }*/

        /*private async Task ContinueAsync()
        {
            SetStatus("Henter seneste save...");
            try
            {
                FarmSaveInfo latest = await App.FarmSaves.GetLatestAsync();
                if (latest == null || string.IsNullOrEmpty(latest.saveId))
                {
                    SetStatus("Ingen gemte gårde.");
                    return;
                }

                await LaunchSaveAsync(latest.saveId);
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                SetStatus("Ingen gemte gårde.");
            }
            catch (System.Exception ex)
            {
                SetStatus(ex.Message);
            }
        }*/

        private void StartNewGame()
        {
            // Kun nyt spil går via lobby (invite/start).
            GameContext.SelectedGameKind = "farm";
            GameContext.Intent = LobbyIntent.Create;
            GameContext.PendingJoinCode = null;
            //GameContext.PendingFarmSaveId = null;
            SceneFlow.GoLobby();
        }

        /// <summary>Fortsæt/Load: spring lobby over og gå direkte ind i gården.</summary>
        private async Task LaunchSaveAsync(string saveId)
        {
            CloseLoad();
            SetStatus("Åbner gård...");
            try
            {
                var net = new PollingNetworkSession(App.Sessions);
                await net.ConnectAsync(App.Auth.Token);

                GameContext.SelectedGameKind = "farm";
                GameContext.Intent = LobbyIntent.Create;
                GameContext.PendingJoinCode = null;
                //GameContext.PendingFarmSaveId = saveId;

                await net.CreateAsync();
                SessionInfo info = net.Current;
                if (info == null)
                {
                    SetStatus("Kunne ikke åbne save.");
                    return;
                }

                if (info.status != "playing" && info.status != "running")
                    await net.StartAsync();

                GameContext.Session = net;
                GameContext.CurrentSession = net.Current;
                //GameContext.PendingFarmSaveId = null;
                //SceneFlow.GoOnlineFarm();
            }
            catch (System.Exception ex)
            {
                SetStatus(ex.Message);
            }
        }

        private async Task OpenLoadAsync()
        {
            if (_loadPanel == null || _loadList == null)
            {
                SetStatus("Load-panel mangler. Koer Configure Project.");
                return;
            }

            _loadPanel.SetActive(true);
            SetStatus("Henter saves...");

            try
            {
                //List<FarmSaveInfo> saves = await App.FarmSaves.ListAsync() ?? new List<FarmSaveInfo>();
                //BindLoadRows(saves);
            }
            catch (System.Exception ex)
            {
                SetStatus(ex.Message);
            }
        }

        /*private void BindLoadRows(List<FarmSaveInfo> saves)
        {
            UiActionRow[] rows = _loadList != null
                ? _loadList.GetComponentsInChildren<UiActionRow>(true)
                : System.Array.Empty<UiActionRow>();
            if (rows.Length == 0)
            {
                SetStatus("Load-rækker mangler. Kør FarmQuest Online/Configure Project.");
                return;
            }

            foreach (UiActionRow row in rows)
                if (row != null) row.Hide();

            if (saves == null || saves.Count == 0)
            {
                rows[0].ShowLabelOnly("Ingen gemte gårde.");
                SetStatus("");
                return;
            }

            int n = Mathf.Min(saves.Count, rows.Length);
            for (int i = 0; i < n; i++)
            {
                FarmSaveInfo save = saves[i];
                string id = save.saveId;
                string title = $"{save.displayName}  ·  {save.money} kr  ·  CO₂ {save.co2:0}  ·  Gæld {save.debt}";
                rows[i].Show(
                    title,
                    "Åbn", () => _ = LaunchSaveAsync(id),
                    "Slet", () => _ = DeleteSaveAsync(id));
            }

            SetStatus(saves.Count > rows.Length
                ? $"{saves.Count} saves (viser {rows.Length})"
                : "");
        }*/

        /*private async Task DeleteSaveAsync(string saveId)
        {
            try
            {
                await App.FarmSaves.DeleteAsync(saveId);
                await OpenLoadAsync();
                await RefreshContinueAsync();
            }
            catch (System.Exception ex)
            {
                SetStatus(ex.Message);
            }
        }*/

        private void CloseLoad()
        {
            if (_loadPanel != null)
                _loadPanel.SetActive(false);
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text ?? "";
        }

        private static void Bind(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }
    }
}
