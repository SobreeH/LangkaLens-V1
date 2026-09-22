using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        private readonly int[] _boxEdges = { 0, 1, 2, 3, 0, 4, 5, 1, 5, 6, 2, 6, 7, 3, 7, 4 };
        private readonly Vector3[] _boxCorners = new Vector3[8];
        private Vector3[] _perimeter;
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
            _perimeter = new Vector3[area.outline.Length];
            _boundaryOutline.positionCount = _perimeter.Length;

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
            _boundaryOutline.enabled = alignment.IsTrackingValid && (alwaysShowBoundary || area.State == MRMeshPlayableArea.AreaState.NearBoundary || area.State == MRMeshPlayableArea.AreaState.Outside);
            if (_boundaryOutline.enabled)
            {
                for (int i = 0; i < _perimeter.Length; i++)
                    _perimeter[i] = area.transform.TransformPoint(new Vector3(area.outline[i].x, boundaryDisplayHeight, area.outline[i].y));
                _boundaryOutline.SetPositions(_perimeter);
                Color color = area.CanPlay ? new Color(1, 0.65f, 0.1f) : new Color(1, 0.15f, 0.1f);
                _boundaryOutline.startColor = _boundaryOutline.endColor = color;
            }

            _panel.SetActive(area.State != MRMeshPlayableArea.AreaState.Inside);
            if (area.State == MRMeshPlayableArea.AreaState.Unconfigured)
                _text.text = "<b>Site setup needed</b>\n" + area.ConfigurationError;
            else if (!alignment.HeadTracked)
                _text.text = "<b>Tracking unavailable</b>\nPause here and look around. The site experience is paused.";
            else if (settingUp)
                _text.text = "<b>Align the entrance sign/table</b>\n" + alignment.Status +
                    "\nPoint the green arrow into the site.\nRIGHT GRIP: move  •  RIGHT STICK: turn / adjust reach\nRelease grip + A: confirm  •  Hold X 2 seconds: restart";
            else if (area.State == MRMeshPlayableArea.AreaState.TrackingUnavailable)
                _text.text = "<b>Site experience paused</b>\nTracking or passthrough is unavailable. Pause here and look around.\nHold X for 2 seconds at the entrance to realign if needed.";
            else if (area.State == MRMeshPlayableArea.AreaState.Outside)
                _text.text = "<b>Outside the playable interior</b>\nThe site experience is paused. Enter the enclosed area when the physical route is clear.\nHold X for 2 seconds at the entrance to realign.";
            else
                _text.text = "<b>Approaching the boundary</b>\nKeep clear of the edge of the playable area.";
        }

        private void OnDisable()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_referenceOutline != null) _referenceOutline.enabled = false;
            if (_headingArrow != null) _headingArrow.enabled = false;
            if (_boundaryOutline != null) _boundaryOutline.enabled = false;
        }

        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        }
    }
}
