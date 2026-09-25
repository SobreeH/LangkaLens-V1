using UnityEngine;

/// <summary>
/// Animates a GameObject by bobbing up and down smoothly using a sine wave.
/// Supports configurable height, speed, axis, and optional gentle rotation.
/// </summary>
[DisallowMultipleComponent]
public class BobbingAnimation : MonoBehaviour
{
    [Header("Bobbing Settings")]
    [Tooltip("Maximum vertical offset distance (in meters).")]
    [SerializeField] private float bobHeight = 0.05f;

    [Tooltip("Number of complete bobbing cycles per second.")]
    [SerializeField] private float bobFrequency = 1.0f;

    [Tooltip("Local direction vector for the bobbing motion.")]
    [SerializeField] private Vector3 bobAxis = Vector3.up;

    [Tooltip("If true, randomizes the starting cycle phase so multiple icons don't move in unison.")]
    [SerializeField] private bool randomizeStartPhase = true;

    [Header("Optional Rotation")]
    [Tooltip("Enable continuous slow rotation.")]
    [SerializeField] private bool enableRotation = false;

    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float rotationSpeed = 30.0f;

    [Tooltip("Axis of rotation.")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    private Vector3 initialLocalPosition;
    private float phaseOffset;
    private bool isInitialized;

    public float BobHeight
    {
        get => bobHeight;
        set => bobHeight = value;
    }

    public float BobFrequency
    {
        get => bobFrequency;
        set => bobFrequency = value;
    }

    public bool EnableRotation
    {
        get => enableRotation;
        set => enableRotation = value;
    }

    private void Awake()
    {
        InitializePosition();
    }

    private void OnEnable()
    {
        InitializePosition();
        if (randomizeStartPhase)
        {
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }
        else
        {
            phaseOffset = 0f;
        }
    }

    private void OnDisable()
    {
        if (isInitialized)
        {
            transform.localPosition = initialLocalPosition;
        }
    }

    private void InitializePosition()
    {
        if (!isInitialized)
        {
            initialLocalPosition = transform.localPosition;
            isInitialized = true;
        }
    }

    private void Update()
    {
        // Sinusoidal vertical displacement
        float angle = (Time.time * bobFrequency * Mathf.PI * 2f) + phaseOffset;
        float offset = Mathf.Sin(angle) * bobHeight;
        transform.localPosition = initialLocalPosition + (bobAxis.normalized * offset);

        // Optional gentle rotation
        if (enableRotation && rotationSpeed != 0f)
        {
            transform.Rotate(rotationAxis.normalized, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    public void SetBasePosition(Vector3 newLocalPos)
    {
        initialLocalPosition = newLocalPos;
        isInitialized = true;
    }

    private void OnValidate()
    {
        bobHeight = Mathf.Max(0f, bobHeight);
        bobFrequency = Mathf.Max(0f, bobFrequency);
    }
}
