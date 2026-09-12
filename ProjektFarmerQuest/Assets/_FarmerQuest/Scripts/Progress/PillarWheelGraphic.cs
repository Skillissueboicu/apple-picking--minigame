using UnityEngine;
using UnityEngine.UI;

namespace FarmerQuest.Progress
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PillarWheelGraphic : MaskableGraphic
    {
        [SerializeField] private PlayerProgressData _data = new();
        [SerializeField] private float _centerR = 0.16f;
        [SerializeField] private float _innerInnerR = 0.22f;
        [SerializeField] private float _innerOuterR = 0.52f;
        [SerializeField] private float _outerInnerR = 0.56f;
        [SerializeField] private float _outerOuterR = 0.88f;
        [SerializeField] private float _gapDegrees = 2f;

        public float GapDegrees => _gapDegrees;
        public float InnerOrbitFactor => (_innerInnerR + _innerOuterR) * 0.5f;
        public float OuterOrbitFactor => (_outerInnerR + _outerOuterR) * 0.5f;

        public void SetData(PlayerProgressData data)
        {
            _data = data ?? new PlayerProgressData();
            SetVerticesDirty();
        }

        /// <summary>Radius i lokale UI-pixels (halv hjul-side).</summary>
        public float WheelRadius
        {
            get
            {
                Rect r = rectTransform.rect;
                float side = Mathf.Min(r.width, r.height);
                if (side < 10f) side = rectTransform.sizeDelta.x;
                return Mathf.Max(10f, side) * 0.5f;
            }
        }

        /// <summary>
        /// Midtpunkt af et felt i Wheel-lokalt rum (0° = op, Sin/Cos — samme som mesh).
        /// outerIndex &lt; 0 = indre kategori-felt.
        /// </summary>
        public Vector2 FieldCenterLocal(PillarRingDef ring, int outerIndex = -1)
        {
            float radius = WheelRadius;
            float orbit = radius * (outerIndex < 0 ? InnerOrbitFactor : OuterOrbitFactor);
            float midDeg = FieldMidDegrees(ring, outerIndex, _gapDegrees);
            float rad = midDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(rad) * orbit, Mathf.Cos(rad) * orbit);
        }

        public static float FieldMidDegrees(PillarRingDef ring, int outerIndex, float gapDegrees = 2f)
        {
            float start = ring.startDeg + gapDegrees * 0.5f;
            float span = Mathf.Max(1f, ring.sweepDeg - gapDegrees);
            if (outerIndex < 0)
                return start + span * 0.5f;

            int n = ring.subColors?.Length ?? 0;
            if (n <= 0) return start + span * 0.5f;

            float subSpan = span / n;
            float subGap = Mathf.Min(1.2f, subSpan * 0.12f);
            float s0 = start + outerIndex * subSpan + subGap * 0.5f;
            float sSpan = Mathf.Max(0.5f, subSpan - subGap);
            return s0 + sSpan * 0.5f;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            Vector2 center = r.center;
            float radius = Mathf.Min(r.width, r.height) * 0.5f;

            float centerR = radius * _centerR;
            float innerIn = radius * _innerInnerR;
            float innerOut = radius * _innerOuterR;
            float outerIn = radius * _outerInnerR;
            float outerOut = radius * _outerOuterR;

            AddRing(vh, center, 0f, centerR, new Color(0.16f, 0.17f, 0.19f, 1f), 0f, 360f, 40);

            foreach (PillarRingDef ring in PlayerProgressData.Rings)
            {
                Color cat = PlayerProgressData.Hex(ring.colorHex);
                Color dim = cat * 0.32f;
                dim.a = 1f;

                float start = ring.startDeg + _gapDegrees * 0.5f;
                float span = Mathf.Max(1f, ring.sweepDeg - _gapDegrees);

                // Indre felt: eget badge — følger ikke ydre slices
                AddRing(vh, center, innerIn, innerOut, dim, start, span, 18);
                float innerFill = _data?.GetInnerFill01(ring) ?? 0f;
                if (innerFill > 0.001f)
                {
                    float filled = Mathf.Lerp(innerIn, innerOut, innerFill);
                    AddRing(vh, center, innerIn, filled, cat, start, span, 18);
                }

                string[] subs = ring.subColors ?? System.Array.Empty<string>();
                if (subs.Length == 0) continue;

                bool outerUnlocked = _data == null || _data.IsOuterUnlocked(ring);
                float subSpan = span / subs.Length;
                float subGap = Mathf.Min(1.2f, subSpan * 0.12f);
                for (int i = 0; i < subs.Length; i++)
                {
                    Color sub = PlayerProgressData.Hex(subs[i]);
                    Color subDim = outerUnlocked ? sub * 0.4f : new Color(0.28f, 0.28f, 0.3f, 1f);
                    subDim.a = 1f;
                    float s0 = start + i * subSpan + subGap * 0.5f;
                    float sSpan = Mathf.Max(0.5f, subSpan - subGap);

                    AddRing(vh, center, outerIn, outerOut, subDim, s0, sSpan, 10);

                    if (!outerUnlocked) continue;

                    float segFill = _data?.GetSegmentFill01(ring, i) ?? 0f;
                    if (segFill > 0.001f)
                    {
                        float oFilled = Mathf.Lerp(outerIn, outerOut, segFill);
                        AddRing(vh, center, outerIn, oFilled, sub, s0, sSpan, 10);
                    }
                }
            }
        }

        private static void AddRing(
            VertexHelper vh, Vector2 center, float innerR, float outerR,
            Color color, float startDeg, float sweepDeg, int segments)
        {
            if (sweepDeg <= 0f || outerR <= innerR) return;
            segments = Mathf.Max(3, segments);
            int startIndex = vh.currentVertCount;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float deg = startDeg + sweepDeg * t;
                float rad = deg * Mathf.Deg2Rad;
                Vector2 dir = new(Mathf.Sin(rad), Mathf.Cos(rad));
                vh.AddVert((Vector3)(center + dir * innerR), color, Vector2.zero);
                vh.AddVert((Vector3)(center + dir * outerR), color, Vector2.zero);
            }

            for (int i = 0; i < segments; i++)
            {
                int i0 = startIndex + i * 2;
                vh.AddTriangle(i0, i0 + 1, i0 + 3);
                vh.AddTriangle(i0, i0 + 3, i0 + 2);
            }
        }
    }
}
