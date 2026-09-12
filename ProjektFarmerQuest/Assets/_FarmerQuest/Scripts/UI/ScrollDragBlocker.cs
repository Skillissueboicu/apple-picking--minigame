using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmerQuest.UI
{
    /// <summary>
    /// Forhindrer at parent-ScrollRect stjæler drag fra en Slider.
    /// Genaktiverer altid scroll (OnDisable / pointer up).
    /// </summary>
    public sealed class ScrollDragBlocker : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
    {
        private ScrollRect _scroll;
        private bool _holding;

        private void Awake() => _scroll = GetComponentInParent<ScrollRect>();

        private void OnDisable() => Release();

        public void OnPointerDown(PointerEventData eventData) => Hold();

        public void OnBeginDrag(PointerEventData eventData) => Hold();

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData) => Release();

        public void OnPointerUp(PointerEventData eventData) => Release();

        private void Hold()
        {
            if (_scroll == null || _holding) return;
            _holding = true;
            _scroll.StopMovement();
            _scroll.enabled = false;
        }

        private void Release()
        {
            if (!_holding) return;
            _holding = false;
            if (_scroll != null) _scroll.enabled = true;
        }
    }
}
