using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using TMPro;
using Oculus.Interaction;

public static class TourReconstructionSetup
{
    public const string ScenePath = "Assets/_LangkaLens/_Scenes/VR_Tour_Scene.unity";
    public const string PrefabFolder = "Assets/_LangkaLens/Tour/3D Models/Prefabs/Reconstruction";
    public const string DataFolder = "Assets/_LangkaLens/Tour/VR Tour Data/Reconstruction";
    public const string GlobePrefab = "Assets/_LangkaLens/Tour/3D Models/Prefabs/globeRED.prefab";
    private const string MuseumScene = "Assets/_LangkaLens/_Scenes/VR_Museum_Scene.unity";
    private const string StationPrefab = "Assets/_LangkaLens/Museum/3D Models/Prefabs/Vmuseum/Artifact_Display_Station.prefab";
    private const string SourceFolder = "Assets/_LangkaLens/Mixed/3D Models/RestoredModels/Prefabs/";
    private static readonly int[] Areas = { 2, 3, 8, 9 };
    private static readonly string[] Sources = { "OG_BJ2", "OG_BJ3", "OG_BJ8", "OG_BW9" };

    [MenuItem("LangkaLens/Tour/Set Up Restored Reconstruction Inspection")]
    public static void Setup()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath || scene.isDirty)
            throw new InvalidOperationException("Open and save VR_Tour_Scene outside Play Mode before setup.");
        var tour = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TourManager>(true)).Single();
        EnsureFolder(PrefabFolder);
        EnsureFolder(DataFolder);
        AddTranslations();
        var panelPrefab = CreatePanel();
        var definitions = new TourReconstructionDefinition[Areas.Length];
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) throw new InvalidOperationException("Project Addressables settings are required.");
        var group = settings.FindGroup("Tour Reconstructions") ?? settings.CreateGroup("Tour Reconstructions", false,
            false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        var schema = group.GetSchema<BundledAssetGroupSchema>();
        schema.BuildPath.SetVariableByName(settings, "Local.BuildPath");
        schema.LoadPath.SetVariableByName(settings, "Local.LoadPath");
        for (int i = 0; i < Areas.Length; ++i)
        {
            string path = CreateDisplay(Sources[i], Areas[i]);
            string guid = AssetDatabase.AssetPathToGUID(path);
            settings.CreateOrMoveEntry(guid, group).address = "Tour/Reconstruction/" + Sources[i];
            string definitionPath = DataFolder + "/" + Sources[i] + ".asset";
            var definition = AssetDatabase.LoadAssetAtPath<TourReconstructionDefinition>(definitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<TourReconstructionDefinition>();
                AssetDatabase.CreateAsset(definition, definitionPath);
            }
            Undo.RecordObject(definition, "Configure tour reconstruction");
            definition.inspectionPrefab = new AssetReferenceGameObject(guid);
            EditorUtility.SetDirty(definition);
            definitions[i] = definition;
        }
        EditorUtility.SetDirty(settings);

        var presenter = tour.reconstructionPresenter;
        if (presenter == null)
        {
            var root = new GameObject("Tour Reconstruction Inspection");
            Undo.RegisterCreatedObjectUndo(root, "Add reconstruction inspection");
            presenter = Undo.AddComponent<TourReconstructionPresenter>(root);
        }
        Undo.RecordObject(presenter, "Wire reconstruction presenter");
        var inspection = presenter.GetComponent<InspectionManager>();
        if (inspection == null) inspection = Undo.AddComponent<InspectionManager>(presenter.gameObject);
        if (inspection.inspectionCanvas == null)
        {
            var panel = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, presenter.transform);
            Undo.RegisterCreatedObjectUndo(panel, "Add reconstruction panel");
            inspection.inspectionCanvas = panel;
        }
        var binding = inspection.inspectionCanvas.GetComponent<TourInspectionPanel>();
        Undo.RecordObject(binding, "Wire reconstruction panel");
        binding.inspection = inspection;
        Undo.RecordObject(inspection, "Wire reconstruction inspection");
        inspection.playerRig = tour.vrRigParent;
        inspection.centerEyeCamera = tour.vrCamera;
        inspection.inspectionCanvas.GetComponent<Canvas>().worldCamera = tour.vrCamera.GetComponent<Camera>();
        inspection.titleText = inspection.inspectionCanvas.transform.Find("Panel/Title").GetComponent<TextMeshProUGUI>();
        inspection.descriptionText = inspection.inspectionCanvas.transform.Find("Panel/Description").GetComponent<TextMeshProUGUI>();
        inspection.toggleModelButton = null;
        inspection.modelStateText = null;
        inspection.modelStateLocalizeEvent = null;
        inspection.minScaleMultiplier = 0.5f;
        inspection.maxScaleMultiplier = 1.5f;
        inspection.inspectionCanvas.SetActive(false);
        presenter.tourManager = tour;
        presenter.inspectionManager = inspection;
        Undo.RecordObject(tour, "Wire tour reconstruction");
        tour.reconstructionPresenter = presenter;

        foreach (var zoom in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<VRZoomController>(true)))
        {
            Undo.RecordObject(zoom, "Gate tour zoom during inspection");
            zoom.tourManager = tour;
        }
        foreach (var hints in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ControllerCanvasHints>(true)))
        {
            var serialized = new SerializedObject(hints);
            serialized.FindProperty("inspection").objectReferenceValue = inspection;
            serialized.ApplyModifiedProperties();
        }
        foreach (var confirmation in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LangkaLens.MR.MRMainMenuConfirmation>(true)))
        {
            Undo.RecordObject(confirmation, "Gate menu confirmation during inspection");
            confirmation.inspectionManager = inspection;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:TourArea", new[] { "Assets/_LangkaLens/Tour" }))
        {
            var area = AssetDatabase.LoadAssetAtPath<TourArea>(AssetDatabase.GUIDToAssetPath(guid));
            var node = area.startingNode;
            if (node == null) continue;
            int index = Array.IndexOf(Areas, node.areaNumber);
            if (index < 0) continue;
            Undo.RecordObject(area, "Configure area reconstruction");
            area.reconstruction = definitions[index];
            EditorUtility.SetDirty(area);
        }
        TourInspectionInputSetup.Configure();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Tour reconstruction inspection configured for 2F1, 3F1, 8F1 and 9F1.");
    }

    private static string CreateDisplay(string source, int area)
    {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(StationPrefab));
        try
        {
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = source + "_Inspection";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            var data = root.GetComponentInChildren<ArtifactData>(true);
            var pivot = data.transform;
            pivot.localPosition = Vector3.zero;
            pivot.localRotation = Quaternion.identity;
            pivot.localScale = Vector3.one;
            var pedestal = root.transform.Find("Pedestal");
            if (pedestal != null) UnityEngine.Object.DestroyImmediate(pedestal.gameObject);
            data.originalOrigin.localPosition = Vector3.zero;
            data.originalOrigin.localRotation = Quaternion.identity;
            data.uiAnchor.localPosition = new Vector3(0f, -0.65f, 0.1f);
            data.uiAnchor.localRotation = Quaternion.identity;
            data.modelContainer = pivot;
            data.pedestal = null;
            data.infoIcon = null;
            data.alternateModel = null;
            data.localizedTitle = new LocalizedString("VTour_UI", "reconstruction_title_" + area);
            data.localizedDescription = new LocalizedString("VTour_UI", "reconstruction_description_" + area);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SourceFolder + source + ".prefab"), pivot);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            Bounds bounds = LocalBounds(model, pivot);
            float largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (largest <= 0.001f) throw new InvalidOperationException(source + " has no visible model bounds.");
            model.transform.localScale /= largest;
            model.transform.localPosition = -bounds.center / largest;
            data.primaryModel = model;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
            bounds = LocalBounds(model, pivot);
            var hitbox = pivot.GetComponent<BoxCollider>();
            hitbox.center = bounds.center;
            hitbox.size = bounds.size + Vector3.one * 0.03f;
            hitbox.isTrigger = true;
            var grabbable = pivot.GetComponent<Grabbable>();
            grabbable.InjectOptionalTargetTransform(pivot);
            grabbable.MaxGrabPoints = 1;
            var properties = new SerializedObject(grabbable);
            properties.FindProperty("_throwWhenUnselected").boolValue = false;
            properties.ApplyModifiedPropertiesWithoutUndo();
            // The museum starts inspection on model selection; tour inspection already starts with left Y.
            var wrapper = pivot.GetComponent<PointableUnityEventWrapper>();
            if (wrapper != null) UnityEngine.Object.DestroyImmediate(wrapper);
            var rotate = pivot.GetComponent<OneGrabFreeRotateTransformer>();
            rotate.invertRotation = false;
            rotate.idleRotationSpeed = Vector3.zero;
            string path = PrefabFolder + "/" + root.name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return path;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject CreatePlaceholder(GameObject arrowPrefab)
    {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(arrowPrefab);
        try
        {
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "ReconstructionPlaceholder";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            var events = root.GetComponent<InteractableUnityEventWrapper>();
            ClearPersistent(events.WhenHover);
            ClearPersistent(events.WhenUnhover);
            ClearPersistent(events.WhenSelect);
            UnityEngine.Object.DestroyImmediate(root.GetComponent<NavigationArrow>());
            UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
            UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshFilter>());
            foreach (var collider in root.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            var colliderRoot = root.AddComponent<BoxCollider>();
            colliderRoot.center = new Vector3(0f, 0.05f, 0f);
            colliderRoot.size = new Vector3(0.5f, 0.45f, 0.45f);
            colliderRoot.isTrigger = true;
            var surface = root.GetComponent<Oculus.Interaction.Surfaces.ColliderSurface>();
            var serialized = new SerializedObject(surface);
            serialized.FindProperty("_collider").objectReferenceValue = colliderRoot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var globeRenderer = SetGlobeVisual(root);
            var label = new GameObject("Label", typeof(TextMeshPro), typeof(LocalizeStringEvent));
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0f, -0.32f, 0f);
            var text = label.GetComponent<TextMeshPro>();
            text.font = Font();
            text.fontSize = 0.55f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.rectTransform.sizeDelta = new Vector2(1.2f, 0.2f);
            var localized = label.GetComponent<LocalizeStringEvent>();
            localized.StringReference.SetReference("VTour_UI", "reconstruction_open");
            UnityEventTools.AddPersistentListener(localized.OnUpdateString, text.SetText);
            var marker = root.AddComponent<TourReconstructionPlaceholder>();
            var fields = new SerializedObject(marker);
            fields.FindProperty("markerRenderer").objectReferenceValue = globeRenderer;
            fields.FindProperty("statusLabel").objectReferenceValue = localized;
            fields.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(events.WhenHover, marker.OnHover);
            UnityEventTools.AddPersistentListener(events.WhenUnhover, marker.OnUnhover);
            UnityEventTools.AddPersistentListener(events.WhenSelect, marker.OnSelected);
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/ReconstructionPlaceholder.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // Update only the marker so scene authoring and display/panel customizations are preserved.
    public static void UpdatePlaceholderVisual()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before updating the marker.");
        string path = PrefabFolder + "/ReconstructionPlaceholder.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var renderer = SetGlobeVisual(root);
            var label = root.transform.Find("Label").GetComponent<TextMeshPro>();
            label.enableAutoSizing = false;
            label.fontSize = 0.55f;
            var fields = new SerializedObject(root.GetComponent<TourReconstructionPlaceholder>());
            fields.FindProperty("markerRenderer").objectReferenceValue = renderer;
            fields.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static Renderer SetGlobeVisual(GameObject root)
    {
        foreach (string name in new[] { "Foundation", "LeftWall", "RightWall", "Roof", "globeRED" })
        {
            var oldVisual = root.transform.Find(name);
            if (oldVisual != null) UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
        }
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(GlobePrefab);
        if (source == null) throw new InvalidOperationException("The globeRED placeholder source is missing.");
        var globe = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
        globe.transform.localPosition = Vector3.zero;
        globe.transform.localRotation = Quaternion.identity;
        globe.transform.localScale = Vector3.one;
        // The marker wrapper owns ray selection. Remove the source's hover-overlay/drag behavior.
        var interaction = globe.transform.Find("ISDK_RayGrabInteraction");
        if (interaction != null) UnityEngine.Object.DestroyImmediate(interaction.gameObject);
        foreach (var collider in globe.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        foreach (var body in globe.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
        Bounds bounds = LocalBounds(globe, root.transform);
        float diameter = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (diameter <= 0.001f) throw new InvalidOperationException("globeRED has no visible bounds.");
        const float markerDiameter = 0.45f;
        globe.transform.localScale *= markerDiameter / diameter;
        globe.transform.localPosition = -bounds.center * (markerDiameter / diameter);
        bounds = LocalBounds(globe, root.transform);
        var hitbox = root.GetComponent<BoxCollider>();
        hitbox.center = bounds.center;
        hitbox.size = bounds.size + Vector3.one * 0.02f;
        return globe.GetComponentInChildren<Renderer>(true);
    }

    private static GameObject CreatePanel()
    {
        var preview = EditorSceneManager.OpenPreviewScene(MuseumScene);
        GameObject root = null;
        try
        {
            var museum = preview.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<InspectionManager>(true)).Single();
            root = UnityEngine.Object.Instantiate(museum.inspectionCanvas);
            SceneManager.MoveGameObjectToScene(root, SceneManager.GetActiveScene());
            root.SetActive(false);
            root.name = "TourInspectionPanel";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one * 0.001f;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800f, 600f);
            var panel = root.transform.Find("Panel");
            Layout(panel.GetComponent<RectTransform>(), Vector2.zero, rect.sizeDelta);
            var title = panel.Find("Title").GetComponent<TextMeshProUGUI>();
            Layout(title.rectTransform, new Vector2(-85f, 245f), new Vector2(560f, 95f));
            title.fontSize = 38f;
            title.text = string.Empty;
            var description = root.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t => t.name == "Description");
            description.transform.SetParent(panel, false);
            Layout(description.rectTransform, Vector2.zero, new Vector2(710f, 360f));
            description.fontSize = 30f;
            description.alignment = TextAlignmentOptions.TopLeft;
            description.text = string.Empty;
            UnityEngine.Object.DestroyImmediate(panel.Find("DescriptionBox").gameObject);
            UnityEngine.Object.DestroyImmediate(panel.Find("Toggle_btn").gameObject);
            UnityEngine.Object.DestroyImmediate(panel.Find("Model_State").gameObject);
            var oldAnchor = root.transform.Find("InspectionAnchorArtifact");
            if (oldAnchor != null) UnityEngine.Object.DestroyImmediate(oldAnchor.gameObject);
            var close = panel.Find("Back_btn").GetComponent<UnityEngine.UI.Button>();
            Layout(close.GetComponent<RectTransform>(), new Vector2(300f, 245f), new Vector2(145f, 70f));
            close.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            LocalizeButton(close, "reconstruction_close");
            var binding = root.AddComponent<TourInspectionPanel>();
            binding.closeButton = close;
            TourSiteInformationLayout.Apply(root);
            root.GetComponent<Canvas>().worldCamera = null;
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/TourInspectionPanel.prefab");
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static void LocalizeButton(UnityEngine.UI.Button button, string key)
    {
        var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        text.fontSize = 30f;
        text.enableAutoSizing = false;
        text.alignment = TextAlignmentOptions.Center;
        var localized = text.GetComponent<LocalizeStringEvent>() ?? text.gameObject.AddComponent<LocalizeStringEvent>();
        localized.OnUpdateString.RemoveAllListeners();
        ClearPersistent(localized.OnUpdateString);
        localized.StringReference = new LocalizedString("VTour_UI", key);
        UnityEventTools.AddPersistentListener(localized.OnUpdateString, text.SetText);
    }

    private static void Layout(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition3D = new Vector3(position.x, position.y, 0f);
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static Bounds LocalBounds(GameObject model, Transform relativeTo)
    {
        var bounds = new Bounds();
        bool initialized = false;
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            Matrix4x4 matrix = relativeTo.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            Bounds local = renderer.localBounds;
            for (int corner = 0; corner < 8; ++corner)
            {
                var point = matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                if (!initialized) { bounds = new Bounds(point, Vector3.zero); initialized = true; }
                else bounds.Encapsulate(point);
            }
        }
        return bounds;
    }

    private static TMP_FontAsset Font() => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Sarabun-Medium SDF.asset");

    private static void ClearPersistent(UnityEngine.Events.UnityEventBase events)
    {
        while (events.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(events, 0);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int split = path.LastIndexOf('/');
        string parent = path.Substring(0, split);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
    }

    private static void AddTranslations()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection("VTour_UI");
        if (collection == null) throw new InvalidOperationException("VTour_UI localization collection is required.");
        AddText(collection, "reconstruction_open", "Inspect restored ruins", "สำรวจแบบจำลองบูรณะ");
        AddText(collection, "reconstruction_loading", "Loading reconstruction...", "กำลังโหลดแบบจำลอง...");
        AddText(collection, "reconstruction_retry", "Loading failed. Select to retry.", "โหลดไม่สำเร็จ เลือกเพื่อลองอีกครั้ง");
        AddText(collection, "reconstruction_close", "Close", "ปิด");
        AddText(collection, "reconstruction_smaller", "Smaller", "ย่อขนาด");
        AddText(collection, "reconstruction_larger", "Larger", "ขยายขนาด");
        TourReconstructionDescriptions.AddToTables(collection);
        EditorUtility.SetDirty(collection.SharedData);
    }

    private static void AddText(UnityEditor.Localization.StringTableCollection collection, string key, string en, string th)
    {
        foreach (var table in collection.StringTables)
        {
            string code = table.LocaleIdentifier.Code;
            if (code == "en") table.AddEntry(key, en);
            else if (code == "th") table.AddEntry(key, th);
            EditorUtility.SetDirty(table);
        }
    }
}
