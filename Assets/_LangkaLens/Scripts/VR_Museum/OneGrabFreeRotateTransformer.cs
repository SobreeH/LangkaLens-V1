using UnityEngine;
using Oculus.Interaction;

public class OneGrabFreeRotateTransformer : MonoBehaviour, ITransformer
{
    [Header("Grab Settings")]
    [Tooltip("Check this to invert the rotation direction")]
    public bool invertRotation = true;

    [Header("Idle Animation")]
    [Tooltip("Speed and axis of the idle rotation (e.g., Y=15 for a slow spin)")]
    public Vector3 idleRotationSpeed = new Vector3(0f, 15f, 0f);

    private IGrabbable _grabbable;
    private Quaternion _initialObjectRotation;
    private Quaternion _initialGrabRotation;
    
    // Tracks whether you are currently holding the object
    private bool _isGrabbed = false;

    public void Initialize(IGrabbable grabbable)
    {
        _grabbable = grabbable;
    }

    private void Update()
    {
        // If the object is NOT grabbed, rotate it slowly based on the speed setting
        if (!_isGrabbed && _grabbable != null)
        {
            _grabbable.Transform.Rotate(idleRotationSpeed * Time.deltaTime, Space.World);
        }
    }

    public void BeginTransform()
    {
        // You grabbed it — pause the idle spinning
        _isGrabbed = true; 

        Pose grabPoint = _grabbable.GrabPoints[0];
        _initialGrabRotation = grabPoint.rotation;
        _initialObjectRotation = _grabbable.Transform.rotation;
    }

    public void UpdateTransform()
    {
        Pose grabPoint = _grabbable.GrabPoints[0];
        Quaternion deltaRotation = grabPoint.rotation * Quaternion.Inverse(_initialGrabRotation);

        if (invertRotation)
        {
            deltaRotation = Quaternion.Inverse(deltaRotation);
        }

        _grabbable.Transform.rotation = deltaRotation * _initialObjectRotation;
    }

    public void EndTransform()
    {
        // You let go — resume the idle spinning
        _isGrabbed = false; 
    }
}