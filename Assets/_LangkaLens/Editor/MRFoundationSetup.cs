using System;
using LangkaLens.MR;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LangkaLens.Editor
{
    public static class MRFoundationSetup
    {
        [MenuItem("Tools/LangkaLens/MR/Set up four-corner foundation calibration")]
        public static void Setup()
        {
            var scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != "Assets/_LangkaLens/Scenes/MR_Scene.unity")
                throw new InvalidOperationException("Open MR_Scene in Edit Mode first.");
            var spawner = UnityEngine.Object.FindFirstObjectByType<MRModelSpawner>();
            var alignment = spawner.alignment;
            if (alignment.fourPoint != null) { Debug.Log("Four-corner setup is already installed."); return; }
            if (spawner.modelPrefabs.Length < 3 || spawner.modelPrefabs[2].name != "OG_BJ3")
                throw new InvalidOperationException("Expected BJ3 in model slot 2.");
            var feedback = alignment.GetComponent<MRMeshAreaFeedback>();
            Undo.SetCurrentGroupName("Set up four-corner foundation calibration");
            int group = Undo.GetCurrentGroup();
            var calibration = Undo.AddComponent<MRFourPointCalibration>(alignment.gameObject);
            calibration.alignment = alignment;
            calibration.area = spawner.playableArea;
            calibration.modelPrefab = spawner.modelPrefabs[2];
            var root = new GameObject("Calibrated model root");
            Undo.RegisterCreatedObjectUndo(root, "Create calibrated root");
            root.transform.SetParent(alignment.transform, false);
            calibration.contentRoot = root.transform;

            var reference = (GameObject)PrefabUtility.InstantiatePrefab(calibration.modelPrefab, scene);
            Undo.RegisterCreatedObjectUndo(reference, "Create editable BJ3 reference");
            reference.name = "BJ3 Calibration Reference (edit corners here)";
            reference.transform.SetParent(alignment.transform, false);
            reference.transform.localPosition = new Vector3(2, 0, 12.75f);
            reference.transform.localRotation = Quaternion.Euler(0, 180, 0);
            reference.transform.localScale = Vector3.one * 14;
            calibration.referenceModel = reference.transform;
            // Approximate corners of the actual platform, excluding the mesh's extra ground patch.
            // The operator captures the corresponding rectangle on site; this is not survey data.
            Vector3[] corners = {
                new Vector3(-0.46f, 0.005f, -0.44f), new Vector3(0.46f, 0.005f, -0.44f),
                new Vector3(0.46f, 0.005f, 0.36f), new Vector3(-0.46f, 0.005f, 0.36f) };
            for (int i = 0; i < 4; i++)
            {
                var marker = new GameObject("Corner " + (char)('A' + i));
                Undo.RegisterCreatedObjectUndo(marker, "Create base corner");
                marker.transform.SetParent(reference.transform, false);
                marker.transform.localPosition = corners[i];
                calibration.modelCorners[i] = marker.transform;
            }
            reference.SetActive(false);
            Undo.RecordObject(alignment, "Use foundation calibration");
            alignment.fourPoint = calibration;
            Undo.RecordObject(spawner.playableArea, "Keep calibrated viewing area unrestricted");
            spawner.playableArea.footprintMode = MRMeshPlayableArea.FootprintMode.Unrestricted;
            Undo.RecordObject(spawner, "Configure site and demo placement");
            spawner.localStructureIndex = 2;
            spawner.demoPlacements = new MRModelSpawner.DemoPlacement[4];
            for (int i = 0; i < 4; i++) spawner.demoPlacements[i] = new MRModelSpawner.DemoPlacement();
            var view = Undo.AddComponent<MRFoundationPreview>(alignment.gameObject);
            view.calibration = calibration;
            view.lineMaterial = feedback.lineMaterial;
            view.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Sarabun-Medium SDF.asset");
            Undo.RecordObject(feedback, "Use Thai-capable calibration font");
            feedback.font = view.font;

            const string folder = "Assets/_LangkaLens/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_LangkaLens", "Materials");
            const string path = folder + "/MRFoundationGhost.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) throw new InvalidOperationException("URP Unlit shader is unavailable.");
                material = new Material(shader);
                material.SetFloat("_Surface", 1);
                material.SetFloat("_Blend", 0);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0);
                material.SetColor("_BaseColor", new Color(0.15f, 0.85f, 1, 0.25f));
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                AssetDatabase.CreateAsset(material, path);
            }
            view.ghostMaterial = material;
            if (!calibration.ValidateConfiguration()) throw new InvalidOperationException("Foundation scene wiring is invalid.");
            EditorUtility.SetDirty(calibration); EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("Four-corner BJ3 calibration installed and MR_Scene saved. Other models retain demo placement.");
        }
    }
}
