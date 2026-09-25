using UnityEngine;

/// <summary>
/// Configures optimal display, foveation, and GPU performance settings for Meta Quest 3 and 3S.
/// Attach this component to the Camera Rig or an active manager GameObject in your VR scene.
/// </summary>
[DefaultExecutionOrder(-100)]
public class QuestPerformanceOptimizer : MonoBehaviour
{
    [Header("Frame Rate & Display")]
    [Tooltip("Target display frequency and frame rate (72Hz is standard and stable for rich Quest scenes, 90Hz for high-speed motion).")]
    [SerializeField] private int targetFrameRate = 72;

    [Header("Fixed Foveated Rendering")]
    [Tooltip("Initial FFR level. High is recommended for Quest 3 / 3S.")]
    [SerializeField] private OVRManager.FoveatedRenderingLevel foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.High;

    [Tooltip("Automatically scales FFR based on real-time GPU utilization.")]
    [SerializeField] private bool useDynamicFoveation = true;

    [Header("Hardware Performance")]
    [SerializeField] private OVRManager.ProcessorPerformanceLevel cpuPerformanceLevel = OVRManager.ProcessorPerformanceLevel.SustainedLow;
    [SerializeField] private OVRManager.ProcessorPerformanceLevel gpuPerformanceLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;

    [Header("Clarity & Sharpening")]
    [Tooltip("Enables Meta contrast-adaptive sharpening for crisp model inspection at 1:1 render scale.")]
    [SerializeField] private bool enableSharpening = true;

    private void Awake()
    {
        ApplyPerformanceSettings();
    }

    private void Start()
    {
        // Re-apply in Start to ensure OVRManager and OVRPlugin display instances are fully initialized
        ApplyPerformanceSettings();
    }

    public void ApplyPerformanceSettings()
    {
        // 1. Lock frame rate and disable vsync throttling
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        // 2. Request native display refresh rate from Meta XR runtime
        if (OVRManager.display != null)
        {
            float[] frequencies = OVRManager.display.displayFrequenciesAvailable;
            if (frequencies != null && frequencies.Length > 0)
            {
                float targetFloat = (float)targetFrameRate;
                bool supported = false;
                foreach (float freq in frequencies)
                {
                    if (Mathf.Approximately(freq, targetFloat))
                    {
                        supported = true;
                        break;
                    }
                }

                if (supported)
                {
                    OVRPlugin.systemDisplayFrequency = targetFloat;
                }
            }
        }

        // 3. Configure Foveated Rendering (saves 20-35% GPU fragment fillrate)
        OVRManager.foveatedRenderingLevel = foveatedRenderingLevel;
        OVRManager.useDynamicFoveatedRendering = useDynamicFoveation;

        // 4. Set balanced CPU/GPU performance clocks for Snapdragon XR2 Gen 2
        OVRManager.suggestedCpuPerfLevel = cpuPerformanceLevel;
        OVRManager.suggestedGpuPerfLevel = gpuPerformanceLevel;

        // 5. Sharpening for high-detail artifact inspection
        if (enableSharpening)
        {
            if (OVRManager.instance != null)
            {
                OVRManager.instance.sharpenType = OVRPlugin.LayerSharpenType.Normal;
            }
            else
            {
                OVRPlugin.SetEyeBufferSharpenType(OVRPlugin.LayerSharpenType.Normal);
            }
        }

        Debug.Log($"[QuestPerformanceOptimizer] Initialized for Quest 3/3S: Target={targetFrameRate}Hz, FFR={foveatedRenderingLevel} (Dynamic={useDynamicFoveation}), Sharpening={enableSharpening}");
    }
}
