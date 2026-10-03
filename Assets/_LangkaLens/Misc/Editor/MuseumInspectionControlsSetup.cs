using System;
using System.Collections.Generic;
using System.Linq;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

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
                    var moved = new Pose(pose.position + Vector3.right * 0.1f, Quaternion.Euler(20, 30, 0));
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Move, moved));
                    Require(Vector3.Distance(position, target.position) < 0.0001f, "Model moved during arrival");
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Cancel, moved));
                    manager.ExitInspectionState();

                    manager.transitionDuration = 0f;
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Hover, pose));
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Select, pose));
                    Require(manager.IsInspecting && !manager.IsMovingToInspection, "Inspection did not finish arrival");
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Move, moved));
                    Require(Vector3.Distance(target.position, position + Vector3.right * 0.1f) < 0.0001f, "Controller movement did not move museum model");
                    Require(Vector3.Angle(target.up, rotation * Vector3.up) < 0.01f, "Model tilted");
                    manager.AdjustScale(0.25f);
                    manager.OnToggleModelButtonClicked();
                    Require(!data.IsShowingPrimary && data.alternateModel.activeSelf, "Model switching failed");
                    manager.ResetInspectionView();
                    Require(Vector3.Distance(target.position, position) < 0.0001f && Quaternion.Angle(target.rotation, rotation) < 0.01f && target.localScale == scale, "Reset failed");
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Move, moved));
                    Require(Vector3.Distance(target.position, position) < 0.0001f, "Reset jumped while held");
                    data.GetComponent<TourModelTransformer>().Rotate(45);
                    grab.ProcessPointerEvent(new PointerEvent(pointer, PointerEventType.Unselect, moved));
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
        }
        finally { manager.transitionDuration = duration; }
        Require(count == 11, "Expected 11 runtime displays");
        Debug.Log("PASS: 11 museum displays tested for selection, arrival lock, movement, upright rotation, model switching, held reset and exit restoration.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
