using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

namespace LangkaLens.MR
{
    [DefaultExecutionOrder(300)]
    public class MRMeshAreaFeedback : MonoBehaviour
    {
        public MRMeshPlayableArea area;
        public MRMeshReferenceAlignment alignment;
        public TMP_FontAsset font;
        public Material lineMaterial;
        public bool alwaysShowBoundary;
        public float boundaryDisplayHeight = 0.06f;

        private GameObject _panel;
        private TextMeshProUGUI _text;
        private LineRenderer _referenceOutline;
        private LineRenderer _headingArrow;
        private LineRenderer _boundaryOutline;
        private LineRenderer _ruinOutline;
        private LineRenderer[] _floorEdges;
        private Vector2[] _floorPoints;
        private readonly int[] _boxEdges = { 0, 1, 2, 3, 0, 4, 5, 1, 5, 6, 2, 6, 7, 3, 7, 4 };
        private readonly Vector3[] _boxCorners = new Vector3[8];
        private Vector3[] _perimeter;
        private Vector3[] _ruinPerimeter;
        private Vector2[] _outerPoints;
        private Vector2[] _ruinPoints;
        private Material _runtimeMaterial;

        private void Start()
        {
            if (area == null || alignment == null || alignment.head == null || lineMaterial == null)
            { enabled = false; return; }
            _runtimeMaterial = new Material(lineMaterial);
            _referenceOutline = MakeLine("Entrance sign outline", 0.009f, false);
            _referenceOutline.positionCount = _boxEdges.Length;
            _headingArrow = MakeLine("Direction into the site", 0.018f, false);
            _headingArrow.positionCount = 5;
            _boundaryOutline = MakeLine("Playable interior outline", 0.035f, true);
            _outerPoints = area.OuterOutline;
            _ruinPoints = area.ExclusionOutline;
            _perimeter = new Vector3[_outerPoints.Length];
            _boundaryOutline.positionCount = _perimeter.Length;
            _ruinOutline = MakeLine("Central ruin exclusion", 0.035f, true);
            _ruinPerimeter = new Vector3[_ruinPoints.Length];
            _ruinOutline.positionCount = _ruinPerimeter.Length;
            if (area.UsesMeshFloors && area.FloorFootprint != null)
            {
                _floorPoints = area.FloorFootprint.Edges;
                _floorEdges = new LineRenderer[_floorPoints.Length / 2];
                for (int i = 0; i < _floorEdges.Length; i++)
                {
                    _floorEdges[i] = MakeLine("Mesh walking edge " + i, 0.035f, false);
                    _floorEdges[i].positionCount = 2;
                }
            }

            _panel = new GameObject("Site setup and boundary status", typeof(RectTransform), typeof(Canvas));
            _panel.transform.SetParent(alignment.head, false);
            _panel.transform.localPosition = new Vector3(0, -0.26f, 1.05f);
            _panel.transform.localScale = Vector3.one * 0.001f;
            var canvas = _panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = alignment.head.GetComponent<Camera>();
            var rect = _panel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(780, 240);
            var image = _panel.AddComponent<Image>();
            image.color = new Color(0.025f, 0.045f, 0.06f, 0.82f);
            image.raycastTarget = false;

            var textObject = new GameObject("Instructions", typeof(RectTransform));
            textObject.transform.SetParent(_panel.transform, false);
            _text = textObject.AddComponent<TextMeshProUGUI>();
            if (font != null) _text.font = font;
            _text.fontSize = 27;
            _text.enableAutoSizing = true;
            _text.fontSizeMin = 21;
            _text.fontSizeMax = 27;
            _text.alignment = TextAlignmentOptions.Center;
            _text.color = Color.white;
            _text.raycastTarget = false;
            _text.rectTransform.anchorMin = Vector2.zero;
            _text.rectTransform.anchorMax = Vector2.one;
            _text.rectTransform.offsetMin = new Vector2(18, 12);
            _text.rectTransform.offsetMax = new Vector2(-18, -12);
        }

        private LineRenderer MakeLine(string label, float width, bool loop)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = _runtimeMaterial;
            line.widthMultiplier = width;
            line.useWorldSpace = true;
            line.loop = loop;
            line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        private void LateUpdate()
        {
            if (_text == null) return;
            bool settingUp = !alignment.IsConfirmed;
            _referenceOutline.enabled = settingUp && alignment.HasPlacedPreview && alignment.HeadTracked && alignment.meshReference != null;
            _headingArrow.enabled = _referenceOutline.enabled;
            if (_referenceOutline.enabled)
            {
                Bounds b = alignment.meshReference.sharedMesh.bounds;
                Vector3 min = b.min, max = b.max;
                _boxCorners[0] = new Vector3(min.x, min.y, min.z);
                _boxCorners[1] = new Vector3(max.x, min.y, min.z);
                _boxCorners[2] = new Vector3(max.x, max.y, min.z);
                _boxCorners[3] = new Vector3(min.x, max.y, min.z);
                _boxCorners[4] = new Vector3(min.x, min.y, max.z);
                _boxCorners[5] = new Vector3(max.x, min.y, max.z);
                _boxCorners[6] = new Vector3(max.x, max.y, max.z);
                _boxCorners[7] = new Vector3(min.x, max.y, max.z);
                for (int i = 0; i < _boxEdges.Length; i++)
                    _referenceOutline.SetPosition(i, alignment.meshReference.transform.TransformPoint(_boxCorners[_boxEdges[i]]));
                _referenceOutline.startColor = _referenceOutline.endColor = Color.cyan;
                Vector3 centre = alignment.meshReference.transform.TransformPoint(b.center);
                Vector3 target = area.transform.TransformPoint(new Vector3(area.interiorSeed.x, 0, area.interiorSeed.y));
                Vector3 direction = Vector3.ProjectOnPlane(target - centre, Vector3.up).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, direction);
                Vector3 start = centre + Vector3.up * 0.55f;
                Vector3 tip = start + direction * 0.8f;
                _headingArrow.SetPositions(new[] { start, tip, tip - direction * 0.18f + side * 0.1f, tip, tip - direction * 0.18f - side * 0.1f });
                _headingArrow.startColor = _headingArrow.endColor = Color.green;
            }
            bool showBoundary = !area.IsUnrestricted && alignment.IsTrackingValid && (alwaysShowBoundary || area.State == MRMeshPlayableArea.AreaState.NearBoundary || area.State == MRMeshPlayableArea.AreaState.Outside);
            _boundaryOutline.enabled = showBoundary && !area.UsesMeshFloors;
            if (_floorEdges != null)
            {
                Color color = area.CanPlay ? new Color(1, 0.65f, 0.1f) : new Color(1, 0.15f, 0.1f);
                for (int i = 0; i < _floorEdges.Length; i++)
                {
                    var line = _floorEdges[i];
                    line.enabled = showBoundary;
                    if (!showBoundary) continue;
                    for (int j = 0; j < 2; j++)
                    {
                        Vector2 point = _floorPoints[i * 2 + j];
                        line.SetPosition(j, area.transform.TransformPoint(new Vector3(point.x, boundaryDisplayHeight, point.y)));
                    }
                    line.startColor = line.endColor = color;
                }
            }
            _ruinOutline.enabled = _boundaryOutline.enabled && _ruinPoints.Length > 0;
            if (_boundaryOutline.enabled)
            {
                for (int i = 0; i < _perimeter.Length; i++)
                    _perimeter[i] = area.transform.TransformPoint(new Vector3(_outerPoints[i].x, boundaryDisplayHeight, _outerPoints[i].y));
                _boundaryOutline.SetPositions(_perimeter);
                Color color = area.CanPlay ? new Color(1, 0.65f, 0.1f) : new Color(1, 0.15f, 0.1f);
                _boundaryOutline.startColor = _boundaryOutline.endColor = color;
                for (int i = 0; i < _ruinPerimeter.Length; i++)
                    _ruinPerimeter[i] = area.transform.TransformPoint(new Vector3(_ruinPoints[i].x, boundaryDisplayHeight, _ruinPoints[i].y));
                _ruinOutline.SetPositions(_ruinPerimeter);
                _ruinOutline.startColor = _ruinOutline.endColor = color;
            }

            _panel.SetActive(area.State != MRMeshPlayableArea.AreaState.Inside);
            if (area.State == MRMeshPlayableArea.AreaState.Unconfigured)
            {
                string title = GetLocalized("Area_Title_SiteSetupNeeded", "Site setup needed");
                _text.text = $"<b>{title}</b>\n" + area.ConfigurationError;
            }
            else if (!alignment.HeadTracked)
            {
                string msg = GetLocalized("Area_Tracking_Unavailable", "Tracking unavailable\nPause here and look around. The site experience is paused.");
                _text.text = FormatWithBoldFirstLine(msg);
            }
            else if (settingUp)
            {
                string title = GetLocalized("Align_Title_Setup", "Align the entrance sign/table");
                string status = !string.IsNullOrEmpty(alignment.StatusKey)
                    ? GetLocalized(alignment.StatusKey, alignment.Status)
                    : alignment.Status;
                string arrow = GetLocalized("Align_Help_Arrow", "Point the green arrow into the site.");
                string controls = GetLocalized("Align_Controls_Guide", "RIGHT GRIP: move  •  RIGHT STICK: turn / adjust reach\nRelease grip + A: confirm  •  Hold X 2 seconds: restart");
                _text.text = $"<b>{title}</b>\n{status}\n{arrow}\n{controls}";
            }
            else if (area.State == MRMeshPlayableArea.AreaState.TrackingUnavailable)
            {
                string msg = GetLocalized("Area_Paused_TrackingOrPT", "Site experience paused\nTracking or passthrough is unavailable. Pause here and look around.\nHold X for 2 seconds at the entrance to realign if needed.");
                _text.text = FormatWithBoldFirstLine(msg);
            }
            else if (area.State == MRMeshPlayableArea.AreaState.Outside)
            {
                string msg = GetLocalized("Area_Outside_Walking", "Outside the walking area\nThe site experience is paused. Return to the marked hallways when the physical route is clear. Keep out of the central ruin.\nHold X for 2 seconds at the entrance to realign.");
                _text.text = FormatWithBoldFirstLine(msg);
            }
            else
            {
                string msg = GetLocalized("Area_Near_Boundary", "Approaching the boundary\nKeep clear of the edge of the playable area.");
                _text.text = FormatWithBoldFirstLine(msg);
            }
        }

        private static string GetLocalized(string key, string fallback)
        {
            try
            {
                string val = LocalizationSettings.StringDatabase.GetLocalizedString("MR_UI", key);
                return string.IsNullOrEmpty(val) ? fallback : val;
            }
            catch
            {
                return fallback;
            }
        }

        private static string FormatWithBoldFirstLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (text.StartsWith("<b>")) return text;
            int newlineIndex = text.IndexOf('\n');
            if (newlineIndex > 0)
            {
                return "<b>" + text.Substring(0, newlineIndex) + "</b>\n" + text.Substring(newlineIndex + 1);
            }
            return "<b>" + text + "</b>";
        }

        private void OnDisable()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_referenceOutline != null) _referenceOutline.enabled = false;
            if (_headingArrow != null) _headingArrow.enabled = false;
            if (_boundaryOutline != null) _boundaryOutline.enabled = false;
            if (_ruinOutline != null) _ruinOutline.enabled = false;
            if (_floorEdges != null) foreach (var line in _floorEdges) if (line != null) line.enabled = false;
        }

        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        }
    }
}
