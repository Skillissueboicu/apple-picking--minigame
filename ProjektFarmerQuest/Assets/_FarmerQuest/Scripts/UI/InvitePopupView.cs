using System;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerQuest.UI
{
    /// <summary>Invite-popup der ligger i scenen (MainMenu/Profile). Ingen spawn.</summary>
    public sealed class InvitePopupView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _bodyLabel;
        [SerializeField] private Button _btnAccept;
        [SerializeField] private Button _btnDecline;
        [SerializeField] private Button _btnLater;

        private void Awake()
        {
            Hide();
        }

        public void Show(
            string title, string body,
            Action onAccept, Action onDecline, Action onLater)
        {
            if (_root == null) _root = gameObject;
            if (_titleLabel != null) _titleLabel.text = title ?? "";
            if (_bodyLabel != null) _bodyLabel.text = body ?? "";

            Wire(_btnAccept, onAccept);
            Wire(_btnDecline, onDecline);
            Wire(_btnLater, onLater ?? Hide);

            _root.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
            else gameObject.SetActive(false);
        }

        public bool IsVisible =>
            _root != null ? _root.activeSelf : gameObject.activeSelf;

        private static void Wire(Button btn, Action action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            if (action != null)
                btn.onClick.AddListener(() => action());
        }
    }
}
