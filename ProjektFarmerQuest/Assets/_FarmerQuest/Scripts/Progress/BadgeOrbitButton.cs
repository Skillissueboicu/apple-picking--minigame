using System;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerQuest.Progress
{
    /// <summary>Badge på hjulet — ligger i scenen (redigérbar). Opdateres ved Play.</summary>
    public sealed class BadgeOrbitButton : MonoBehaviour
    {
        [SerializeField] private string _slotKey;
        [SerializeField] private Button _button;
        [SerializeField] private Image _fill;
        [SerializeField] private Outline _outline;
        [SerializeField] private Text _label;

        public string SlotKey => _slotKey;

        public void Setup(string slotKey, string tier, Color theme, string label, bool locked, Action onClick)
        {
            _slotKey = slotKey;
            if (_fill != null)
            {
                _fill.color = locked
                    ? new Color(0.35f, 0.35f, 0.38f, 0.85f)
                    : BadgeData.TierColor(tier);
                _fill.raycastTarget = true;
            }
            if (_outline != null)
                _outline.effectColor = theme;
            if (_label != null)
            {
                _label.text = label ?? "";
                _label.color = locked ? new Color(0.7f, 0.7f, 0.72f, 1f) : Color.white;
            }
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                if (onClick != null)
                    _button.onClick.AddListener(() => onClick());
            }
        }

        public void Place(Vector2 anchoredPos, float size)
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = anchoredPos;
            if (_label != null)
                _label.fontSize = Mathf.Clamp(Mathf.RoundToInt(size * 0.26f), 12, 32);
        }
    }
}
