using UnityEngine;
using UnityEngine.UI;

namespace LangkaLens.MR
{
    [DisallowMultipleComponent]
    public class MRMainMenuConfirmation : MonoBehaviour
    {
        public Transform head;
        public GameObject popup;
        public Button homeButton;
        [Tooltip("Optional: in a tour scene, only allow exiting from the tour's default menu.")]
        public TourManager tourManager;
        [Tooltip("Optional: in a museum scene, only allow exiting when inspection is inactive.")]
        public InspectionManager inspectionManager;
        [Min(0.5f)] public float popupDistance = 1.5f;
        [Min(1f)] public float confirmationSeconds = 6f;

        // Short taps coexist with the alignment system's two-second X hold.
        private const float MaximumTapSeconds = 0.5f;
        private bool _wasHeld;
        private bool _waitForRelease = true;
        private bool _exiting;
        private float _pressedAt;
        private float _dismissAt;

        public bool IsShowing => popup != null && popup.activeSelf;

        private void Start()
        {
            Dismiss();
            if (head == null || popup == null || homeButton == null)
            {
                Debug.LogError("Main menu confirmation is missing its head, popup, or Home button reference.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            ProcessInput(OVRInput.Get(OVRInput.RawButton.X, OVRInput.Controller.LTouch),
                MRMeshReferenceAlignment.HeadIsTracked() &&
                OVRInput.IsControllerConnected(OVRInput.Controller.LTouch), Time.unscaledTime);
        }

        // Explicit samples also allow checking taps, holds, and focus loss without a headset.
        public void ProcessInput(bool held, bool inputAvailable, float now)
        {
            if (_exiting) return;
            inputAvailable &= tourManager == null || (tourManager.isActiveAndEnabled && tourManager.IsAtMainMenu);
            inputAvailable &= inspectionManager == null ||
                              (inspectionManager.isActiveAndEnabled && inspectionManager.CanExitToMainMenu);
            if (!inputAvailable)
            {
                Dismiss();
                _wasHeld = false;
                _waitForRelease = true;
                return;
            }
            if (IsShowing && now >= _dismissAt) Dismiss();
            if (_waitForRelease)
            {
                _waitForRelease = held;
                return;
            }
            if (held && !_wasHeld) _pressedAt = now;
            if (held && now - _pressedAt >= MaximumTapSeconds) Dismiss();
            if (!held && _wasHeld && now - _pressedAt < MaximumTapSeconds)
            {
                if (IsShowing)
                {
                    Dismiss();
                    _exiting = true;
                    // Use exactly the tablet's action, even while the tablet is hidden.
                    homeButton.onClick.Invoke();
                }
                else if (head != null && popup != null && homeButton != null)
                {
                    PositionPopup();
                    _dismissAt = now + confirmationSeconds;
                    popup.SetActive(true);
                }
            }
            _wasHeld = held;
        }

        private void LateUpdate()
        {
            if (IsShowing && head != null) PositionPopup();
        }

        private void PositionPopup()
        {
            popup.transform.SetPositionAndRotation(head.position + head.forward * popupDistance, head.rotation);
        }

        private void Dismiss()
        {
            if (popup != null) popup.SetActive(false);
        }

        private void OnDisable()
        {
            Dismiss();
            _wasHeld = false;
            _waitForRelease = true;
            _exiting = false;
        }
    }
}
