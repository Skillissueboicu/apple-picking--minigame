using System.Collections.Generic;
using System.Threading.Tasks;
using FarmerQuest.Core;
using FarmerQuest.Net;
using FarmerQuest.Progress;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerQuest.UI
{
    /// <summary>
    /// Profil med faner: Progress (hjul), Invitationer, Venner, Blokerede.
    /// Lister bruger UiActionRow der allerede ligger i scenen — ingen spawn i Play.
    /// </summary>
    public sealed class ProfileController : MonoBehaviour
    {
        [SerializeField] private Text _greetingLabel;
        [SerializeField] private Text _emailLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private Button _btnBack;

        [SerializeField] private Button _tabProgress;
        [SerializeField] private Button _tabInvites;
        [SerializeField] private Button _tabFriends;
        [SerializeField] private Button _tabBlocks;

        [SerializeField] private GameObject _panelProgress;
        [SerializeField] private GameObject _panelInvites;
        [SerializeField] private GameObject _panelFriends;
        [SerializeField] private GameObject _panelBlocks;

        [SerializeField] private PillarWheelPanel _wheelPanel;
        [SerializeField] private RectTransform _inviteList;
        [SerializeField] private RectTransform _friendsList;
        [SerializeField] private RectTransform _incomingList;
        [SerializeField] private RectTransform _outgoingList;
        [SerializeField] private RectTransform _blocksList;

        [SerializeField] private InputField _friendUsernameField;
        [SerializeField] private Button _btnSendFriendRequest;
        [SerializeField] private InputField _blockUsernameField;
        [SerializeField] private Button _btnBlockUser;

        private bool _alive = true;

        private void Start()
        {
            App.Init();
            if (App.Auth == null || !App.Auth.IsAuthenticated)
            {
                SceneFlow.GoLogin();
                return;
            }

            if (_btnBack == null)
            {
                Debug.LogError("[Profile] UI mangler. Kør FarmQuest Online/Configure Project.");
                return;
            }

            if (_greetingLabel != null)
                _greetingLabel.text = App.Auth.User?.name ?? "Spiller";
            if (_emailLabel != null)
            {
                string email = App.Auth.User?.email ?? "";
                _emailLabel.text = email;
                _emailLabel.gameObject.SetActive(!string.IsNullOrEmpty(email));
            }

            _btnBack.onClick.RemoveAllListeners();
            _btnBack.onClick.AddListener(SceneFlow.GoMainMenu);

            BindTab(_tabProgress, "progress");
            BindTab(_tabInvites, "invites");
            BindTab(_tabFriends, "friends");
            BindTab(_tabBlocks, "blocks");

            if (_btnSendFriendRequest != null)
            {
                _btnSendFriendRequest.onClick.RemoveAllListeners();
                //_btnSendFriendRequest.onClick.AddListener(() => _ = SendFriendRequestAsync());
            }
            if (_btnBlockUser != null)
            {
                _btnBlockUser.onClick.RemoveAllListeners();
                //_btnBlockUser.onClick.AddListener(() => _ = BlockUserAsync());
            }

            //InviteNotificationService.Instance?.StartMonitoring();
            ShowTab("progress");
        }

        private void OnDestroy() => _alive = false;

        private void BindTab(Button btn, string tab)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => ShowTab(tab));
        }

        private void ShowTab(string tab)
        {
            SetActive(_panelProgress, tab == "progress");
            SetActive(_panelInvites, tab == "invites");
            SetActive(_panelFriends, tab == "friends");
            SetActive(_panelBlocks, tab == "blocks");

            if (tab == "progress" && _wheelPanel != null)
                _wheelPanel.RefreshFromServer();
            /*else if (tab == "invites")
                _ = RefreshInvitesAsync();
            else if (tab == "friends")
                _ = RefreshFriendsAsync();
            else if (tab == "blocks")
                _ = RefreshBlocksAsync();*/
        }

        private static void SetActive(GameObject go, bool on)
        {
            if (go != null) go.SetActive(on);
        }

        private void SetStatus(string msg)
        {
            if (_statusLabel != null) _statusLabel.text = msg ?? "";
        }

        private static UiActionRow[] RowsOf(RectTransform list)
        {
            if (list == null) return System.Array.Empty<UiActionRow>();
            return list.GetComponentsInChildren<UiActionRow>(true);
        }

        private static void HideAll(UiActionRow[] rows)
        {
            if (rows == null) return;
            foreach (UiActionRow row in rows)
                if (row != null) row.Hide();
        }

        private static bool BindOrWarn(RectTransform list, out UiActionRow[] rows)
        {
            rows = RowsOf(list);
            if (rows.Length == 0)
            {
                Debug.LogWarning(
                    "[Profile] Ingen UiActionRow i liste. Kør FarmQuest Online/Configure Project.");
                return false;
            }
            HideAll(rows);
            return true;
        }

        // ——— Invitationer ———

        /*private async Task RefreshInvitesAsync()
        {
            if (!_alive) return;
            try
            {
                List<SessionInvitationInfo> invites = await App.Sessions.GetPendingInvitationsAsync();
                if (!_alive) return;
                RenderInvites(invites ?? new List<SessionInvitationInfo>());
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }*/

        /*private void RenderInvites(List<SessionInvitationInfo> invites)
        {
            if (!BindOrWarn(_inviteList, out UiActionRow[] rows)) return;

            if (invites.Count == 0)
            {
                rows[0].ShowLabelOnly("Ingen ventende invitationer.");
                SetStatus("Ingen ventende invitationer.");
                return;
            }

            SetStatus($"{invites.Count} ventende invitation(er)");
            int n = Mathf.Min(invites.Count, rows.Length);
            for (int i = 0; i < n; i++)
            {
                SessionInvitationInfo invite = invites[i];
                string game = invite.gameKind == "placement" ? "2D Minispil" : "Farmer Quest";
                SessionInvitationInfo capt = invite;
                rows[i].Show(
                    $"{invite.hostName} — {game} ({invite.code})",
                    "Acceptér", () => _ = AcceptInviteAsync(capt),
                    "Afvis", () => _ = DeclineInviteAsync(capt));
            }
            if (invites.Count > rows.Length)
                SetStatus($"{invites.Count} invitationer (viser {rows.Length})");
        }*/

        /*private async Task AcceptInviteAsync(SessionInvitationInfo invite)
        {
            try
            {
                SessionInfo session = await App.Sessions.AcceptInvitationAsync(invite.invitationId);
                GameContext.SelectedGameKind = string.IsNullOrEmpty(invite.gameKind) ? "farm" : invite.gameKind;
                GameContext.Intent = LobbyIntent.Join;
                GameContext.PendingJoinCode = invite.code;
                GameContext.CurrentSession = session;
                InviteNotificationService.Instance?.StopMonitoring();
                SceneFlow.GoLobby();
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }

        private async Task DeclineInviteAsync(SessionInvitationInfo invite)
        {
            try
            {
                await App.Sessions.DeclineInvitationAsync(invite.invitationId);
                await RefreshInvitesAsync();
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }*/

        // ——— Venner ———

        /*private async Task RefreshFriendsAsync()
        {
            if (!_alive || App.Social == null) return;
            try
            {
                List<FriendDto> friends = await App.Social.GetFriendsAsync() ?? new();
                List<FriendRequestDto> incoming = await App.Social.GetIncomingRequestsAsync() ?? new();
                List<FriendRequestDto> outgoing = await App.Social.GetOutgoingRequestsAsync() ?? new();
                if (!_alive) return;

                RenderFriends(friends);
                RenderIncoming(incoming);
                RenderOutgoing(outgoing);
                SetStatus($"Venner: {friends.Count} · Ind: {incoming.Count} · Ud: {outgoing.Count}");
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }*/

        /*private void RenderFriends(List<FriendDto> friends)
        {
            if (!BindOrWarn(_friendsList, out UiActionRow[] rows)) return;
            if (friends.Count == 0)
            {
                rows[0].ShowLabelOnly("Ingen venner endnu.");
                return;
            }
            int n = Mathf.Min(friends.Count, rows.Length);
            for (int i = 0; i < n; i++)
            {
                FriendDto f = friends[i];
                string id = f.userId;
                rows[i].Show(f.name ?? f.userId, "Fjern", () => _ = RemoveFriendAsync(id));
            }
        }*/

        /*private void RenderIncoming(List<FriendRequestDto> incoming)
        {
            if (!BindOrWarn(_incomingList, out UiActionRow[] rows)) return;
            if (incoming.Count == 0)
            {
                rows[0].ShowLabelOnly("Ingen indkommende.");
                return;
            }
            int n = Mathf.Min(incoming.Count, rows.Length);
            for (int i = 0; i < n; i++)
            {
                FriendRequestDto r = incoming[i];
                long id = r.id;
                rows[i].Show(
                    r.fromName ?? r.fromUserId,
                    "Acceptér", () => _ = AcceptFriendAsync(id),
                    "Afvis", () => _ = DeclineFriendAsync(id));
            }
        }*/

        /*private void RenderOutgoing(List<FriendRequestDto> outgoing)
        {
            if (!BindOrWarn(_outgoingList, out UiActionRow[] rows)) return;
            if (outgoing.Count == 0)
            {
                rows[0].ShowLabelOnly("Ingen udgående.");
                return;
            }
            int n = Mathf.Min(outgoing.Count, rows.Length);
            for (int i = 0; i < n; i++)
                rows[i].ShowLabelOnly($"→ {outgoing[i].toName ?? outgoing[i].toUserId}");
        }*/

        /*private async Task SendFriendRequestAsync()
        {
            string name = _friendUsernameField != null ? _friendUsernameField.text?.Trim() : null;
            if (string.IsNullOrEmpty(name))
            {
                SetStatus("Skriv et brugernavn.");
                return;
            }
            try
            {
                await App.Social.SendFriendRequestAsync(name);
                if (_friendUsernameField != null) _friendUsernameField.text = "";
                SetStatus($"Venneanmodning sendt til {name}.");
                await RefreshFriendsAsync();
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }*/

        /*private async Task AcceptFriendAsync(long id)
        {
            try
            {
                await App.Social.AcceptFriendRequestAsync(id);
                await RefreshFriendsAsync();
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }

        private async Task DeclineFriendAsync(long id)
        {
            try
            {
                await App.Social.DeclineFriendRequestAsync(id);
                await RefreshFriendsAsync();
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }

        private async Task RemoveFriendAsync(string userId)
        {
            try
            {
                await App.Social.RemoveFriendAsync(userId);
                await RefreshFriendsAsync();
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }*/

        // ——— Blokeringer ———

        /*private async Task RefreshBlocksAsync()
        {
            if (!_alive || App.Social == null) return;
            try
            {
                List<BlockDto> blocks = await App.Social.GetBlocksAsync() ?? new();
                if (!_alive) return;
                if (!BindOrWarn(_blocksList, out UiActionRow[] rows)) return;

                if (blocks.Count == 0)
                {
                    rows[0].ShowLabelOnly("Ingen blokerede brugere.");
                    SetStatus("Ingen blokeringer.");
                    return;
                }

                int n = Mathf.Min(blocks.Count, rows.Length);
                for (int i = 0; i < n; i++)
                {
                    BlockDto b = blocks[i];
                    string id = b.userId;
                    rows[i].Show(b.name ?? b.userId, "Fjern blokering", () => _ = UnblockAsync(id));
                }
                SetStatus($"{blocks.Count} blokeret");
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }

        private async Task BlockUserAsync()
        {
            string name = _blockUsernameField != null ? _blockUsernameField.text?.Trim() : null;
            if (string.IsNullOrEmpty(name))
            {
                SetStatus("Skriv et brugernavn.");
                return;
            }
            try
            {
                await App.Social.BlockUserAsync(name);
                if (_blockUsernameField != null) _blockUsernameField.text = "";
                SetStatus($"{name} er blokeret.");
                await RefreshBlocksAsync();
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }

        private async Task UnblockAsync(string userId)
        {
            try
            {
                await App.Social.UnblockUserAsync(userId);
                await RefreshBlocksAsync();
            }
            catch (ApiException ex)
            {
                SetStatus(ex.Message);
            }
        }*/
    }
}
