using System;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

public static class TourModelInteractionSetup
{
    // This installed ISDK version has no QuickActionsAPI. Use its public injection APIs.
    public static void ConfigureModel(GameObject target)
    {
        var grab = target.GetComponent<Grabbable>();
        var body = target.GetComponent<Rigidbody>();
        var transformer = target.GetComponent<TourModelTransformer>() ?? target.AddComponent<TourModelTransformer>();
        grab.InjectOptionalOneGrabTransformer(transformer);
        grab.MaxGrabPoints = 1;
        grab.InjectOptionalThrowWhenUnselected(false);
        body.isKinematic = true;
        body.useGravity = false;
        var old = target.GetComponent<OneGrabFreeRotateTransformer>();
        if (old != null) UnityEngine.Object.DestroyImmediate(old);

        var provider = target.GetComponent<MoveFromTargetProvider>() ?? target.AddComponent<MoveFromTargetProvider>();
        var distance = target.GetComponent<DistanceGrabInteractable>() ?? target.AddComponent<DistanceGrabInteractable>();
        distance.InjectRigidbody(body);
        distance.InjectOptionalPointableElement(grab);
        distance.InjectOptionalMovementProvider(provider);
        var near = target.GetComponent<GrabInteractable>() ?? target.AddComponent<GrabInteractable>();
        near.InjectRigidbody(body);
        near.InjectOptionalPointableElement(grab);
        foreach (var ray in target.GetComponentsInChildren<RayInteractable>(true))
            ray.InjectOptionalMovementProvider(provider);
    }

    [MenuItem("LangkaLens/Tour/Improve Model Inspection Controls")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:TourReconstructionDefinition"))
        {
            var definition = AssetDatabase.LoadAssetAtPath<TourReconstructionDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            string path = AssetDatabase.GUIDToAssetPath(definition.inspectionPrefab.AssetGUID);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ConfigureModel(root.GetComponentInChildren<ArtifactData>(true).modelContainer.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                count++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var collection = LocalizationEditorSettings.GetStringTableCollection(ControllerCanvasHints.TableName);
        foreach (var table in collection.StringTables)
        {
            bool thai = table.LocaleIdentifier.Code == "th";
            if (!thai && table.LocaleIdentifier.Code != "en") continue;
            table.AddEntry("inspect_move_model", thai ? "จับเพื่อย้าย / หมุนแบบจำลอง" : "Hold to move / turn model");
            table.AddEntry("inspect_turn_reset", thai ? "หมุน / กดเพื่อคืนมุมมอง" : "Turn / click to reset view");
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection.SharedData);
        AssetDatabase.SaveAssets();
        Debug.Log("Updated inspection controls on " + count + " reconstruction prefabs.");
    }

    private class TestGrab : IGrabbable
    {
        public List<Pose> GrabPoints { get; } = new List<Pose>();
        public Transform Transform { get; set; }
    }

    public static void ValidateMovement()
    {
        var target = new GameObject("Inspection movement validation");
        try
        {
            var transformer = target.AddComponent<TourModelTransformer>();
            var grab = new TestGrab { Transform = target.transform };
            grab.GrabPoints.Add(new Pose(new Vector3(2, 1, 3), Quaternion.identity));
            target.transform.position = new Vector3(0, 1, 2);
            transformer.Initialize(grab);
            transformer.SetInspecting(true);
            transformer.BeginTransform();
            transformer.UpdateTransform();
            Require(Vector3.Distance(target.transform.position, new Vector3(0, 1, 2)) < 0.0001f, "No snap on grab");
            grab.GrabPoints[0] = new Pose(new Vector3(2.2f, 1.3f, 2.6f), Quaternion.Euler(30, 45, 20));
            transformer.UpdateTransform();
            Require(Vector3.Distance(target.transform.position, new Vector3(0.2f, 1.3f, 1.6f)) < 0.0001f, "One-to-one movement");
            Require(Vector3.Angle(target.transform.up, Vector3.up) < 0.001f, "Stays upright");
            transformer.Rotate(90);
            var rotation = target.transform.rotation;
            transformer.UpdateTransform();
            Require(Quaternion.Angle(rotation, target.transform.rotation) < 0.001f, "Stick turn survives grab update");
            transformer.EndTransform();
            var position = target.transform.position;
            grab.GrabPoints[0] = Pose.identity;
            transformer.UpdateTransform();
            Require(target.transform.position == position, "No drift after release");
            transformer.BeginTransform();
            target.transform.position = Vector3.zero;
            transformer.Rebase();
            transformer.UpdateTransform();
            Require(target.transform.position == Vector3.zero, "Reset while held stays put");
            transformer.SetInspecting(false);
            grab.GrabPoints[0] = new Pose(Vector3.one, Quaternion.identity);
            transformer.UpdateTransform();
            Require(target.transform.position == Vector3.zero, "Inactive inspection cannot move");
            Debug.Log("PASS: 7 inspection movement checks.");
        }
        finally { UnityEngine.Object.DestroyImmediate(target); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
