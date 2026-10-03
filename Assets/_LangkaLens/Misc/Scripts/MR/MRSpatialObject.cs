using UnityEngine;
using Oculus.Interaction;

namespace LangkaLens.MR
{
    [RequireComponent(typeof(Grabbable))]
    public class MRSpatialObject : MonoBehaviour
    {
        private OVRSpatialAnchor _spatialAnchor;
        private Grabbable _grabbable;

        private void Start()
        {
            _grabbable = GetComponent<Grabbable>();
            if (_grabbable != null)
            {
                _grabbable.WhenPointerEventRaised += HandlePointerEvent;
            }

            // Initially anchor the object when it spawns
            AnchorObject();
        }

        private void HandlePointerEvent(PointerEvent evt)
        {
            if (evt.Type == PointerEventType.Select)
            {
                // Object is grabbed - remove anchor so it can be moved
                UnanchorObject();
            }
            else if (evt.Type == PointerEventType.Unselect)
            {
                // Object is released - re-anchor it at its new position
                AnchorObject();
            }
        }

        private void AnchorObject()
        {
            if (_spatialAnchor == null)
            {
                // Adding the OVRSpatialAnchor component automatically locks the GameObject 
                // to its physical position in the world to prevent tracking drift.
                _spatialAnchor = gameObject.AddComponent<OVRSpatialAnchor>();
            }
        }

        private void UnanchorObject()
        {
            if (_spatialAnchor != null)
            {
                Destroy(_spatialAnchor);
                _spatialAnchor = null;
            }
        }

        private void OnDestroy()
        {
            if (_grabbable != null)
            {
                _grabbable.WhenPointerEventRaised -= HandlePointerEvent;
            }
        }
    }
}
