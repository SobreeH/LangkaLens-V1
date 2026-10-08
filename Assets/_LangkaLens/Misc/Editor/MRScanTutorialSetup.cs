using LangkaLens.MR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class MRScanTutorialSetup
{
    private const string ClipPath = "Assets/_LangkaLens/MR/Video/Scan_Display.mp4";

    [MenuItem("Tools/LangkaLens/MR/Set up scan video tutorial")]
    public static void Setup()
    {
        var scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/_LangkaLens/_Scenes/MR_Scene.unity")
            throw new System.InvalidOperationException("Open MR_Scene in Edit Mode before setting up its tutorial.");

        var alignment = Object.FindFirstObjectByType<MRMeshReferenceAlignment>();
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(ClipPath);
        if (alignment == null || alignment.head == null || clip == null)
            throw new System.InvalidOperationException("The MR alignment, viewer and tutorial clip are required.");

        const string materialPath = "Assets/_LangkaLens/MR/Video/ScanTutorial.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_LangkaLens/MR/Video/ScanTutorial.shader");
            if (shader == null) throw new System.InvalidOperationException("The tutorial UI shader is missing.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        var existing = Object.FindFirstObjectByType<MRScanTutorial>(FindObjectsInactive.Include);
        if (existing != null)
        {
            var existingImage = existing.GetComponent<RawImage>();
            Undo.RecordObject(existingImage, "Set tutorial video material");
            existingImage.material = material;
            existingImage.maskable = false;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var root = new GameObject("MR Scan Tutorial", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasGroup), typeof(RawImage), typeof(VideoPlayer));
        Undo.RegisterCreatedObjectUndo(root, "Add MR scan tutorial");
        root.layer = LayerMask.NameToLayer("UI");
        root.transform.SetParent(alignment.head.parent, false);
        var rect = (RectTransform)root.transform;
        rect.sizeDelta = new Vector2(720f, 405f);
        rect.localScale = Vector3.one * 0.001f;
        var offset = new Vector3(-0.9f, 0.18f, 1.6f);
        Quaternion heading = Quaternion.Euler(0f, alignment.head.eulerAngles.y, 0f);
        rect.SetPositionAndRotation(alignment.head.position + heading * offset, heading);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = alignment.head.GetComponent<Camera>();
        var group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        var image = root.GetComponent<RawImage>();
        image.raycastTarget = false;
        image.maskable = false;
        image.material = material;

        var player = root.GetComponent<VideoPlayer>();
        player.source = VideoSource.VideoClip;
        player.clip = clip;
        player.playOnAwake = false;
        player.isLooping = true;
        player.waitForFirstFrame = true;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.aspectRatio = VideoAspectRatio.FitInside;
        player.audioOutputMode = VideoAudioOutputMode.None;

        var follow = Undo.AddComponent<GentleCanvasFollow>(root);
        var followSettings = new SerializedObject(follow);
        followSettings.FindProperty("head").objectReferenceValue = alignment.head;
        followSettings.FindProperty("viewerOffset").vector3Value = offset;
        // Visitors can glance at the video without it immediately following their gaze away.
        followSettings.FindProperty("angularDeadZone").floatValue = 35f;
        followSettings.FindProperty("positionDeadZone").floatValue = 0.15f;
        followSettings.FindProperty("smoothTime").floatValue = 0.45f;
        followSettings.ApplyModifiedProperties();

        var tutorial = Undo.AddComponent<MRScanTutorial>(root);
        var settings = new SerializedObject(tutorial);
        settings.FindProperty("alignment").objectReferenceValue = alignment;
        settings.FindProperty("videoPlayer").objectReferenceValue = player;
        settings.FindProperty("videoImage").objectReferenceValue = image;
        settings.FindProperty("presentation").objectReferenceValue = group;
        settings.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;
        Debug.Log("MR scan tutorial added: silent looping video, left of the forward view, visible until alignment completes.", root);
    }
}
