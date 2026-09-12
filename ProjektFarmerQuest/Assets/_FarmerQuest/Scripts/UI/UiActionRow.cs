using System;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerQuest.UI
{
    /// <summary>
    /// Fast række i editoren (label + op til 2 knapper). Ingen runtime-spawn —
    /// vis/skjul og sæt tekst/callbacks i Play.
    /// </summary>
    public sealed class UiActionRow : MonoBehaviour
    {
        [SerializeField] private Text _label;
        [SerializeField] private Button _primaryButton;
        [SerializeField] private Text _primaryLabel;
        [SerializeField] private Button _secondaryButton;
        [SerializeField] private Text _secondaryLabel;

        public void Hide() => gameObject.SetActive(false);

        public void ShowLabelOnly(string text)
        {
            gameObject.SetActive(true);
            if (_label != null) _label.text = text ?? "";
            SetButton(_primaryButton, _primaryLabel, null, null);
            SetButton(_secondaryButton, _secondaryLabel, null, null);
        }

        public void Show(
            string text,
            string primaryText, Action onPrimary,
            string secondaryText = null, Action onSecondary = null)
        {
            gameObject.SetActive(true);
            if (_label != null) _label.text = text ?? "";
            SetButton(_primaryButton, _primaryLabel, primaryText, onPrimary);
            SetButton(_secondaryButton, _secondaryLabel, secondaryText, onSecondary);
        }

        private static void SetButton(Button btn, Text label, string text, Action onClick)
        {
            if (btn == null) return;
            bool on = !string.IsNullOrEmpty(text) && onClick != null;
            btn.gameObject.SetActive(on);
            if (!on) return;
            if (label != null) label.text = text;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick());
        }
    }
}
