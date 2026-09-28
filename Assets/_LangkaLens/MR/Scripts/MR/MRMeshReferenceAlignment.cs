using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace LangkaLens.MR
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public class MRMeshReferenceAlignment : MonoBehaviour
    {
        [Header("Entrance sign/table in the authored mesh")]
        public MeshFilter meshReference;
        public Transform rightController;
        public Transform head;
        [Range(0.15f, 3)] public float placementReach = 0.6f;
        [Min(1)] public float rotationSpeed = 45;
        [Tooltip("Hide imported scan surfaces/helpers during MR; the sign outline remains visible during setup.")]
        public bool hideScanAtRuntime = true;

        public bool IsConfirmed { get; private set; }
        public bool IsBusy { get; private set; }
        public bool HasPlacedPreview { get; private set; }
        public bool HeadTracked => HeadIsTracked();
        public bool IsTrackingValid => isActiveAndEnabled && IsConfirmed && _anchor != null && _anchor.Created && _anchor.Localized &&
                                       _anchor.IsTracked && HeadIsTracked();
        public string Status { get; private set; } = "Align the outline with the entrance sign/table.";
        public string StatusKey { get; private set; } = "Align_Status_Default";

        private OVRSpatialAnchor _anchor;
        private GameObject _anchorObject;
        private Vector3 _rootInAnchor;
        private Quaternion _rotationInAnchor;
        private Vector3 _referenceCentreInRoot;
        private Quaternion _authoredRotation;
        private float _yaw;
        private float _resetHeld;
        private bool _resetLatched;
        private Renderer[] _scanRenderers;
        private bool[] _rendererStates;
        private Collider[] _scanColliders;
        private bool[] _colliderStates;

        private void Awake()
        {
            if (meshReference == null || meshReference.sharedMesh == null || !meshReference.transform.IsChildOf(transform) ||
                rightController == null || head == null || Vector3.Distance(transform.lossyScale, Vector3.one) > 0.001f)
            {
                Status = "Entrance alignment is missing references or the site is not at metre scale.";
                StatusKey = "Align_Status_MissingRefs";
                enabled = false;
                return;
            }
            _referenceCentreInRoot = transform.InverseTransformPoint(meshReference.transform.TransformPoint(meshReference.sharedMesh.bounds.center));
            _authoredRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            _scanRenderers = GetComponentsInChildren<Renderer>(true);
            _rendererStates = new bool[_scanRenderers.Length];
            for (int i = 0; i < _scanRenderers.Length; i++)
            {
                _rendererStates[i] = _scanRenderers[i].enabled;
                if (hideScanAtRuntime) _scanRenderers[i].enabled = false;
            }
            _scanColliders = GetComponentsInChildren<Collider>(true);
            _colliderStates = new bool[_scanColliders.Length];
            for (int i = 0; i < _scanColliders.Length; i++)
            {
                _colliderStates[i] = _scanColliders[i].enabled;
                _scanColliders[i].enabled = false;
            }
        }

        private void Update()
        {
            bool reset = HeadIsTracked() && OVRInput.Get(OVRInput.RawButton.X, OVRInput.Controller.LTouch);
            _resetHeld = reset ? _resetHeld + Time.unscaledDeltaTime : 0;
            if (!reset) _resetLatched = false;
            if (_resetHeld >= 2 && !_resetLatched)
            {
                _resetLatched = true;
                BeginAlignment();
            }
            if (IsConfirmed || IsBusy || !HeadIsTracked() || !ControllerIsTracked()) return;

            Vector2 stick = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick, OVRInput.Controller.RTouch);
            if (OVRInput.Get(OVRInput.RawButton.RHandTrigger, OVRInput.Controller.RTouch))
            {
                if (Mathf.Abs(stick.x) > 0.15f) _yaw += stick.x * rotationSpeed * Time.unscaledDeltaTime;
                if (Mathf.Abs(stick.y) > 0.15f) placementReach = Mathf.Clamp(placementReach + stick.y * Time.unscaledDeltaTime * 0.6f, 0.15f, 3);
                Quaternion rotation = Quaternion.AngleAxis(_yaw, Vector3.up) * _authoredRotation;
                Vector3 centre = rightController.position + rightController.forward * placementReach;
                SetReferencePose(centre, rotation);
                HasPlacedPreview = true;
                Status = "Match all edges and the height of the real sign/table. Release grip, then press A.";
                StatusKey = "Align_Status_Adjusting";
            }
            if (HasPlacedPreview && !OVRInput.Get(OVRInput.RawButton.RHandTrigger, OVRInput.Controller.RTouch) && OVRInput.GetDown(OVRInput.RawButton.A, OVRInput.Controller.RTouch))
                StartCoroutine(ConfirmAlignment());
        }

        // Move the site; never move/clamp the headset's tracked pose.
        public void SetReferencePose(Vector3 referenceCentre, Quaternion rootRotation)
        {
            transform.SetPositionAndRotation(referenceCentre - rootRotation * _referenceCentreInRoot, rootRotation);
        }

        public void BeginAlignment()
        {
            StopAllCoroutines();
            IsConfirmed = false;
            IsBusy = false;
            HasPlacedPreview = false;
            if (_anchorObject != null) Destroy(_anchorObject);
            _anchorObject = null;
            _anchor = null;
            Status = "Hold RIGHT GRIP to place the sign/table outline.";
            StatusKey = "Align_Status_HoldGrip";
        }

        private IEnumerator ConfirmAlignment()
        {
            IsBusy = true;
            Status = "Locking the entrance reference. Keep looking around the sign/table.";
            StatusKey = "Align_Status_Locking";
            Vector3 rootPosition = transform.position;
            Quaternion rootRotation = transform.rotation;
            _anchorObject = new GameObject("Ban Jalae 3 entrance anchor (session)");
            _anchorObject.transform.SetPositionAndRotation(transform.TransformPoint(_referenceCentreInRoot), rootRotation);
            _anchor = _anchorObject.AddComponent<OVRSpatialAnchor>();
            float deadline = Time.realtimeSinceStartup + 15;
            while (_anchor != null && (!_anchor.Created || !_anchor.Localized || !_anchor.IsTracked) && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (_anchor == null || !_anchor.Created || !_anchor.Localized || !_anchor.IsTracked || !HeadIsTracked())
            {
                BeginAlignment();
                Status = "Could not track the entrance anchor. Look around, then align and press A again.";
                StatusKey = "Align_Status_TrackFailed";
                yield break;
            }
            _rootInAnchor = _anchor.transform.InverseTransformPoint(rootPosition);
            _rotationInAnchor = Quaternion.Inverse(_anchor.transform.rotation) * rootRotation;
            IsBusy = false;
            IsConfirmed = true;
            Status = "Entrance aligned. Hold X for two seconds to realign.";
            StatusKey = "Align_Status_Confirmed";
        }

        private void LateUpdate()
        {
            if (IsTrackingValid)
                transform.SetPositionAndRotation(_anchor.transform.TransformPoint(_rootInAnchor), _anchor.transform.rotation * _rotationInAnchor);
        }

        private bool ControllerIsTracked() => OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) &&
                                              OVRInput.GetControllerOrientationTracked(OVRInput.Controller.RTouch);

        public static bool HeadIsTracked()
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked &&
                   device.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state) &&
                   (state & (InputTrackingState.Position | InputTrackingState.Rotation)) == (InputTrackingState.Position | InputTrackingState.Rotation) &&
                   OVRManager.hasVrFocus;
        }

        private void OnDisable()
        {
            BeginAlignment();
            if (_scanRenderers != null) for (int i = 0; i < _scanRenderers.Length; i++)
                if (_scanRenderers[i] != null) _scanRenderers[i].enabled = _rendererStates[i];
            if (_scanColliders != null) for (int i = 0; i < _scanColliders.Length; i++)
                if (_scanColliders[i] != null) _scanColliders[i].enabled = _colliderStates[i];
        }
    }
}
