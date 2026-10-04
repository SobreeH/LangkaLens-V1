using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MuseumInspectionControlsSetup
{
    private const string ScenePath = "Assets/_LangkaLens/_Scenes/VR_Museum_Scene.unity";

    [MenuItem("LangkaLens/Museum/Improve Inspection Controls")]
    public static void Configure()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("Open the museum scene outside Play mode first.");
        var paths = new HashSet<string>();
        foreach (var data in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ArtifactData>(true)))
        {
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(data);
            if (root == null) throw new InvalidOperationException("Museum display is not a prefab: " + data.name);
            paths.Add(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root));
        }
        foreach (string path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var data in root.GetComponentsInChildren<ArtifactData>(true))
                {
                    // Move the collider, both model variants and their common pivot together.
                    var grab = data.GetComponent<Grabbable>();
                    if (grab == null) throw new InvalidOperationException("Missing museum Grabbable: " + path);
                    data.modelContainer = data.transform;
                    grab.InjectOptionalTargetTransform(data.transform);
                    TourModelInteractionSetup.ConfigureModel(data.gameObject);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        ValidateAssets();
        Debug.Log("Updated " + paths.Count + " museum display prefabs.");
    }

    public static void ValidateAssets()
    {
        int count = 0;
        foreach (var data in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ArtifactData>(true)))
        {
            var grab = data.GetComponent<Grabbable>();
            var transformer = data.GetComponent<TourModelTransformer>();
            Require(data.modelContainer == data.transform && transformer != null && grab != null, "Missing museum pivot or transformer");
            var serialized = new SerializedObject(grab);
            Require(serialized.FindProperty("_oneGrabTransformer").objectReferenceValue == transformer &&
                serialized.FindProperty("_targetTransform").objectReferenceValue == data.modelContainer &&
                !serialized.FindProperty("_throwWhenUnselected").boolValue, "Incorrect grab configuration");
            Require(data.GetComponent<GrabInteractable>() != null && data.GetComponent<DistanceGrabInteractable>() != null, "Missing grip interactions");
            Require(data.primaryModel != null && data.alternateModel != null &&
                data.primaryModel.transform.IsChildOf(data.modelContainer) && data.alternateModel.transform.IsChildOf(data.modelContainer), "Model pair does not share its pivot");
            var events = data.GetComponent<PointableUnityEventWrapper>();
            var calls = new SerializedObject(events).FindProperty("_whenSelect.m_PersistentCalls.m_Calls");
            Require(calls.arraySize > 0 && calls.GetArrayElementAtIndex(0).FindPropertyRelative("m_MethodName").stringValue == "TriggerInspection", "Inspection entry event was lost");
            count++;
        }
        Require(count == 11, "Expected 11 museum displays");
        Debug.Log("PASS: all 11 museum displays have movement, model-pair and inspection-entry wiring.");
    }

    public static void ValidateRuntime()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Runtime validation needs Play mode.");
        var manager = InspectionManager.Instance;
        Require(manager != null && !manager.IsInspecting, "Museum must be idle before validation");
        float duration = manager.transitionDuration;
        int count = 0;
        try
        {
            foreach (var data in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ArtifactData>(true)))
            {
                var target = data.modelContainer;
                var grab = data.GetComponent<Grabbable>();
                var position = target.position;
                var rotation = target.rotation;
                var scale = target.localScale;
                var pose = new Pose(position + Vector3.forward, Quaternion.identity);
                const int pointer = -7891;
                try
                {
                    manager.transitionDuration = 0.4f;
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Hover, pose));
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Select, pose));
                    Require(manager.IsInspecting && manager.IsMovingToInspection, "Selection must open inspection");
                    var moved = new Pose(pose.position + new Vector3(0.1f, 0.2f, -0.1f), Quaternion.Euler(20, 30, 0));
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Move, moved));
                    Require(Vector3.Distance(position, target.position) < 0.0001f, "Model moved during arrival");
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Cancel, moved));
                    manager.ExitInspectionState();

                    manager.transitionDuration = 0f;
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Hover, pose));
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Select, pose));
                    Require(manager.IsInspecting && !manager.IsMovingToInspection, "Inspection did not finish arrival");
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Move, moved));
                    Require(Vector3.Distance(target.position, position) < 0.0001f, "Controller movement displaced the inspected museum model");
                    Require(Quaternion.Angle(target.rotation, rotation) > 1f, "Grip rotation stopped working");
                    Require(Vector3.Angle(target.up, rotation * Vector3.up) < 0.01f, "Model tilted");
                    manager.AdjustScale(0.25f);
                    Require(target.localScale == scale * 1.25f && Vector3.Distance(target.position, position) < 0.0001f,
                        "Zoom must change scale without moving the model");
                    manager.OnToggleModelButtonClicked();
                    Require(!data.IsShowingPrimary && data.alternateModel.activeSelf, "Model switching failed");
                    manager.ResetInspectionView();
                    Require(Vector3.Distance(target.position, position) < 0.0001f && Quaternion.Angle(target.rotation, rotation) < 0.01f && target.localScale == scale, "Reset failed");
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Move, moved));
                    Require(Vector3.Distance(target.position, position) < 0.0001f, "Reset jumped while held");
                    data.GetComponent<TourModelTransformer>().Rotate(45);
                    Require(Quaternion.Angle(target.rotation, rotation) > 1f && Vector3.Distance(target.position, position) < 0.0001f,
                        "Joystick rotation must turn the model in place");
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Unselect, moved));
                    // Exit must also recover any displacement from other scripts.
                    target.position += new Vector3(0.3f, 0.2f, 0.1f);
                    manager.ExitInspectionState();
                    Require(Vector3.Distance(target.position, position) < 0.0001f && Quaternion.Angle(target.rotation, rotation) < 0.01f && target.localScale == scale && data.IsShowingPrimary, "Exit did not restore display");
                    Require(data.pedestal == null || data.pedestal.activeSelf, "Pedestal was not restored");
                    count++;
                }
                finally
                {
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Cancel, pose));
                    manager.ExitInspectionState();
                }
            }

            // Position restoration must work even for an artifact without a custom transformer.
            var plain = new GameObject("Inspection exit restoration validation");
            try
            {
                var position = new Vector3(1f, 2f, 3f);
                var rotation = Quaternion.Euler(0f, 30f, 0f);
                plain.transform.SetPositionAndRotation(position, rotation);
                manager.transitionDuration = 0f;
                manager.EnterInspectionState(plain, plain.transform, plain.transform, null, null, null);
                Require(manager.IsInspecting, "Plain artifact did not enter inspection");
                plain.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                plain.transform.localScale = Vector3.one * 2f;
                manager.ExitInspectionState();
                Require(Vector3.Distance(plain.transform.position, position) < 0.0001f &&
                    Quaternion.Angle(plain.transform.rotation, rotation) < 0.01f && plain.transform.localScale == Vector3.one,
                    "Exit failed to restore an artifact without a transformer");
            }
            finally
            {
                manager.ExitInspectionState();
                UnityEngine.Object.DestroyImmediate(plain);
            }
        }
        finally { manager.transitionDuration = duration; }
        Require(count == 11, "Expected 11 runtime displays");
        Debug.Log("PASS: 11 museum displays tested for selection, arrival/inspection position locks, grip/stick rotation, stationary zoom, model switching, held reset and exit restoration.");
    }

    // Run on an ArtifactData coroutine host: InspectionManager stops its own coroutines on exit.
    public static IEnumerator ValidateCanvasContentResetRuntime()
    {
        Require(EditorApplication.isPlaying, "Canvas validation needs Play mode");
        var manager = InspectionManager.Instance;
        Require(manager != null && !manager.IsInspecting, "Museum must be idle before validation");
        var roots = manager.gameObject.scene.GetRootGameObjects();
        var data = roots.SelectMany(r => r.GetComponentsInChildren<ArtifactData>(true)).First();
        var scrolls = roots.SelectMany(r => r.GetComponentsInChildren<ScrollRect>(true)).ToArray();
        var descriptionScroll = manager.inspectionCanvas.GetComponentInChildren<ScrollRect>(true);
        var descriptionReset = manager.inspectionCanvas.GetComponent<CanvasContentReset>();
        var chat = roots.SelectMany(r => r.GetComponentsInChildren<GeminiChatUI>(true)).Single();
        var chatReset = chat.GetComponent<CanvasContentReset>();
        Require(descriptionReset != null && chatReset != null, "Dynamic canvases have no reset behavior");
        var descriptionChanged = typeof(InspectionManager).GetMethod("OnDescriptionChanged",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        float duration = manager.transitionDuration;
        bool chatWasActive = chat.gameObject.activeSelf;
        string chatText = chat.chatLogText.text;
        string longText = string.Concat(Enumerable.Repeat("Museum description line for scrolling.\n", 100));
        try
        {
            manager.transitionDuration = 0f;
            data.TriggerInspection();
            yield return null;
            yield return null;
            descriptionChanged.Invoke(manager, new object[] { longText });
            yield return null;
            yield return null;
            Require(descriptionScroll.content.rect.height > descriptionScroll.viewport.rect.height,
                "Long dynamic description must overflow the viewport");
            Require(Mathf.Abs(descriptionScroll.verticalNormalizedPosition - 1f) < 0.001f,
                "New dynamic description did not start at the top after layout");
            descriptionScroll.verticalNormalizedPosition = 0f;
            Require(descriptionScroll.content.anchoredPosition.y > 1f, "Description did not scroll");

            chat.gameObject.SetActive(true);
            yield return null;
            yield return null;
            chat.chatLogText.text = longText;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(chat.scrollRect.content);
            Canvas.ForceUpdateCanvases();
            chat.scrollRect.verticalNormalizedPosition = 0f;
            Require(chat.scrollRect.content.anchoredPosition.y > 1f, "Chat did not scroll");
            chat.gameObject.SetActive(false);

            foreach (var scroll in scrolls)
            {
                scroll.content.anchoredPosition += new Vector2(0f, 500f);
                scroll.velocity = new Vector2(0f, 100f);
            }
            manager.ExitInspectionState();
            foreach (var scroll in scrolls)
            {
                Require(scroll.content.anchoredPosition.sqrMagnitude < 0.001f,
                    "Exit retained a content offset on " + scroll.GetComponentInParent<Canvas>(true).name);
                Require(scroll.velocity == Vector2.zero, "Exit retained scroll inertia");
            }
            yield return null;
            yield return null;
            chat.gameObject.SetActive(true);
            yield return null;
            yield return null;
            Require(chat.chatLogText.text == longText, "Exit changed chat messages");
            Require(Mathf.Abs(chat.scrollRect.verticalNormalizedPosition - 1f) < 0.001f,
                "Hidden chat did not reopen at the top after layout");

            data.TriggerInspection();
            yield return null;
            yield return null;
            descriptionChanged.Invoke(manager, new object[] { "Short description." });
            yield return null;
            yield return null;
            Require(descriptionScroll.content.rect.height < descriptionScroll.viewport.rect.height &&
                descriptionScroll.content.anchoredPosition.sqrMagnitude < 0.001f,
                "Short dynamic description inherited a previous scroll offset");
            descriptionChanged.Invoke(manager, new object[] { longText });
            yield return null;
            yield return null;
            Require(Mathf.Abs(descriptionScroll.verticalNormalizedPosition - 1f) < 0.001f,
                "Growing dynamic content lost its starting position");
            manager.ExitInspectionState();

            manager.transitionDuration = 0.4f;
            data.TriggerInspection();
            Require(manager.IsMovingToInspection, "Early-exit validation needs an active transition");
            descriptionScroll.content.anchoredPosition = new Vector2(0f, 500f);
            descriptionScroll.velocity = new Vector2(0f, 100f);
            manager.ExitInspectionState();
            Require(!manager.IsInspecting && !manager.inspectionCanvas.activeSelf &&
                descriptionScroll.content.anchoredPosition.sqrMagnitude < 0.001f &&
                descriptionScroll.velocity == Vector2.zero, "Early inspection exit did not reset content");
            Debug.Log("PASS: " + scrolls.Length + " museum scroll views reset on exit; dynamic long/short descriptions, hidden chat reopening, retained messages, inertia and early exit verified.");
        }
        finally
        {
            manager.ExitInspectionState();
            manager.transitionDuration = duration;
            chat.chatLogText.text = chatText;
            chatReset.ResetContents();
            chat.gameObject.SetActive(chatWasActive);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
