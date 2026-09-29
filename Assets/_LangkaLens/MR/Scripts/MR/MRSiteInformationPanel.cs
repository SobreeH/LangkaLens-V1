using TMPro;
using UnityEngine;

namespace LangkaLens.MR
{
    // Run after the spawner has applied alignment/tracking visibility for this frame.
    [DefaultExecutionOrder(500)]
    public class MRSiteInformationPanel : MonoBehaviour
    {
        public MRModelSpawner spawner;
        public Transform head;
        public GameObject view;
        public GameObject[] pages;
        public TMP_Text pageNumber;

        [Tooltip("Metres to the right, below and ahead of the viewer when the panel appears.")]
        public Vector3 viewerOffset = new Vector3(1.65f, -0.12f, 1.35f);
        [Tooltip("Keep the panel still while looking at it; reorient only after a large turn away.")]
        [Range(100, 170)] public float reorientAngle = 120;

        private int _page;
        private bool _requestedVisible;
        private bool _visible;
        private Quaternion _heading;

        private void Awake()
        {
            if (spawner == null || head == null || view == null || pages == null || pages.Length == 0)
            {
                Debug.LogError("Site information needs its spawner, head, view and pages.", this);
                enabled = false;
                return;
            }
            ShowPage(0);
            view.SetActive(false);
        }

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.RawButton.Y, OVRInput.Controller.LTouch))
                ToggleVisibility();
        }

        public void ToggleVisibility() => _requestedVisible = !_requestedVisible;

        private void LateUpdate()
        {
            // Keep listening on this root while only the canvas view is hidden.
            bool visible = _requestedVisible && spawner != null && head != null && spawner.IsModelVisible;
            if (!visible)
            {
                if (_visible) view.SetActive(false);
                _visible = false;
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f) forward = _visible ? _heading * Vector3.forward : Vector3.forward;
            if (!_visible || Vector3.Angle(_heading * Vector3.forward, forward) > reorientAngle)
                _heading = Quaternion.LookRotation(forward, Vector3.up);

            // Follow translation, but not small head turns: the user can turn to read the panel.
            Vector3 position = head.position + _heading * viewerOffset;
            Vector3 facing = Vector3.ProjectOnPlane(position - head.position, Vector3.up);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(facing, Vector3.up));
            if (!_visible) view.SetActive(true);
            _visible = true;
        }

        public void NextPage() => ShowPage(_page + 1);
        public void PreviousPage() => ShowPage(_page - 1);

        private void ShowPage(int index)
        {
            if (pages == null || pages.Length == 0) return;
            _page = (index + pages.Length) % pages.Length;
            for (int i = 0; i < pages.Length; i++)
                if (pages[i] != null) pages[i].SetActive(i == _page);
            if (pageNumber != null) pageNumber.text = $"{_page + 1} / {pages.Length}";
        }

        private void OnDisable()
        {
            if (view != null) view.SetActive(false);
            _visible = false;
        }
    }
}
