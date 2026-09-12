using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FarmerQuest.Progress
{
    /// <summary>Commit DEBUG-slider når pointer slippes (egen fil — undgår nested MonoBehaviour / missing script).</summary>
    public sealed class DebugSliderCommitRelay : MonoBehaviour, IPointerUpHandler, IEndDragHandler
    {
        public Action OnCommit;
        public void OnPointerUp(PointerEventData eventData) => OnCommit?.Invoke();
        public void OnEndDrag(PointerEventData eventData) => OnCommit?.Invoke();
    }
}
