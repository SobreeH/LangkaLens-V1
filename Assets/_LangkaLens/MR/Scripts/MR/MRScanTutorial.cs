using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace LangkaLens.MR
{
    /// <summary>Plays the display-alignment demonstration while entrance setup is active.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(350)]
    public sealed class MRScanTutorial : MonoBehaviour
    {
        [SerializeField] private MRMeshReferenceAlignment alignment;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoImage;
        [SerializeField] private CanvasGroup presentation;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.2f;

        private RenderTexture videoTexture;
        private bool preparing;
        private bool hasFrame;
        private bool failed;
        private bool paused;
        private bool wantsPlayback;

        private void Awake()
        {
            if (alignment == null || videoPlayer == null || videoPlayer.clip == null ||
                videoImage == null || presentation == null)
            {
                Debug.LogError("Scan tutorial is missing its alignment, video or view references.", this);
                enabled = false;
                return;
            }

            // Keep a small, owned render target. The imported clip retains its original detail.
            int width = Mathf.Min(1280, (int)videoPlayer.clip.width);
            int height = Mathf.Max(1, Mathf.RoundToInt(width * (float)videoPlayer.clip.height / videoPlayer.clip.width));
            videoTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "MR scan tutorial video (runtime)",
                useMipMap = false,
                autoGenerateMips = false
            };
            videoTexture.Create();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoImage.texture = videoTexture;
            videoImage.raycastTarget = false;
            presentation.alpha = 0f;
            presentation.interactable = false;
            presentation.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            if (videoPlayer == null) return;
            failed = false;
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.frameReady += OnFrameReady;
            videoPlayer.errorReceived += OnVideoError;
        }

        private void Update()
        {
            wantsPlayback = alignment.isActiveAndEnabled && !alignment.IsConfirmed &&
                !alignment.IsBusy && HasTrackedViewer() && !paused && !failed;

            if (wantsPlayback)
            {
                if (!videoPlayer.isPrepared && !preparing)
                {
                    preparing = true;
                    videoPlayer.sendFrameReadyEvents = true;
                    videoPlayer.Prepare();
                }
                else if (videoPlayer.isPrepared && !videoPlayer.isPlaying)
                {
                    videoPlayer.Play();
                }
            }
            else if (videoPlayer.isPlaying || preparing || videoPlayer.isPrepared)
            {
                // Release the decoder after setup; a reset starts the demonstration from the beginning.
                videoPlayer.Stop();
                preparing = false;
                hasFrame = false;
            }

            float targetAlpha = wantsPlayback && hasFrame ? 1f : 0f;
            presentation.alpha = Mathf.MoveTowards(presentation.alpha, targetAlpha,
                Time.unscaledDeltaTime / Mathf.Max(0.05f, fadeSeconds));
        }

        private bool HasTrackedViewer()
        {
#if UNITY_EDITOR
            // Permit a desktop Play Mode preview without changing headset tracking requirements.
            if (!UnityEngine.XR.XRSettings.isDeviceActive) return true;
#endif
            return alignment.HeadTracked && OVRManager.hasInputFocus;
        }

        private void OnPrepared(VideoPlayer source)
        {
            preparing = false;
            if (wantsPlayback && isActiveAndEnabled) source.Play();
        }

        private void OnFrameReady(VideoPlayer source, long frame)
        {
            hasFrame = true;
            source.sendFrameReadyEvents = false;
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            failed = true;
            preparing = false;
            hasFrame = false;
            presentation.alpha = 0f;
            source.Stop();
            Debug.LogWarning("Scan tutorial video could not play: " + message, this);
        }

        private void OnApplicationPause(bool isPaused)
        {
            paused = isPaused;
            if (paused && presentation != null) presentation.alpha = 0f;
        }

        private void OnDisable()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnPrepared;
                videoPlayer.frameReady -= OnFrameReady;
                videoPlayer.errorReceived -= OnVideoError;
                videoPlayer.Stop();
            }
            preparing = false;
            hasFrame = false;
            wantsPlayback = false;
            if (presentation != null) presentation.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (videoImage != null)
            {
                videoImage.texture = null;
            }
            if (videoPlayer != null) videoPlayer.targetTexture = null;
            if (videoTexture == null) return;
            videoTexture.Release();
            Destroy(videoTexture);
        }
    }
}
