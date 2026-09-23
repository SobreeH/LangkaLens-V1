using TMPro;
using UnityEngine;

namespace LangkaLens.MR
{
    [DefaultExecutionOrder(250)]
    public class MRFoundationPreview : MonoBehaviour
    {
        public MRFourPointCalibration calibration;
        public Material ghostMaterial;
        public Material lineMaterial;
        public TMP_FontAsset font;
        private GameObject _preview, _visuals;
        private readonly LineRenderer[] _marks = new LineRenderer[5];
        private readonly LineRenderer[] _errors = new LineRenderer[4];
        private readonly TMP_Text[] _labels = new TMP_Text[4];

        private void Start()
        {
            if (calibration == null || calibration.modelPrefab == null || calibration.contentRoot == null ||
                ghostMaterial == null || lineMaterial == null) { enabled = false; return; }
            _preview = Instantiate(calibration.modelPrefab, calibration.contentRoot, false);
            _preview.name = "BJ3 alignment preview";
            _preview.transform.localPosition = Vector3.zero;
            _preview.transform.localRotation = Quaternion.identity;
            _preview.transform.localScale = Vector3.one;
            foreach (var c in _preview.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var b in _preview.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
            foreach (var r in _preview.GetComponentsInChildren<Renderer>(true))
            {
                var materials = r.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = ghostMaterial;
                r.sharedMaterials = materials;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            _preview.SetActive(false);
            // Unscaled markers stay readable regardless of the reconstruction's fitted scale.
            _visuals = new GameObject("Foundation setup markers");
            for (int i = 0; i < 5; i++) _marks[i] = MakeLine("Corner " + i, 6, 0.012f);
            for (int i = 0; i < 4; i++)
            {
                _errors[i] = MakeLine("Corner mismatch " + i, 2, 0.012f);
                var go = new GameObject("Label " + (char)('A' + i));
                go.transform.SetParent(_visuals.transform, false);
                var label = go.AddComponent<TextMeshPro>();
                if (font != null) label.font = font;
                label.text = ((char)('A' + i)).ToString();
                label.fontSize = 5; label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.sizeDelta = new Vector2(0.5f, 0.3f);
                _labels[i] = label;
            }
        }

        private LineRenderer MakeLine(string name, int count, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_visuals.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial; line.useWorldSpace = true;
            line.positionCount = count; line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }

        private void LateUpdate()
        {
            if (_preview == null) return;
            bool setup = !calibration.IsConfirmed && calibration.EnvironmentReady;
            bool reviewing = setup && calibration.Stage == MRFourPointCalibration.SetupStage.Review && calibration.HasSolution;
            _preview.SetActive(reviewing);
            _visuals.SetActive(setup);
            if (!setup) return;
            for (int i = 0; i < 4; i++)
            {
                bool captured = calibration.CornerTracked(i);
                _marks[i].enabled = captured; _labels[i].gameObject.SetActive(captured);
                _errors[i].enabled = captured && reviewing;
                if (!captured) continue;
                Vector3 point = calibration.CornerPosition(i);
                Color color = i == calibration.SelectedCorner ? Color.yellow : Color.cyan;
                Cross(_marks[i], point, color);
                _labels[i].color = color;
                _labels[i].transform.position = point + Vector3.up * 0.22f;
                _labels[i].transform.rotation = Quaternion.LookRotation(_labels[i].transform.position - calibration.alignment.head.position);
                if (reviewing)
                {
                    _errors[i].SetPosition(0, point);
                    _errors[i].SetPosition(1, calibration.contentRoot.TransformPoint(calibration.ReferencePoints[i]));
                    _errors[i].startColor = _errors[i].endColor = new Color(1, 0.65f, 0.1f);
                }
            }
            _marks[4].enabled = calibration.Stage == MRFourPointCalibration.SetupStage.Capture && calibration.HasCursor;
            if (_marks[4].enabled) Cross(_marks[4], calibration.Cursor, Color.green);
        }

        private static void Cross(LineRenderer line, Vector3 p, Color color)
        {
            const float size = 0.1f;
            line.SetPosition(0, p - Vector3.right * size); line.SetPosition(1, p + Vector3.right * size);
            line.SetPosition(2, p); line.SetPosition(3, p - Vector3.forward * size);
            line.SetPosition(4, p + Vector3.forward * size); line.SetPosition(5, p);
            line.startColor = line.endColor = color;
        }

        private void OnDisable()
        {
            if (_preview != null) _preview.SetActive(false);
            if (_visuals != null) _visuals.SetActive(false);
        }
        private void OnDestroy()
        {
            if (_preview != null) Destroy(_preview);
            if (_visuals != null) Destroy(_visuals);
        }
    }
}
