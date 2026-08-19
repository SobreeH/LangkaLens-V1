using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class UniversalOneHandedLocomotion : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.0f;
    public float turnSpeed = 90.0f;
    
    [Header("References")]
    [Tooltip("Drag the CenterEyeAnchor here so movement follows where you look")]
    public Transform centerEyeCamera; 

    private CharacterController _characterController;

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Read both left and right joysticks
        Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        // Determine which stick is being actively used by checking which has a stronger input
        Vector2 activeInput = leftStick.sqrMagnitude > rightStick.sqrMagnitude ? leftStick : rightStick;

        // Apply a small deadzone to prevent accidental drift
        if (activeInput.sqrMagnitude > 0.01f)
        {
            // Y-Axis: Forward / Backward Movement
            if (Mathf.Abs(activeInput.y) > 0.1f)
            {
                // Calculate movement direction relative to where the headset is looking
                Vector3 forward = centerEyeCamera.forward;
                forward.y = 0; // Keep movement flat on the floor
                forward.Normalize();

                // SimpleMove automatically applies deltaTime and gravity
                _characterController.SimpleMove(forward * activeInput.y * moveSpeed);
            }

            // X-Axis: Left / Right Turning
            if (Mathf.Abs(activeInput.x) > 0.1f)
            {
                transform.Rotate(0, activeInput.x * turnSpeed * Time.deltaTime, 0);
            }
        }
    }
}