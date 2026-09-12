using System;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerQuest.Progress
{
    /// <summary>DEBUG badge-række — ligger i scenen (redigérbar). Opdateres ved Play.</summary>
    public sealed class BadgeSlotRowView : MonoBehaviour
    {
        [SerializeField] private string _slotKey;
        [SerializeField] private Image _background;
        [SerializeField] private Button _titleButton;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Image _bronzeMedal;
        [SerializeField] private Image _silverMedal;
        [SerializeField] private Image _goldMedal;
        [SerializeField] private Slider _slider;

        public string SlotKey => _slotKey;
        public Text TitleLabel => _titleLabel;
        public Slider Slider => _slider;
        public Image BronzeMedal => _bronzeMedal;
        public Image SilverMedal => _silverMedal;
        public Image GoldMedal => _goldMedal;

        public void Setup(
            string slotKey,
            string titleText,
            Color? theme,
            bool locked,
            int points,
            Action onTitleClick,
            Action<int> onLiveValue,
            Action onCommit)
        {
            _slotKey = slotKey;
            if (_background != null)
            {
                if (locked)
                    _background.color = new Color(0.22f, 0.22f, 0.24f, 0.95f);
                else if (theme.HasValue)
                {
                    Color t = theme.Value;
                    _background.color = new Color(t.r * 0.4f, t.g * 0.4f, t.b * 0.4f, 0.95f);
                }
                else
                    _background.color = new Color(0.28f, 0.18f, 0.04f, 0.95f);
            }

            if (_titleLabel != null)
            {
                _titleLabel.color = locked
                    ? new Color(0.65f, 0.65f, 0.68f, 1f)
                    : new Color(1f, 0.92f, 0.45f, 1f);
                _titleLabel.text = titleText;
            }

            if (_titleButton != null)
            {
                _titleButton.onClick.RemoveAllListeners();
                if (onTitleClick != null)
                    _titleButton.onClick.AddListener(() => onTitleClick());
            }

            ApplyTierMedals(points, locked);

            if (_slider != null)
            {
                _slider.minValue = 0;
                _slider.maxValue = PlayerProgressData.MaxSlotPoints;
                _slider.wholeNumbers = true;
                _slider.SetValueWithoutNotify(points);
                _slider.interactable = !locked;
                _slider.onValueChanged.RemoveAllListeners();
                if (onLiveValue != null)
                    _slider.onValueChanged.AddListener(v => onLiveValue(Mathf.RoundToInt(v)));

                var commit = _slider.GetComponent<DebugSliderCommitRelay>();
                if (commit == null)
                    commit = _slider.gameObject.AddComponent<DebugSliderCommitRelay>();
                commit.OnCommit = onCommit;
            }
        }

        public void ApplyTierMedals(int points, bool locked)
        {
            Color dim = new(0.28f, 0.28f, 0.3f, 0.7f);
            if (locked)
            {
                if (_bronzeMedal != null) _bronzeMedal.color = dim;
                if (_silverMedal != null) _silverMedal.color = dim;
                if (_goldMedal != null) _goldMedal.color = dim;
                return;
            }
            if (_bronzeMedal != null)
                _bronzeMedal.color = points >= 100 ? BadgeData.TierColor("Bronze") : dim;
            if (_silverMedal != null)
                _silverMedal.color = points >= 200 ? BadgeData.TierColor("Silver") : dim;
            if (_goldMedal != null)
                _goldMedal.color = points >= 300 ? BadgeData.TierColor("Gold") : dim;
        }
    }
}
