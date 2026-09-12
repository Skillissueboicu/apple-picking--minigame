using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FarmerQuest.Core;
using FarmerQuest.Net;
using FarmerQuest.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FarmerQuest.Progress
{
    /// <summary>
    /// Climate-hjul + DEBUG-sliders pr. badge/slice. Scroll bevares ved opdatering.
    /// </summary>
    public sealed class PillarWheelPanel : MonoBehaviour
    {
        [SerializeField] private PillarWheelGraphic _wheel;
        [SerializeField] private RectTransform _wheelRoot;
        [SerializeField] private Text _centerLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private RectTransform _badgesBox;
        [SerializeField] private RectTransform _xpButtonsBox;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private bool _showXpDebugButtons;
        [SerializeField] private Button _btnClose;
        [SerializeField] private GameObject _rootToHideOnClose;

        [Header("Editor UI (redigér i scenen)")]
        [SerializeField] private BadgeDetailView _badgeDetail;
        [SerializeField] private BadgeOrbitButton _orbitButtonTemplate;
        [SerializeField] private BadgeSlotRowView _slotRowTemplate;

        private PlayerProgressData _data = new();
        private bool _busy;
        private bool _debugUiBuilt;
        private bool _orbitBuiltOnce;
        private readonly Dictionary<string, SlotUi> _slotUi = new(StringComparer.OrdinalIgnoreCase);
        private static Sprite _bakeCircleSprite;

        private static Font EditorUiFont =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        private static Sprite BakeCircleSprite => _bakeCircleSprite;

        private sealed class SlotUi
        {
            public string slotKey;
            public BadgeSlotRowView row;
            public Text label;
            public Slider slider;
            public bool suppress;
        }

        public PlayerProgressData Current => _data;

        public void SetCloseHandler(Action onClose)
        {
            if (_btnClose == null) return;
            _btnClose.onClick.RemoveAllListeners();
            _btnClose.onClick.AddListener(() =>
            {
                Hide();
                onClose?.Invoke();
            });
        }

        private void Awake()
        {
            HideEditorTemplates();
            if (_btnClose == null) return;
            _btnClose.onClick.RemoveAllListeners();
            _btnClose.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            // Ret collapsed DebugSection (size 0) fra gamle bakes — uden at spawne noget.
            if (_showXpDebugButtons)
                EnsureDebugSectionLayout();
            // Næste Apply/refresh skal måle viewport igen (fuldskærm-hjul)
            _orbitBuiltOnce = false;
        }

        private void HideEditorTemplates()
        {
            Transform templates = transform.Find("UiTemplates");
            if (templates != null)
                templates.gameObject.SetActive(false);
            if (_orbitButtonTemplate != null)
                _orbitButtonTemplate.gameObject.SetActive(false);
            if (_slotRowTemplate != null)
                _slotRowTemplate.gameObject.SetActive(false);
        }

        /// <summary>
        /// Slider-rækker må ikke have size 0 (sker når childControlHeight=true gemmer scenen).
        /// </summary>
        private void EnsureDebugSectionLayout()
        {
            if (_xpButtonsBox == null) return;

            var vlg = _xpButtonsBox.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlHeight = false;
                vlg.childForceExpandHeight = false;
            }

            const float rowWidth = 880f;
            for (int i = 0; i < _xpButtonsBox.childCount; i++)
            {
                var rt = _xpButtonsBox.GetChild(i) as RectTransform;
                if (rt == null) continue;
                var le = rt.GetComponent<LayoutElement>();
                float h = le != null ? Mathf.Max(le.minHeight, le.preferredHeight) : 40f;
                if (h < 1f) h = 40f;
                if (le != null) le.flexibleWidth = 0f;
                // Genskab synlig højde hvis layout har nulstillet sizeDelta
                if (rt.sizeDelta.y < 8f || rt.rect.height < 8f)
                    rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, rowWidth), h);
            }

            FitPreferredHeight(_xpButtonsBox);
            RefreshScrollLayout();
        }

        private void Hide()
        {
            if (_rootToHideOnClose != null) _rootToHideOnClose.SetActive(false);
            else gameObject.SetActive(false);
        }

        public async void RefreshFromServer()
        {
            if (_busy) return;
            _busy = true;
            SetStatus("Henter progress...");
            try
            {
                App.Init();
                if (App.Stats == null)
                {
                    SetStatus("Stats-API mangler.");
                    return;
                }

                PlayerStatDto dto = await App.Stats.GetMineAsync();
                Apply(PlayerStatsApi.ToProgress(dto));
                SetStatus("");
            }
            catch (ApiException ex)
            {
                SetStatus("Kunne ikke hente progress.");
                Debug.LogWarning("[PillarWheel] " + ex.Message);
                if (ex.StatusCode == 401)
                    SceneFlow.GoLogin();
            }
            catch (Exception ex)
            {
                SetStatus("Kunne ikke hente progress.");
                Debug.LogWarning("[PillarWheel] " + ex.Message);
            }
            finally
            {
                _busy = false;
            }
        }

        public void Apply(PlayerProgressData data)
        {
            bool firstDebug = _showXpDebugButtons && !_debugUiBuilt;
            float scrollPos = _scroll != null ? _scroll.verticalNormalizedPosition : 1f;
            _data = data ?? new PlayerProgressData();
            if (_wheel != null) _wheel.SetData(_data);
            if (_centerLabel != null)
                _centerLabel.text = "";

            if (isActiveAndEnabled && !_orbitBuiltOnce)
                StartCoroutine(BuildOrbitWhenReady());
            else
            {
                LayoutWheelToFillViewport();
                RebuildOrbitBadges();
            }

            if (_showXpDebugButtons)
            {
                if (!_debugUiBuilt) WireOrBuildDebugSliders();
                else SyncDebugSliders();
            }

            if (_scroll != null)
            {
                if (!_scroll.enabled) _scroll.enabled = true;
                RefreshScrollLayout();
                // Første gang: start øverst (hjul). Ellers bevar position.
                _scroll.verticalNormalizedPosition = firstDebug ? 1f : Mathf.Clamp01(scrollPos);
            }
        }

        private System.Collections.IEnumerator BuildOrbitWhenReady()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutWheelToFillViewport();
            Canvas.ForceUpdateCanvases();
            RebuildOrbitBadges();
            _orbitBuiltOnce = true;
            RefreshScrollLayout();
        }

        private void RefreshScrollLayout()
        {
            if (_scroll == null || _scroll.content == null) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>Hjul fylder næsten hele viewport (læsbare badges).</summary>
        private void LayoutWheelToFillViewport()
        {
            if (_wheelRoot == null) return;

            float side = 900f;
            if (_scroll != null && _scroll.viewport != null)
            {
                Rect v = _scroll.viewport.rect;
                float avail = Mathf.Min(v.width, v.height);
                if (avail > 80f)
                    side = avail * 0.96f;
            }

            side = Mathf.Clamp(side, 480f, 1400f);
            _wheelRoot.sizeDelta = new Vector2(side, side);

            var host = _wheelRoot.parent as RectTransform;
            if (host != null)
            {
                float hostSide = side + 24f;
                host.sizeDelta = new Vector2(hostSide, hostSide);
                var le = host.GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.preferredWidth = hostSide;
                    le.preferredHeight = hostSide;
                    le.minHeight = hostSide;
                    le.flexibleWidth = 0f;
                }
            }

            if (_wheel != null)
                _wheel.SetVerticesDirty();
        }

        private void RebuildOrbitBadges()
        {
            if (_badgesBox == null) return;

            // Scene-objekter (ingen Instantiate). Place() = samme felt-midte som wheel-mesh.
            var existing = _badgesBox.GetComponentsInChildren<BadgeOrbitButton>(true);
            if (existing == null || existing.Length == 0)
            {
                Debug.LogWarning(
                    "[PillarWheel] Ingen orbit-badges i scenen. Kør FarmQuest Online/Configure Project.");
                return;
            }

            float wheelSize = _wheelRoot != null
                ? Mathf.Min(_wheelRoot.rect.width, _wheelRoot.rect.height)
                : 900f;
            if (wheelSize < 10f && _wheelRoot != null)
                wheelSize = _wheelRoot.sizeDelta.x;
            if (wheelSize < 10f) wheelSize = 900f;

            GetOrbitLayout(wheelSize, out _, out _, out float innerBadge, out float outerBadge);
            float gap = _wheel != null ? _wheel.GapDegrees : 2f;

            var byKey = new Dictionary<string, BadgeOrbitButton>(StringComparer.OrdinalIgnoreCase);
            foreach (BadgeOrbitButton b in existing)
            {
                if (b != null && !string.IsNullOrEmpty(b.SlotKey))
                    byKey[b.SlotKey] = b;
            }

            foreach (PillarRingDef ring in PlayerProgressData.Rings)
            {
                string ikey = PlayerProgressData.InnerSlotKey(ring);
                int innerPts = _data.GetSlotPoints(ikey);
                Vector2 ipos = FieldCenter(ring, -1, wheelSize, gap);
                RefreshOrbitButton(byKey, ikey, ipos, innerBadge,
                    BadgeData.TierFromPoints(innerPts),
                    PlayerProgressData.Hex(ring.colorHex),
                    BadgeCatalog.ShortLabel(ikey),
                    locked: false, boldOutline: true);

                bool outerUnlocked = _data.IsOuterUnlocked(ring);
                int n = ring.subColors?.Length ?? 0;
                for (int i = 0; i < n; i++)
                {
                    string slot = PlayerProgressData.SlotKey(ring, i);
                    int pts = outerUnlocked ? _data.GetSlotPoints(slot) : 0;
                    Color theme = outerUnlocked
                        ? PlayerProgressData.Hex(ring.subColors[i])
                        : new Color(0.45f, 0.45f, 0.48f, 1f);
                    string label = !outerUnlocked ? "🔒" : BadgeCatalog.ShortLabel(slot);
                    Vector2 pos = FieldCenter(ring, i, wheelSize, gap);
                    RefreshOrbitButton(byKey, slot, pos, outerBadge,
                        outerUnlocked ? BadgeData.TierFromPoints(pts) : "Gray",
                        theme, label, locked: !outerUnlocked, boldOutline: false);
                }
            }
        }

        private Vector2 FieldCenter(PillarRingDef ring, int outerIndex, float wheelSize, float gap)
        {
            if (_wheel != null && _wheelRoot != null
                && Mathf.Min(_wheelRoot.rect.width, _wheelRoot.rect.height) > 10f)
                return _wheel.FieldCenterLocal(ring, outerIndex);

            float radius = wheelSize * 0.5f;
            float orbit = radius * (outerIndex < 0 ? 0.37f : 0.72f);
            float midDeg = PillarWheelGraphic.FieldMidDegrees(ring, outerIndex, gap);
            float rad = midDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(rad) * orbit, Mathf.Cos(rad) * orbit);
        }

        private void RefreshOrbitButton(
            Dictionary<string, BadgeOrbitButton> byKey,
            string slotKey, Vector2 pos, float size,
            string tier, Color theme, string label, bool locked, bool boldOutline)
        {
            if (!byKey.TryGetValue(slotKey, out BadgeOrbitButton btn) || btn == null)
                return;
            btn.gameObject.SetActive(true);
            btn.Place(pos, size);
            var outline = btn.GetComponent<Outline>();
            if (outline != null)
                outline.effectDistance = boldOutline ? new Vector2(3.5f, -3.5f) : new Vector2(2f, -2f);
            string key = slotKey;
            btn.Setup(key, tier, theme, label, locked, () => ShowBadgeDetail(key));
        }

        /// <summary>
        /// Radii matcher PillarWheelGraphic (felt-midte). Badge-størrelse skalerer med hjul.
        /// </summary>
        public static void GetOrbitLayout(float wheelSize,
            out float innerOrbit, out float outerOrbit, out float innerBadge, out float outerBadge)
        {
            float radius = wheelSize * 0.5f;
            innerOrbit = radius * 0.37f;
            outerOrbit = radius * 0.72f;
            innerBadge = Mathf.Clamp(wheelSize * 0.095f, 48f, 110f);
            outerBadge = Mathf.Clamp(wheelSize * 0.078f, 40f, 92f);
        }

        private void WireOrBuildDebugSliders()
        {
            if (_xpButtonsBox == null) return;
            var baked = _xpButtonsBox.GetComponentsInChildren<BadgeSlotRowView>(true);
            if (baked == null || baked.Length == 0)
            {
                Debug.LogWarning(
                    "[PillarWheel] Ingen slider-rækker i scenen. Kør FarmQuest Online/Configure Project.");
                return;
            }

            WireBakedSlotRows(baked);
            WireBakedLockToggle();
            EnsureDebugSectionLayout();
            _debugUiBuilt = true;
        }

        private void WireBakedSlotRows(BadgeSlotRowView[] rows)
        {
            _slotUi.Clear();
            foreach (BadgeSlotRowView row in rows)
            {
                if (row == null || string.IsNullOrEmpty(row.SlotKey)) continue;
                string slot = row.SlotKey;
                string title = ResolveSlotTitle(slot);
                bool locked = !_data.CanSetSlotPoints(slot, 1);
                int pts = _data.GetSlotPoints(slot);
                Color? theme = null;
                PillarRingDef ring = _data.FindRingForSlot(slot);
                if (ring != null && PlayerProgressData.IsInnerSlotKey(slot))
                    theme = PlayerProgressData.Hex(ring.colorHex);

                string captured = slot;
                string capturedTitle = title;
                var ui = new SlotUi
                {
                    slotKey = captured,
                    row = row,
                    label = row.TitleLabel,
                    slider = row.Slider,
                    suppress = false,
                };
                _slotUi[captured] = ui;

                string titleText = locked
                    ? $"🔒 {title}  ·  låst"
                    : FormatSlotLabel(title, pts);

                row.Setup(
                    captured,
                    titleText,
                    theme,
                    locked,
                    pts,
                    onTitleClick: () => ShowBadgeDetail(captured),
                    onLiveValue: p =>
                    {
                        if (ui.suppress) return;
                        if (ui.label != null)
                            ui.label.text = FormatSlotLabel(capturedTitle, p);
                        ui.row.ApplyTierMedals(p, locked);
                    },
                    onCommit: () => _ = CommitSlotAsync(captured, Mathf.RoundToInt(ui.slider.value)));
            }
        }

        private void WireBakedLockToggle()
        {
            if (_xpButtonsBox == null) return;
            Toggle lockToggle = _xpButtonsBox.GetComponentInChildren<Toggle>(true);
            if (lockToggle == null) return;
            lockToggle.onValueChanged.RemoveAllListeners();
            lockToggle.SetIsOnWithoutNotify(PlayerProgressRules.LockOuterUntilInnerComplete);
            lockToggle.onValueChanged.AddListener(OnLockOuterToggled);
        }

        private void OnLockOuterToggled(bool on)
        {
            PlayerProgressRules.LockOuterUntilInnerComplete = on;
            SyncDebugSliders();
            RebuildOrbitBadges();
        }

        private static void AddDebugBanner(Transform parent, string text, float height)
        {
            var banner = new GameObject("DebugBanner", typeof(RectTransform));
            banner.transform.SetParent(parent, false);
            var bRt = (RectTransform)banner.transform;
            bRt.sizeDelta = new Vector2(900f, height);
            var bLe = banner.AddComponent<LayoutElement>();
            bLe.preferredWidth = 900f;
            bLe.flexibleWidth = 0f;
            bLe.preferredHeight = height;
            bLe.minHeight = height;
            var bImg = banner.AddComponent<Image>();
            bImg.color = new Color(1f, 0.45f, 0.05f, 1f);
            var outline = banner.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.7f, 0.2f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            var bTextGo = new GameObject("Text", typeof(RectTransform));
            bTextGo.transform.SetParent(banner.transform, false);
            Stretch(bTextGo.transform, 8f);
            var bText = bTextGo.AddComponent<Text>();
            bText.font = EditorUiFont;
            bText.fontSize = 22;
            bText.fontStyle = FontStyle.Bold;
            bText.alignment = TextAnchor.MiddleCenter;
            bText.color = new Color(0.12f, 0.08f, 0.02f, 1f);
            bText.text = text;
            bText.raycastTarget = false;
        }

        private static void AddFixedLabel(Transform parent, string text, int fontSize, float height, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(900f, height);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 900f;
            le.flexibleWidth = 0f;
            le.preferredHeight = height;
            le.minHeight = height;
            var label = go.AddComponent<Text>();
            label.font = EditorUiFont;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = color;
            label.text = text ?? "";
            label.raycastTarget = false;
        }

        private void SyncDebugSliders()
        {
            foreach (var kv in _slotUi)
            {
                SlotUi ui = kv.Value;
                if (ui?.slider == null) continue;
                int pts = _data.GetSlotPoints(ui.slotKey);
                bool locked = !_data.CanSetSlotPoints(ui.slotKey, 1);
                string title = ResolveSlotTitle(ui.slotKey);
                ui.suppress = true;
                ui.slider.SetValueWithoutNotify(pts);
                ui.slider.interactable = !locked;
                if (ui.label != null)
                {
                    ui.label.color = locked
                        ? new Color(0.65f, 0.65f, 0.68f, 1f)
                        : new Color(1f, 0.92f, 0.45f, 1f);
                    ui.label.text = locked
                        ? $"🔒 {title}  ·  låst"
                        : FormatSlotLabel(title, pts);
                }
                if (ui.row != null)
                    ui.row.ApplyTierMedals(pts, locked);
                ui.suppress = false;
            }
        }

        private static string FormatSlotLabel(string title, int pts) =>
            $"{title}  ·  {pts}/300  ·  {BadgeCatalog.DanishTier(BadgeData.TierFromPoints(pts))}";

        private void ShowBadgeDetail(string slotKey)
        {
            if (_badgeDetail == null)
            {
                Debug.LogWarning("[PillarWheel] BadgeDetailView mangler i editoren.");
                return;
            }
            _badgeDetail.Show(slotKey, _data.GetSlotPoints(slotKey));
        }

        private static string ResolveSlotTitle(string slotKey)
        {
            foreach (PillarRingDef ring in PlayerProgressData.Rings)
            {
                if (string.Equals(PlayerProgressData.InnerSlotKey(ring), slotKey, StringComparison.OrdinalIgnoreCase))
                    return PlayerProgressData.InnerSlotLabel(ring);
                int n = ring.subColors?.Length ?? 0;
                for (int i = 0; i < n; i++)
                {
                    if (string.Equals(PlayerProgressData.SlotKey(ring, i), slotKey, StringComparison.OrdinalIgnoreCase))
                        return PlayerProgressData.SlotLabel(ring, i);
                }
            }
            return slotKey;
        }

        private static void Stretch(Transform t, float pad)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        private async Task CommitSlotAsync(string slotKey, int points)
        {
            if (_busy || App.Stats == null) return;
            points = Mathf.Clamp(points, 0, PlayerProgressData.MaxSlotPoints);
            // Undgå unødvendig POST / dobbelt-commit fra handle+track
            if (_data.GetSlotPoints(slotKey) == points) return;
            if (!_data.CanSetSlotPoints(slotKey, points))
            {
                SetStatus("Ydre låst — optjen indre badge først.");
                SyncDebugSliders();
                return;
            }
            _busy = true;
            float scrollPos = _scroll != null ? _scroll.verticalNormalizedPosition : 1f;
            try
            {
                PlayerStatDto dto = await App.Stats.SetSlotPointsAsync(slotKey, points);
                _data = PlayerStatsApi.ToProgress(dto);
                if (_wheel != null) _wheel.SetData(_data);
                if (_centerLabel != null)
                    _centerLabel.text = "";
                RebuildOrbitBadges();
                SyncDebugSliders();
                SetStatus($"DEBUG slot {slotKey} = {points}");
            }
            catch (Exception ex)
            {
                SetStatus("DEBUG slot fejlede.");
                Debug.LogWarning("[PillarWheel] slot: " + ex.Message);
            }
            finally
            {
                if (_scroll != null)
                {
                    _scroll.enabled = true;
                    RefreshScrollLayout();
                    _scroll.verticalNormalizedPosition = Mathf.Clamp01(scrollPos);
                }
                _busy = false;
            }
        }

        private void SetStatus(string msg)
        {
            if (_statusLabel != null) _statusLabel.text = msg ?? "";
        }

        private static string ShortBadge(string key) => BadgeCatalog.ShortLabel(key);

        public static PillarWheelPanel Build(
            Transform parent, bool showXpButtons, bool showClose, string title = "Climate Wheel",
            Sprite circleSprite = null)
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[PillarWheel] Build() er kun til editor/Configure — ikke Play.");
                return null;
            }

            _bakeCircleSprite = circleSprite;
            if (_bakeCircleSprite == null)
            {
                Debug.LogError("[PillarWheel] circle-sprite.png mangler. Læg den i Assets/_FarmerQuest/UI/.");
                return null;
            }

            // Stort hjul i editor; Play skalerer til viewport via LayoutWheelToFillViewport
            const float wheelSize = 960f;
            const float contentWidth = 1000f;

            var root = new GameObject("PillarWheelPanel", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.11f, 0.13f, 0.96f);
            bg.raycastTarget = false;

            // ScrollRect (root) → Viewport (mask) → Content (én lodret liste)
            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            scrollGo.transform.SetParent(root.transform, false);
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(0f, showClose ? 84f : 0f);
            scrollRt.offsetMax = Vector2.zero;
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.scrollSensitivity = 80f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = (RectTransform)viewportGo.transform;
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(8f, 8f);
            viewportRt.offsetMax = new Vector2(-8f, -8f);
            var viewportHit = viewportGo.AddComponent<Image>();
            viewportHit.color = new Color(0f, 0f, 0f, 0.02f);
            viewportHit.raycastTarget = true;
            viewportGo.AddComponent<RectMask2D>();

            var content = CreateScrollContent(viewportGo.transform, contentWidth);
            scroll.content = content;
            scroll.viewport = viewportRt;

            AddFixedLabel(content, title, 34, 44f, Color.white);
            if (showXpButtons)
            {
                AddFixedLabel(content, "Scroll ned for DEBUG-sliders under hjulet", 18, 28f,
                    new Color(1f, 0.55f, 0.1f, 1f));
            }

            float host = wheelSize + 80f;
            var hostGo = new GameObject("WheelHost", typeof(RectTransform));
            hostGo.transform.SetParent(content, false);
            var hostRt = (RectTransform)hostGo.transform;
            hostRt.sizeDelta = new Vector2(host, host);
            var hostLe = hostGo.AddComponent<LayoutElement>();
            hostLe.preferredWidth = contentWidth;
            hostLe.flexibleWidth = 1f;
            hostLe.preferredHeight = host;
            hostLe.minHeight = host;

            var wheelGo = new GameObject("Wheel", typeof(RectTransform));
            wheelGo.transform.SetParent(hostGo.transform, false);
            var wheelRt = (RectTransform)wheelGo.transform;
            wheelRt.anchorMin = wheelRt.anchorMax = new Vector2(0.5f, 0.5f);
            wheelRt.pivot = new Vector2(0.5f, 0.5f);
            wheelRt.sizeDelta = new Vector2(wheelSize, wheelSize);
            var wheel = wheelGo.AddComponent<PillarWheelGraphic>();
            wheel.color = Color.white;
            wheel.raycastTarget = false;

            // Badges som barn af Wheel — samme koordinatsystem som PillarWheelGraphic
            var badgesGo = new GameObject("BadgesOrbit", typeof(RectTransform));
            badgesGo.transform.SetParent(wheelGo.transform, false);
            var badges = (RectTransform)badgesGo.transform;
            badges.anchorMin = Vector2.zero;
            badges.anchorMax = Vector2.one;
            badges.offsetMin = Vector2.zero;
            badges.offsetMax = Vector2.zero;

            var centerGo = new GameObject("CenterLabel", typeof(RectTransform));
            centerGo.transform.SetParent(wheelGo.transform, false);
            var cRt = (RectTransform)centerGo.transform;
            cRt.anchorMin = new Vector2(0.32f, 0.32f);
            cRt.anchorMax = new Vector2(0.68f, 0.68f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            var center = centerGo.AddComponent<Text>();
            center.font = EditorUiFont;
            center.fontSize = 26;
            center.alignment = TextAnchor.MiddleCenter;
            center.color = Color.white;
            center.text = "";
            center.raycastTarget = false;

            // Egne sektion under hjulet — må ikke være hele Content (ellers slettes hjulet)
            RectTransform xpBox = null;
            if (showXpButtons)
                xpBox = CreateDebugSection(content, contentWidth);

            Text status = null;
            {
                var statusGo = new GameObject("Status", typeof(RectTransform));
                statusGo.transform.SetParent(content, false);
                ((RectTransform)statusGo.transform).sizeDelta = new Vector2(0f, 28f);
                var statusLe = statusGo.AddComponent<LayoutElement>();
                statusLe.preferredWidth = contentWidth;
                statusLe.flexibleWidth = 1f;
                statusLe.preferredHeight = 28f;
                status = statusGo.AddComponent<Text>();
                status.font = EditorUiFont;
                status.fontSize = 18;
                status.alignment = TextAnchor.MiddleCenter;
                status.color = new Color(0.85f, 0.85f, 0.85f, 1f);
                status.raycastTarget = false;
            }

            Button close = null;
            if (showClose)
            {
                var closeBar = new GameObject("CloseBar", typeof(RectTransform));
                closeBar.transform.SetParent(root.transform, false);
                var cbrt = (RectTransform)closeBar.transform;
                cbrt.anchorMin = new Vector2(0.5f, 0f);
                cbrt.anchorMax = new Vector2(0.5f, 0f);
                cbrt.pivot = new Vector2(0.5f, 0f);
                cbrt.anchoredPosition = new Vector2(0f, 12f);
                cbrt.sizeDelta = new Vector2(300f, 58f);
                close = BakeTextButton(closeBar.transform, "Luk",
                    new Color(0.4f, 0.4f, 0.45f, 1f), 300f, 58f, 22);
                var closeRt = close.GetComponent<RectTransform>();
                closeRt.anchorMin = Vector2.zero;
                closeRt.anchorMax = Vector2.one;
                closeRt.offsetMin = Vector2.zero;
                closeRt.offsetMax = Vector2.zero;
                var closeLe = close.GetComponent<LayoutElement>();
                if (closeLe != null)
                {
                    if (Application.isPlaying) Destroy(closeLe);
                    else DestroyImmediate(closeLe);
                }
            }

            // Editor-templates — deaktiveret, uden for synlig UI (kun til Instantiate ved Configure)
            var templates = new GameObject("UiTemplates", typeof(RectTransform));
            templates.transform.SetParent(root.transform, false);
            var templatesRt = (RectTransform)templates.transform;
            templatesRt.anchorMin = templatesRt.anchorMax = new Vector2(0f, 0f);
            templatesRt.pivot = new Vector2(0f, 0f);
            templatesRt.anchoredPosition = new Vector2(-4000f, -4000f);
            templatesRt.sizeDelta = new Vector2(1f, 1f);

            BadgeOrbitButton orbitTpl = BakeOrbitButtonTemplate(templates.transform);
            BadgeSlotRowView rowTpl = BakeSlotRowTemplate(templates.transform);
            BadgeDetailView detail = BakeBadgeDetail(root.transform);

            // Alle badges + sliders ind i hierarchy (synlige i editor uden Play)
            BakeOrbitBadgesInto(badges, orbitTpl, wheelSize);
            if (xpBox != null)
            {
                BakeDebugSectionInto(xpBox, rowTpl, includeHeaders: true);
                FinalizeDebugSectionBake(xpBox, contentWidth);
            }

            templates.SetActive(false);

            var panel = root.AddComponent<PillarWheelPanel>();
            panel._wheel = wheel;
            panel._wheelRoot = wheelRt;
            panel._centerLabel = center;
            panel._statusLabel = status;
            panel._badgesBox = badges;
            panel._xpButtonsBox = xpBox;
            panel._scroll = scroll;
            panel._showXpDebugButtons = showXpButtons;
            panel._btnClose = close;
            panel._rootToHideOnClose = root;
            panel._orbitButtonTemplate = orbitTpl;
            panel._slotRowTemplate = rowTpl;
            panel._badgeDetail = detail;
            if (close != null)
            {
                close.onClick.RemoveAllListeners();
                close.onClick.AddListener(() => root.SetActive(false));
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float contentH = LayoutUtility.GetPreferredHeight(content);
            if (contentH < 100f)
            {
                // Fallback hvis CSF ikke har kørt endnu i editor
                contentH = 0f;
                for (int i = 0; i < content.childCount; i++)
                {
                    var le = content.GetChild(i).GetComponent<LayoutElement>();
                    contentH += le != null ? Mathf.Max(le.minHeight, le.preferredHeight) : 40f;
                    contentH += 8f;
                }
                contentH += 40f;
            }
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentH);
            scroll.verticalNormalizedPosition = 1f;
            return panel;
        }

        /// <summary>Efter bake: rækker skal have reel højde (ikke 0×0) i scenen.</summary>
        private static void FinalizeDebugSectionBake(RectTransform xpBox, float width)
        {
            if (xpBox == null) return;
            var vlg = xpBox.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlHeight = false;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth = true;
                vlg.childForceExpandWidth = true;
            }

            float innerW = Mathf.Max(100f, width - 16f);
            for (int i = 0; i < xpBox.childCount; i++)
            {
                var rt = xpBox.GetChild(i) as RectTransform;
                if (rt == null) continue;
                var le = rt.GetComponent<LayoutElement>();
                float h = le != null ? Mathf.Max(le.minHeight, le.preferredHeight) : 40f;
                if (h < 1f) h = 40f;
                if (le != null)
                {
                    le.flexibleWidth = 0f;
                    le.preferredWidth = innerW;
                }
                rt.sizeDelta = new Vector2(innerW, h);
            }

            FitPreferredHeight(xpBox);
            var boxLe = xpBox.GetComponent<LayoutElement>();
            if (boxLe != null)
            {
                boxLe.preferredWidth = width;
                boxLe.flexibleWidth = 0f;
            }
            xpBox.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        /// <summary>Opretter alle 33 orbit-badges under BadgesOrbit (kun editor/Configure).</summary>
        public static void BakeOrbitBadgesInto(RectTransform badgesBox, BadgeOrbitButton template, float wheelSize)
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[PillarWheel] BakeOrbitBadgesInto må ikke kaldes i Play.");
                return;
            }
            if (badgesBox == null || template == null) return;
            for (int i = badgesBox.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(badgesBox.GetChild(i).gameObject);

            GetOrbitLayout(wheelSize, out float innerOrbit, out float outerOrbit,
                out float innerBadge, out float outerBadge);
            const float gap = 2f;

            foreach (PillarRingDef ring in PlayerProgressData.Rings)
            {
                // Samme felt-midte som PillarWheelGraphic (inkl. gaps)
                float imid = PillarWheelGraphic.FieldMidDegrees(ring, -1, gap);
                float irad = imid * Mathf.Deg2Rad;
                Vector2 ipos = new(Mathf.Sin(irad) * innerOrbit, Mathf.Cos(irad) * innerOrbit);
                string ikey = PlayerProgressData.InnerSlotKey(ring);
                CreateBakedOrbit(badgesBox, template, $"Inner_{ring.key}", ikey, ipos, innerBadge,
                    BadgeData.TierFromPoints(0), PlayerProgressData.Hex(ring.colorHex),
                    BadgeCatalog.ShortLabel(ikey), boldOutline: true);

                int n = ring.subColors?.Length ?? 0;
                for (int i = 0; i < n; i++)
                {
                    float omid = PillarWheelGraphic.FieldMidDegrees(ring, i, gap);
                    float orad = omid * Mathf.Deg2Rad;
                    Vector2 pos = new(Mathf.Sin(orad) * outerOrbit, Mathf.Cos(orad) * outerOrbit);
                    string slot = PlayerProgressData.SlotKey(ring, i);
                    CreateBakedOrbit(badgesBox, template, $"Slot_{ring.key}_{i}", slot, pos, outerBadge,
                        "Gray", PlayerProgressData.Hex(ring.subColors[i]),
                        BadgeCatalog.ShortLabel(slot), boldOutline: false);
                }
            }
        }

        private static void CreateBakedOrbit(
            RectTransform parent, BadgeOrbitButton template,
            string name, string slotKey, Vector2 pos, float size,
            string tier, Color theme, string label, bool boldOutline)
        {
            BadgeOrbitButton btn = Instantiate(template, parent);
            btn.gameObject.name = name;
            btn.gameObject.SetActive(true);
            btn.Place(pos, size);
            var outline = btn.GetComponent<Outline>();
            if (outline != null)
                outline.effectDistance = boldOutline ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
            btn.Setup(slotKey, tier, theme, label, locked: false, onClick: null);
        }

        /// <summary>Opretter DEBUG-headers + alle 33 slider-rækker (kun editor/Configure).</summary>
        public static void BakeDebugSectionInto(
            RectTransform xpBox, BadgeSlotRowView rowTemplate, bool includeHeaders)
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[PillarWheel] BakeDebugSectionInto må ikke kaldes i Play.");
                return;
            }
            if (xpBox == null || rowTemplate == null) return;
            for (int i = xpBox.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(xpBox.GetChild(i).gameObject);

            if (includeHeaders)
            {
                AddDebugBanner(xpBox, "DEBUG MODE  ·  Hvert felt = eget badge (0–300)", 52f);
                AddFixedLabel(xpBox,
                    "Indre (5) og ydre felter er separate. Slip slider for at gemme.\nBronze 100 · Sølv 200 · Guld 300",
                    15, 52f, new Color(1f, 0.78f, 0.35f, 1f));

                Toggle lockToggle = BakeLockToggle(xpBox,
                    "Lås ydre indtil indre badge er optjent",
                    PlayerProgressRules.LockOuterUntilInnerComplete);
                var lockLe = lockToggle.transform.parent.GetComponent<LayoutElement>();
                if (lockLe != null)
                {
                    lockLe.preferredWidth = 900f;
                    lockLe.preferredHeight = 48f;
                }

                AddFixedLabel(xpBox,
                    "Ydre er grå/låst indtil indre badge i samme kategori er optjent.",
                    14, 36f, new Color(0.9f, 0.75f, 0.4f, 1f));

                AddFixedLabel(xpBox, "▸ Indre badges (5 kategorier)", 22, 34f,
                    new Color(1f, 0.7f, 0.25f, 1f));
            }

            foreach (PillarRingDef ring in PlayerProgressData.Rings)
            {
                CreateBakedSlotRow(xpBox, rowTemplate,
                    PlayerProgressData.InnerSlotKey(ring),
                    PlayerProgressData.InnerSlotLabel(ring),
                    PlayerProgressData.Hex(ring.colorHex),
                    locked: false);
            }

            foreach (PillarRingDef ring in PlayerProgressData.Rings)
            {
                if (includeHeaders)
                {
                    AddFixedLabel(xpBox, $"▸ Ydre · {ring.label}", 22, 34f,
                        PlayerProgressData.Hex(ring.colorHex));
                }
                int n = ring.subColors?.Length ?? 0;
                for (int i = 0; i < n; i++)
                {
                    CreateBakedSlotRow(xpBox, rowTemplate,
                        PlayerProgressData.SlotKey(ring, i),
                        PlayerProgressData.SlotLabel(ring, i),
                        theme: null,
                        locked: false);
                }
            }

            if (includeHeaders)
                AddFixedLabel(xpBox, "", 14, 40f, Color.clear);
        }

        private static void CreateBakedSlotRow(
            RectTransform parent, BadgeSlotRowView template,
            string slotKey, string title, Color? theme, bool locked)
        {
            BadgeSlotRowView row = Instantiate(template, parent);
            row.gameObject.name = $"Slot_{slotKey}";
            row.gameObject.SetActive(true);
            var rt = row.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(900f, 88f);
            var le = row.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = 900f;
                le.preferredHeight = 88f;
                le.minHeight = 88f;
                le.flexibleWidth = 0f;
            }
            row.Setup(slotKey, FormatSlotLabel(title, 0), theme, locked, 0,
                onTitleClick: null, onLiveValue: null, onCommit: null);
        }

        private static BadgeOrbitButton BakeOrbitButtonTemplate(Transform parent)
        {
            var go = new GameObject("BadgeOrbitButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(48f, 48f);

            var img = go.AddComponent<Image>();
            img.sprite = BakeCircleSprite;
            img.preserveAspect = true;
            img.raycastTarget = true;
            img.color = BadgeData.TierColor("Gray");

            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2f, -2f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            Stretch(labelGo.transform, 3f);
            var label = labelGo.AddComponent<Text>();
            label.font = EditorUiFont;
            label.fontSize = 12;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = "Badge";

            var view = go.AddComponent<BadgeOrbitButton>();
            SetPrivate(view, "_button", btn);
            SetPrivate(view, "_fill", img);
            SetPrivate(view, "_outline", outline);
            SetPrivate(view, "_label", label);
            go.SetActive(false);
            return view;
        }

        private static BadgeSlotRowView BakeSlotRowTemplate(Transform parent)
        {
            var go = new GameObject("BadgeSlotRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(900f, 88f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 900f;
            le.flexibleWidth = 0f;
            le.preferredHeight = 88f;
            le.minHeight = 88f;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.28f, 0.18f, 0.04f, 0.95f);

            var titleBtnGo = new GameObject("TitleBtn", typeof(RectTransform));
            titleBtnGo.transform.SetParent(go.transform, false);
            var tbrt = (RectTransform)titleBtnGo.transform;
            tbrt.anchorMin = new Vector2(0f, 0.52f);
            tbrt.anchorMax = new Vector2(0.72f, 1f);
            tbrt.offsetMin = new Vector2(8f, 2f);
            tbrt.offsetMax = new Vector2(-4f, -4f);
            var titleBg = titleBtnGo.AddComponent<Image>();
            titleBg.color = new Color(0f, 0f, 0f, 0.15f);
            var titleBtn = titleBtnGo.AddComponent<Button>();
            titleBtn.targetGraphic = titleBg;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(titleBtnGo.transform, false);
            Stretch(labelGo.transform, 4f);
            var label = labelGo.AddComponent<Text>();
            label.font = EditorUiFont;
            label.fontSize = 15;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = new Color(1f, 0.92f, 0.45f, 1f);
            label.raycastTarget = false;
            label.text = "Badge · 0/300 · Ikke optjent";

            Image bronze = BakeMedal(go.transform, "Bronze", new Vector2(0.74f, 0.55f), new Vector2(0.82f, 0.95f), "B");
            Image silver = BakeMedal(go.transform, "Silver", new Vector2(0.83f, 0.55f), new Vector2(0.91f, 0.95f), "S");
            Image gold = BakeMedal(go.transform, "Gold", new Vector2(0.92f, 0.55f), new Vector2(1f, 0.95f), "G");

            Slider slider = BakeSlider(go.transform);

            var view = go.AddComponent<BadgeSlotRowView>();
            SetPrivate(view, "_background", bg);
            SetPrivate(view, "_titleButton", titleBtn);
            SetPrivate(view, "_titleLabel", label);
            SetPrivate(view, "_bronzeMedal", bronze);
            SetPrivate(view, "_silverMedal", silver);
            SetPrivate(view, "_goldMedal", gold);
            SetPrivate(view, "_slider", slider);
            go.SetActive(false);
            return view;
        }

        private static Image BakeMedal(Transform parent, string name, Vector2 aMin, Vector2 aMax, string letter)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);
            var img = go.AddComponent<Image>();
            img.sprite = BakeCircleSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = new Color(0.28f, 0.28f, 0.3f, 0.7f);

            var textGo = new GameObject("L", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.transform, 1f);
            var t = textGo.AddComponent<Text>();
            t.font = EditorUiFont;
            t.fontSize = 12;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            t.text = letter;
            return img;
        }

        private static Slider BakeSlider(Transform parent)
        {
            var go = new GameObject("Slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.offsetMin = new Vector2(10f, 6f);
            rt.offsetMax = new Vector2(-10f, -2f);

            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            Stretch(bg.transform, 0f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.12f, 0.08f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fa = (RectTransform)fillArea.transform;
            fa.anchorMin = new Vector2(0f, 0.25f);
            fa.anchorMax = new Vector2(1f, 0.75f);
            fa.offsetMin = new Vector2(6f, 0f);
            fa.offsetMax = new Vector2(-6f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            Stretch(fill.transform, 0f);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.95f, 0.55f, 0.1f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            Stretch(handleArea.transform, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea.transform, false);
            var hrt = (RectTransform)handle.transform;
            hrt.sizeDelta = new Vector2(22f, 22f);
            var hImg = handle.AddComponent<Image>();
            hImg.sprite = BakeCircleSprite;
            hImg.color = new Color(1f, 0.85f, 0.3f, 1f);

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = PlayerProgressData.MaxSlotPoints;
            slider.wholeNumbers = true;
            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = hrt;
            slider.targetGraphic = hImg;
            slider.direction = Slider.Direction.LeftToRight;
            go.AddComponent<ScrollDragBlocker>();
            if (go.GetComponent<DebugSliderCommitRelay>() == null)
                go.AddComponent<DebugSliderCommitRelay>();
            return slider;
        }

        private static BadgeDetailView BakeBadgeDetail(Transform parent)
        {
            var root = new GameObject("BadgeDetail", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;
            var dimBtn = root.AddComponent<Button>();
            dimBtn.targetGraphic = dim;
            dimBtn.transition = Selectable.Transition.None;

            var card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(root.transform, false);
            var cardRt = (RectTransform)card.transform;
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(560f, 520f);
            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.12f, 0.13f, 0.16f, 1f);
            cardImg.raycastTarget = true;
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(1f, 0.7f, 0.25f, 1f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            var tierGo = new GameObject("Tier", typeof(RectTransform));
            tierGo.transform.SetParent(card.transform, false);
            var tierRt = (RectTransform)tierGo.transform;
            tierRt.anchorMin = tierRt.anchorMax = new Vector2(0f, 1f);
            tierRt.pivot = new Vector2(0f, 1f);
            tierRt.anchoredPosition = new Vector2(20f, -16f);
            tierRt.sizeDelta = new Vector2(48f, 48f);
            var tierIcon = tierGo.AddComponent<Image>();
            tierIcon.sprite = BakeCircleSprite;
            tierIcon.preserveAspect = true;
            tierIcon.raycastTarget = false;

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(card.transform, false);
            var titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(80f, -64f);
            titleRt.offsetMax = new Vector2(-20f, -16f);
            var title = titleGo.AddComponent<Text>();
            title.font = EditorUiFont;
            title.fontSize = 26;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleLeft;
            title.color = new Color(1f, 0.9f, 0.45f, 1f);
            title.raycastTarget = false;
            title.text = "Badge";

            var bodyGo = new GameObject("Body", typeof(RectTransform));
            bodyGo.transform.SetParent(card.transform, false);
            var bodyRt = (RectTransform)bodyGo.transform;
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(24f, 72f);
            bodyRt.offsetMax = new Vector2(-24f, -72f);
            var body = bodyGo.AddComponent<Text>();
            body.font = EditorUiFont;
            body.fontSize = 17;
            body.alignment = TextAnchor.UpperLeft;
            body.color = new Color(0.92f, 0.93f, 0.95f, 1f);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.raycastTarget = false;
            body.text = "Beskrivelse…";

            var closeGo = new GameObject("Close", typeof(RectTransform));
            closeGo.transform.SetParent(card.transform, false);
            var closeRt = (RectTransform)closeGo.transform;
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 16f);
            closeRt.sizeDelta = new Vector2(200f, 44f);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.95f, 0.55f, 0.12f, 1f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            var closeTextGo = new GameObject("Text", typeof(RectTransform));
            closeTextGo.transform.SetParent(closeGo.transform, false);
            Stretch(closeTextGo.transform, 4f);
            var closeText = closeTextGo.AddComponent<Text>();
            closeText.font = EditorUiFont;
            closeText.fontSize = 20;
            closeText.fontStyle = FontStyle.Bold;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.color = Color.white;
            closeText.text = "Luk";
            closeText.raycastTarget = false;

            var view = root.AddComponent<BadgeDetailView>();
            SetPrivate(view, "_root", root);
            SetPrivate(view, "_title", title);
            SetPrivate(view, "_body", body);
            SetPrivate(view, "_tierIcon", tierIcon);
            SetPrivate(view, "_btnClose", closeBtn);
            SetPrivate(view, "_btnDimBackground", dimBtn);
            root.SetActive(false);
            return view;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var f = target.GetType().GetField(field,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            f?.SetValue(target, value);
        }

        private static Button BakeTextButton(
            Transform parent, string text, Color color, float width, float height, int fontSize)
        {
            var go = new GameObject("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(width, height);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;

            var txtGo = new GameObject("Text", typeof(RectTransform));
            var txtRt = (RectTransform)txtGo.transform;
            txtRt.SetParent(go.transform, false);
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var label = txtGo.AddComponent<Text>();
            label.text = text;
            label.font = EditorUiFont;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return btn;
        }

        private static Toggle BakeLockToggle(Transform parent, string caption, bool on)
        {
            var row = new GameObject("ToggleRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            ((RectTransform)row.transform).sizeDelta = new Vector2(640f, 48f);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 12f;
            h.childControlWidth = false;
            h.childControlHeight = false;
            var le = row.AddComponent<LayoutElement>();
            le.preferredWidth = 640f;
            le.preferredHeight = 48f;

            var toggleGo = new GameObject("Toggle", typeof(RectTransform));
            toggleGo.transform.SetParent(row.transform, false);
            ((RectTransform)toggleGo.transform).sizeDelta = new Vector2(36f, 36f);
            var bg = toggleGo.AddComponent<Image>();
            bg.color = Color.white;
            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.targetGraphic = bg;

            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            checkGo.transform.SetParent(toggleGo.transform, false);
            Stretch(checkGo.transform, 6f);
            var check = checkGo.AddComponent<Image>();
            check.color = new Color(0.353f, 0.561f, 0.227f, 1f);
            toggle.graphic = check;
            toggle.isOn = on;

            AddFixedLabel(row.transform, caption, 22, 40f, new Color(0.96f, 0.96f, 0.96f, 1f));
            return toggle;
        }

        /// <summary>Én scroll-content: top-forankret, bredde = viewport, højde = sum af børn.</summary>
        private static RectTransform CreateScrollContent(Transform viewport, float width)
        {
            var go = new GameObject("Content", typeof(RectTransform));
            go.transform.SetParent(viewport, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 0f);

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 8, 32);
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            return rt;
        }

        /// <summary>
        /// Debug-sektion uden ContentSizeFitter (nested CSF ødelægger scroll-højde).
        /// Højde sættes eksplicit via FitPreferredHeight.
        /// </summary>
        private static RectTransform CreateDebugSection(Transform parent, float width)
        {
            var go = new GameObject("DebugSection", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(width, 0f);

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            // Height styres af børnenes sizeDelta — ellers gemmes 0×0 i scenen
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.11f, 0.03f, 0.9f);
            bg.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.5f, 0.05f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.flexibleWidth = 0f;
            le.preferredHeight = 80f;
            le.minHeight = 80f;
            rt.sizeDelta = new Vector2(width, 80f);
            return rt;
        }

        private static void FitPreferredHeight(RectTransform root)
        {
            if (root == null) return;
            var vlg = root.GetComponent<VerticalLayoutGroup>();
            float pad = vlg != null ? vlg.padding.top + vlg.padding.bottom : 0f;
            float spacing = vlg != null ? vlg.spacing : 0f;
            float h = pad;
            int n = root.childCount;
            for (int i = 0; i < n; i++)
            {
                var child = root.GetChild(i) as RectTransform;
                var childLe = child != null ? child.GetComponent<LayoutElement>() : null;
                float ch = childLe != null
                    ? Mathf.Max(childLe.minHeight, childLe.preferredHeight)
                    : (child != null ? child.sizeDelta.y : 40f);
                if (ch < 1f) ch = 40f;
                h += ch;
                if (i < n - 1) h += spacing;
            }

            var le = root.GetComponent<LayoutElement>() ?? root.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = h;
            le.minHeight = h;
            le.flexibleWidth = 0f;
            float w = le.preferredWidth > 10f ? le.preferredWidth : root.sizeDelta.x;
            if (w < 10f) w = 900f;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
        }
    }
}
