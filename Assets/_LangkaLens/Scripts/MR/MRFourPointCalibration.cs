using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace LangkaLens.MR
{
    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    public class MRFourPointCalibration : MonoBehaviour
    {
        public enum SetupStage { Capture, Review, Locked }

        [Header("BJ3 reference, in model coordinates")]
        public GameObject modelPrefab;
        public Transform referenceModel;
        [Tooltip("Four approximate base corners, ordered A, B, C, D around the perimeter.")]
        public Transform[] modelCorners = new Transform[4];
        public Transform contentRoot;
        public MRMeshReferenceAlignment alignment;
        public MRMeshPlayableArea area;

        [Header("Placement")]
        [Range(0.15f, 15)] public float reach = 2;
        public bool estimateScale = true;
        [Min(0.01f)] public float authoredScale = 14;
        [Tooltip("Advisory only. Small footprint differences do not prevent confirmation.")]
        public float mismatchWarningMetres = 0.75f;
        [Tooltip("Additional disagreement after locking before content is paused.")]
        public float trackingErrorAllowance = 0.4f;
        [Tooltip("Sudden anchor movement requires the operator to review the placement.")]
        public float trackingJumpMetres = 0.75f;
        public float trackingJumpDegrees = 5;

        public SetupStage Stage { get; private set; }
        public bool IsConfirmed => isActiveAndEnabled && Stage == SetupStage.Locked;
        public bool IsBusy { get; private set; }
        public bool IsTrackingValid { get; private set; }
        public bool HasSolution { get; private set; }
        public bool HasCursor { get; private set; }
        public bool NeedsReview { get; private set; }
        public int SelectedCorner { get; private set; }
        public Vector3 Cursor { get; private set; }
        public MRFoundationFit.Solution Solution { get; private set; }
        public float[] CornerErrors { get; } = new float[4];
        public Vector3[] ReferencePoints { get; } = new Vector3[4];
        public Vector3[] ObservedPoints { get; } = new Vector3[4];
        public Vector3 PreviewOffset { get; private set; }
        public float PreviewYaw { get; private set; }
        public int CapturedCount
        {
            get { int count = 0; for (int i = 0; i < 4; i++) if (_anchors[i] != null) count++; return count; }
        }
        public bool EnvironmentReady => alignment != null && alignment.HeadTracked && area != null &&
            area.passthrough != null && area.passthrough.isActiveAndEnabled && !area.passthrough.hidden &&
            area.passthrough.textureOpacity >= 0.95f && OVRManager.instance != null &&
            OVRManager.instance.isInsightPassthroughEnabled && OVRManager.IsInsightPassthroughInitialized();

        private readonly OVRSpatialAnchor[] _anchors = new OVRSpatialAnchor[4];
        private readonly Vector3[] _anchorOffsets = new Vector3[4];
        private float _resetHeld, _stableTime, _lockedScale, _lockedError;
        private bool _resetLatched, _configured;
        private string _messageEnglish, _messageThai;

        private void Start()
        {
            _configured = ValidateConfiguration();
            if (referenceModel != null) referenceModel.gameObject.SetActive(false);
            Restart();
        }

        public bool ValidateConfiguration()
        {
            if (alignment == null || alignment.head == null || alignment.rightController == null || area == null ||
                !area.IsUnrestricted || modelPrefab == null || referenceModel == null || contentRoot == null ||
                modelCorners == null || modelCorners.Length != 4 ||
                (contentRoot.parent != null && Vector3.Distance(contentRoot.parent.lossyScale, Vector3.one) > 0.001f)) return false;
            for (int i = 0; i < 4; i++)
            {
                if (modelCorners[i] == null || !modelCorners[i].IsChildOf(referenceModel)) return false;
                ReferencePoints[i] = referenceModel.InverseTransformPoint(modelCorners[i].position);
            }
            return MRFoundationFit.TrySolve(ReferencePoints, ReferencePoints, false, 1, out _);
        }

        public bool CornerTracked(int index) => index >= 0 && index < 4 && _anchors[index] != null &&
            _anchors[index].Created && _anchors[index].Localized && _anchors[index].IsTracked;

        public Vector3 CornerPosition(int index) => CornerTracked(index)
            ? _anchors[index].transform.TransformPoint(_anchorOffsets[index]) : ObservedPoints[index];

        public void Restart()
        {
            StopAllCoroutines();
            for (int i = 0; i < 4; i++) RemoveAnchor(i);
            Stage = SetupStage.Capture;
            IsBusy = IsTrackingValid = HasSolution = HasCursor = NeedsReview = false;
            SelectedCorner = 0;
            PreviewOffset = Vector3.zero; PreviewYaw = 0;
            _stableTime = 0;
            ClearMessage();
        }

        private void Update()
        {
            if (!_configured) return;
            bool reset = alignment.HeadTracked && OVRInput.Get(OVRInput.RawButton.X);
            _resetHeld = reset ? _resetHeld + Time.unscaledDeltaTime : 0;
            if (!reset) _resetLatched = false;
            if (_resetHeld >= 2 && !_resetLatched) { Restart(); _resetLatched = true; }
            if (!EnvironmentReady || IsBusy || !OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) ||
                !OVRInput.GetControllerOrientationTracked(OVRInput.Controller.RTouch)) return;

            if (Stage == SetupStage.Locked)
            {
                if (NeedsReview && OVRInput.GetDown(OVRInput.RawButton.A))
                { Stage = SetupStage.Review; NeedsReview = false; IsTrackingValid = false; ClearMessage(); }
                return;
            }

            Vector2 stick = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
            bool grip = OVRInput.Get(OVRInput.RawButton.RHandTrigger);
            bool trigger = OVRInput.Get(OVRInput.RawButton.RIndexTrigger);
            if (Stage == SetupStage.Capture)
            {
                if (grip)
                {
                    if (Mathf.Abs(stick.y) > 0.2f) reach = Mathf.Clamp(reach + stick.y * Time.unscaledDeltaTime * 1.5f, 0.15f, 15);
                    Cursor = alignment.rightController.position + alignment.rightController.forward * reach;
                    HasCursor = true;
                }
                else if (HasCursor)
                {
                    Vector3 right = Vector3.ProjectOnPlane(alignment.head.right, Vector3.up).normalized;
                    Vector3 forward = Vector3.ProjectOnPlane(alignment.head.forward, Vector3.up).normalized;
                    if (stick.magnitude > 0.2f)
                        Cursor += (right * stick.x + (trigger ? Vector3.up : forward) * stick.y) * Time.unscaledDeltaTime * 0.2f;
                }
                if (!grip && HasCursor && OVRInput.GetDown(OVRInput.RawButton.A)) StartCoroutine(CaptureCorner());
                else if (OVRInput.GetDown(OVRInput.RawButton.B))
                {
                    int previous = (SelectedCorner + 3) % 4;
                    if (_anchors[previous] != null) EditCorner(previous);
                }
            }
            else
            {
                if (OVRInput.GetDown(OVRInput.RawButton.Y)) SelectedCorner = (SelectedCorner + 1) % 4;
                if (OVRInput.GetDown(OVRInput.RawButton.B)) { EditCorner(SelectedCorner); return; }
                // Preview adjustments use site heading, so they remain meaningful after anchor updates.
                if (stick.magnitude > 0.2f)
                {
                    if (trigger) { PreviewOffset += Vector3.up * stick.y * Time.unscaledDeltaTime * 0.2f; PreviewYaw += stick.x * Time.unscaledDeltaTime * 5; }
                    else PreviewOffset += new Vector3(stick.x, 0, stick.y) * Time.unscaledDeltaTime * 0.2f;
                }
                if (OVRInput.GetDown(OVRInput.RawButton.A) && HasSolution)
                {
                    Stage = SetupStage.Locked;
                    _lockedScale = Solution.scale;
                    _lockedError = Solution.maxError;
                    _stableTime = 0;
                    ClearMessage();
                }
            }
        }

        public void EditCorner(int index)
        {
            if (index < 0 || index > 3 || IsBusy) return;
            HasCursor = CornerTracked(index);
            if (HasCursor) Cursor = _anchors[index].transform.TransformPoint(_anchorOffsets[index]);
            RemoveAnchor(index);
            SelectedCorner = index; Stage = SetupStage.Capture;
            IsTrackingValid = HasSolution = NeedsReview = false;
            ClearMessage();
        }

        private IEnumerator CaptureCorner()
        {
            IsBusy = true;
            int index = SelectedCorner;
            Vector3 captured = Cursor;
            RemoveAnchor(index);
            var go = new GameObject("BJ3 corner " + (char)('A' + index) + " anchor (session)");
            go.transform.position = captured;
            _anchors[index] = go.AddComponent<OVRSpatialAnchor>();
            float deadline = Time.realtimeSinceStartup + 15;
            while (!CornerTracked(index) && Time.realtimeSinceStartup < deadline && EnvironmentReady) yield return null;
            if (!CornerTracked(index) || !EnvironmentReady)
            {
                RemoveAnchor(index); IsBusy = false;
                Message("Could not lock this corner. Look around and press A to retry.", "บันทึกจุดไม่ได้ มองรอบ ๆ แล้วกด A เพื่อลองอีกครั้ง");
                yield break;
            }
            // Preserve the chosen point even if anchor creation refines its initial pose.
            _anchorOffsets[index] = _anchors[index].transform.InverseTransformPoint(captured);
            IsBusy = false; HasCursor = false; ClearMessage();
            if (CapturedCount == 4) { Stage = SetupStage.Review; SelectedCorner = 0; }
            else for (int n = 1; n <= 4; n++)
            {
                int next = (index + n) % 4;
                if (_anchors[next] == null) { SelectedCorner = next; break; }
            }
        }

        private void LateUpdate()
        {
            IsTrackingValid = false; HasSolution = false;
            if (!_configured || !EnvironmentReady || CapturedCount != 4) { _stableTime = 0; return; }
            for (int i = 0; i < 4; i++)
            {
                if (!CornerTracked(i)) { _stableTime = 0; return; }
                ObservedPoints[i] = _anchors[i].transform.TransformPoint(_anchorOffsets[i]);
            }
            if (!MRFoundationFit.TrySolve(ReferencePoints, ObservedPoints, Stage != SetupStage.Locked && estimateScale,
                Stage == SetupStage.Locked ? _lockedScale : authoredScale, out var solution))
            { _stableTime = 0; return; }
            HasSolution = true;
            for (int i = 0; i < 4; i++) CornerErrors[i] = Vector3.Distance(solution.TransformPoint(ReferencePoints[i]), ObservedPoints[i]);
            if (Stage == SetupStage.Locked &&
                (solution.maxError > _lockedError + trackingErrorAllowance ||
                Vector3.Distance(solution.position, Solution.position) > trackingJumpMetres ||
                Quaternion.Angle(solution.rotation, Solution.rotation) > trackingJumpDegrees)) NeedsReview = true;
            if (NeedsReview) { _stableTime = 0; return; }
            Solution = solution;
            GetDisplayPose(out Vector3 position, out Quaternion rotation);
            if (Stage == SetupStage.Locked && _stableTime >= 0.5f)
            {
                float blend = 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime);
                contentRoot.SetPositionAndRotation(Vector3.Lerp(contentRoot.position, position, blend),
                    Quaternion.Slerp(contentRoot.rotation, rotation, blend));
            }
            else contentRoot.SetPositionAndRotation(position, rotation);
            contentRoot.localScale = Vector3.one * solution.scale;
            if (Stage == SetupStage.Locked)
            {
                _stableTime += Time.unscaledDeltaTime;
                IsTrackingValid = _stableTime >= 0.5f;
            }
        }

        public void GetDisplayPose(out Vector3 position, out Quaternion rotation)
        {
            Vector3 centre = Vector3.zero;
            for (int i = 0; i < 4; i++) centre += ReferencePoints[i] * 0.25f;
            rotation = Solution.rotation * Quaternion.Euler(0, PreviewYaw, 0);
            position = Solution.TransformPoint(centre) + Solution.rotation * PreviewOffset - rotation * (centre * Solution.scale);
        }

        public string Instructions(bool thai)
        {
            string T(string en, string th) => thai ? th : en;
            if (!_configured) return T("Foundation setup needs its model, four markers, head, controller and unrestricted area references.", "ตรวจสอบโมเดล จุดอ้างอิง 4 จุด กล้อง ตัวควบคุม และพื้นที่แบบไม่จำกัด");
            if (!EnvironmentReady) return T("Tracking or passthrough unavailable. Pause and look around.", "ไม่พบการติดตามหรือภาพภายนอก หยุดและมองรอบ ๆ");
            if (NeedsReview) return T("Placement tracking changed. A: review placement | Hold X 2 seconds: restart", "ตำแหน่งติดตามเปลี่ยน กด A ตรวจสอบ | กด X ค้าง 2 วินาทีเพื่อเริ่มใหม่");
            if (IsBusy) return T("Locking corner — keep looking around it...", "กำลังบันทึกจุด — มองบริเวณรอบจุด...");
            string message = thai ? _messageThai : _messageEnglish;
            if (!string.IsNullOrEmpty(message)) message += "\n";
            if (Stage == SetupStage.Capture) return message + CaptureInstructions(thai);
            if (!HasSolution) return T("Waiting for all four corners to track, or corner order is invalid.\nY: select corner | B: replace selected corner | Hold X: restart",
                "รอติดตามครบ 4 จุด หรือลำดับมุมไม่ถูกต้อง\nY: เลือกมุม | B: วางมุมใหม่ | X ค้าง: เริ่มใหม่");
            if (Stage == SetupStage.Locked) return T("Placement locked. Hold X 2 seconds to set up again.", "ล็อกตำแหน่งแล้ว กด X ค้าง 2 วินาทีเพื่อตั้งค่าใหม่");
            string errors = $"A {CornerErrors[0] * 100:F0}   B {CornerErrors[1] * 100:F0}   C {CornerErrors[2] * 100:F0}   D {CornerErrors[3] * 100:F0} cm";
            return T("<b>Check the approximate placement</b>\n", "<b>ตรวจสอบตำแหน่งโดยประมาณ</b>\n") + errors + "\n" +
                (Solution.maxError > mismatchWarningMetres ? T("The footprints differ. Confirm if the preview looks suitable.\n", "ขนาดฐานต่างกัน ยืนยันได้หากตำแหน่งเหมาะสม\n") : T("Small corner differences are OK.\n", "ความต่างเล็กน้อยของมุมยอมรับได้\n")) +
                T($"STICK: shift | TRIGGER + STICK: height / turn\nA: lock | Y: select corner ({(char)('A' + SelectedCorner)}) | B: replace it\nHold X 2 seconds: restart",
                  $"STICK: ขยับ | TRIGGER + STICK: สูงต่ำ / หมุน\nA: ล็อก | Y: เลือกมุม ({(char)('A' + SelectedCorner)}) | B: วางใหม่\nX ค้าง 2 วินาที: เริ่มใหม่");
        }

        public string CaptureInstructions(bool thai)
        {
            string T(string en, string th) => thai ? th : en;
            return T($"<b>Base corner {(char)('A' + SelectedCorner)} | {CapturedCount}/4 captured</b>\nA: near-left > B: near-right > C: far-right > D: far-left\nChoose roughly the same base height at every corner.\nHold RIGHT GRIP: aim | STICK up/down: reach\nRelease grip: STICK fine move | hold TRIGGER: height\nA: capture | B: undo | Hold X 2 seconds: restart",
                    $"<b>มุมฐาน {(char)('A' + SelectedCorner)} | บันทึก {CapturedCount}/4 จุด</b>\nA: ใกล้ซ้าย > B: ใกล้ขวา > C: ไกลขวา > D: ไกลซ้าย\nเลือกจุดที่ระดับฐานใกล้เคียงกันทุกมุม\nกด GRIP ขวาค้างเพื่อเล็ง | STICK ขึ้น/ลงปรับระยะ\nปล่อย GRIP: STICK ขยับละเอียด | กด TRIGGER ค้างปรับสูงต่ำ\nA: บันทึก | B: ย้อนกลับ | X ค้าง 2 วินาที: เริ่มใหม่");
        }

        public static bool IsThai => LocalizationSettings.SelectedLocale != null &&
            LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("th");
        private void Message(string english, string thai) { _messageEnglish = english; _messageThai = thai; }
        private void ClearMessage() { _messageEnglish = _messageThai = null; }
        private void RemoveAnchor(int i)
        {
            if (_anchors[i] != null) Destroy(_anchors[i].gameObject);
            _anchors[i] = null;
        }
        private void OnDisable() { Restart(); }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (referenceModel == null || modelCorners == null || modelCorners.Length != 4) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < 4; i++)
            {
                if (modelCorners[i] == null) continue;
                Vector3 point = modelCorners[i].position;
                Gizmos.DrawWireSphere(point, 0.12f);
                UnityEditor.Handles.Label(point + Vector3.up * 0.2f, "Base " + (char)('A' + i));
                if (modelCorners[(i + 1) % 4] != null) Gizmos.DrawLine(point, modelCorners[(i + 1) % 4].position);
            }
        }
#endif
    }
}
