using UnityEngine;
using UnityEngine.UI;

namespace FarmerQuest.Progress
{
    /// <summary>Badge-info fane — ligger i scenen/prefab, redigeres i editoren.</summary>
    public sealed class BadgeDetailView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _title;
        [SerializeField] private Text _body;
        [SerializeField] private Image _tierIcon;
        [SerializeField] private Button _btnClose;
        [SerializeField] private Button _btnDimBackground;

        private bool _wired;

        private void WireButtons()
        {
            if (_wired) return;
            _wired = true;
            if (_btnClose != null)
            {
                _btnClose.onClick.RemoveAllListeners();
                _btnClose.onClick.AddListener(Hide);
            }
            if (_btnDimBackground != null)
            {
                _btnDimBackground.onClick.RemoveAllListeners();
                _btnDimBackground.onClick.AddListener(Hide);
            }
        }

        public void Show(string slotKey, int points)
        {
            if (_root == null) _root = gameObject;
            WireButtons();
            string tier = BadgeData.TierFromPoints(points);
            if (_title != null)
                _title.text = BadgeCatalog.Title(slotKey);
            if (_body != null)
                _body.text = BadgeCatalog.BuildDetailBody(slotKey, points);
            if (_tierIcon != null)
                _tierIcon.color = BadgeData.TierColor(tier);
            _root.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
            else gameObject.SetActive(false);
        }
    }
}
