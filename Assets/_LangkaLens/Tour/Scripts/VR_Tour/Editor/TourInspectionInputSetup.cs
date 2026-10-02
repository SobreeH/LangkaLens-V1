using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;

public static class TourInspectionInputSetup
{
    private const string Folder = "Assets/_LangkaLens/Tour/Materials/VRtour/InspectionButtons";

    [MenuItem("LangkaLens/Tour/Configure Y Inspection Button")]
    public static void Configure()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != TourReconstructionSetup.ScenePath)
            throw new InvalidOperationException("Open VR_Tour_Scene outside Play Mode first.");
        var presenter = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TourReconstructionPresenter>(true)).Single();
        var highlight = presenter.GetComponent<TourInspectionButtonHighlight>();
        if (highlight == null) highlight = Undo.AddComponent<TourInspectionButtonHighlight>(presenter.gameObject);
        // Restore previous setup before regenerating; never split an already split mesh.
        foreach (var target in highlight.Targets)
        {
            if (target.renderer == null) continue;
            Undo.RecordObject(target.renderer, "Restore controller source");
            target.renderer.sharedMesh = target.originalMesh;
            target.renderer.sharedMaterials = target.originalMaterials;
        }
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/_LangkaLens/Tour/Materials/VRtour", "InspectionButtons");
        var targets = new List<TourInspectionButtonHighlight.Target>();
        var renderers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OVRControllerHelper>(true))
            .Where(h => h.m_controller == OVRInput.Controller.LTouch)
            .SelectMany(h => h.GetComponentsInChildren<SkinnedMeshRenderer>(true)).Distinct();
        foreach (var renderer in renderers)
        {
            // Meta's older Rift visual calls Button 2 b_button02; other left visuals name it Y.
            int bone = Array.FindIndex(renderer.bones, b => b != null &&
                (b.name == "b_button_y" || (renderer.name == "left_touch_mesh" && b.name == "b_button02")));
            if (bone < 0) continue;
            var original = renderer.sharedMesh;
            var mesh = UnityEngine.Object.Instantiate(original);
            mesh.name = original.name + "_TourY";
            var weights = original.boneWeights;
            var materials = renderer.sharedMaterials.ToList();
            var indices = new List<int>();
            var buttonTriangles = new List<int[]>();
            for (int submesh = 0; submesh < original.subMeshCount; ++submesh)
            {
                var triangles = original.GetTriangles(submesh);
                var body = new List<int>();
                var button = new List<int>();
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    bool isButton = IsButton(weights[triangles[i]], bone) &&
                        IsButton(weights[triangles[i + 1]], bone) && IsButton(weights[triangles[i + 2]], bone);
                    var destination = isButton ? button : body;
                    destination.Add(triangles[i]); destination.Add(triangles[i + 1]); destination.Add(triangles[i + 2]);
                }
                mesh.SetTriangles(body, submesh, false);
                if (button.Count == 0) continue;
                indices.Add(original.subMeshCount + buttonTriangles.Count);
                buttonTriangles.Add(button.ToArray());
                string materialPath = Folder + "/" + original.name + "_Y_" + submesh + ".mat";
                var material = new Material(materials[submesh]);
                material.name = original.name + "_Y_" + submesh;
                material.EnableKeyword("_EMISSION");
                material.SetTexture("_EmissionMap", null);
                material.SetColor("_EmissionColor", new Color(0.1f, 0.07f, 0.03f));
                // The source marks emission as black; URP would remove the keyword on reload.
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                // The source marks emission as black; URP would remove the keyword on reload.
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                materials.Add(SaveAsset(material, materialPath));
            }
            if (buttonTriangles.Count == 0)
                throw new InvalidOperationException(renderer.name + ": no rigid Y-button triangles found.");
            mesh.subMeshCount = original.subMeshCount + buttonTriangles.Count;
            for (int i = 0; i < buttonTriangles.Count; ++i)
                mesh.SetTriangles(buttonTriangles[i], original.subMeshCount + i, false);
            // Vertex positions, bone weights, bind poses and UVs remain exactly as authored.
            mesh.bounds = original.bounds;
            var savedMesh = SaveAsset(mesh, Folder + "/" + original.name + "_TourY.asset");
            targets.Add(new TourInspectionButtonHighlight.Target
            {
                renderer = renderer, originalMesh = original, highlightMesh = savedMesh,
                originalMaterials = renderer.sharedMaterials, highlightMaterials = materials.ToArray(),
                buttonMaterialIndices = indices.ToArray()
            });
        }
        if (targets.Count == 0) throw new InvalidOperationException("No left controller visuals found.");
        var serialized = new SerializedObject(highlight);
        var entries = serialized.FindProperty("targets");
        entries.arraySize = targets.Count;
        for (int i = 0; i < targets.Count; ++i)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            var target = targets[i];
            entry.FindPropertyRelative("renderer").objectReferenceValue = target.renderer;
            entry.FindPropertyRelative("originalMesh").objectReferenceValue = target.originalMesh;
            entry.FindPropertyRelative("highlightMesh").objectReferenceValue = target.highlightMesh;
            SetMaterials(entry.FindPropertyRelative("originalMaterials"), target.originalMaterials);
            SetMaterials(entry.FindPropertyRelative("highlightMaterials"), target.highlightMaterials);
            var slots = entry.FindPropertyRelative("buttonMaterialIndices");
            slots.arraySize = target.buttonMaterialIndices.Length;
            for (int j = 0; j < slots.arraySize; ++j) slots.GetArrayElementAtIndex(j).intValue = target.buttonMaterialIndices[j];
        }
        serialized.ApplyModifiedProperties();
        AddHint("inspect_reconstruction", "Inspect ruins", "สำรวจแบบบูรณะ");
        AddHint("cancel_reconstruction", "Cancel loading", "ยกเลิกการโหลด");
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Configured Y inspection and blinking button on " + targets.Count + " left controller visuals.");
    }

    private static bool IsButton(BoneWeight weight, int bone) =>
        (weight.boneIndex0 == bone && weight.weight0 > 0.99f) ||
        (weight.boneIndex1 == bone && weight.weight1 > 0.99f) ||
        (weight.boneIndex2 == bone && weight.weight2 > 0.99f) ||
        (weight.boneIndex3 == bone && weight.weight3 > 0.99f);

    private static T SaveAsset<T>(T asset, string path) where T : UnityEngine.Object
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null) { AssetDatabase.CreateAsset(asset, path); return asset; }
        EditorUtility.CopySerialized(asset, existing);
        UnityEngine.Object.DestroyImmediate(asset);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static void SetMaterials(SerializedProperty property, Material[] materials)
    {
        property.arraySize = materials.Length;
        for (int i = 0; i < materials.Length; ++i) property.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
    }

    private static void AddHint(string key, string en, string th)
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(ControllerCanvasHints.TableName);
        foreach (var table in collection.StringTables)
        {
            if (table.LocaleIdentifier.Code == "en") table.AddEntry(key, en);
            if (table.LocaleIdentifier.Code == "th") table.AddEntry(key, th);
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection.SharedData);
    }
}
