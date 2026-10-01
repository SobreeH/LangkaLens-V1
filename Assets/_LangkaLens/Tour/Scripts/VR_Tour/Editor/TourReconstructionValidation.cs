using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.AddressableAssets;
using UnityEngine.Localization.Settings;
using Oculus.Interaction;

public static class TourReconstructionValidation
{
    public static string Status { get; private set; } = "Not run";
    public static int Checks { get; private set; }
    private static IEnumerator routine;
    private static int lastFrame;
    private static double deadline;

    [MenuItem("LangkaLens/Validation/Tour Reconstruction Assets")]
    public static void ValidateAssets()
    {
        Checks = 0;
        var definitions = AssetDatabase.FindAssets("t:TourReconstructionDefinition", new[] { TourReconstructionSetup.DataFolder })
            .Select(g => AssetDatabase.LoadAssetAtPath<TourReconstructionDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        Require(definitions.Length == 4, "Expected four reconstruction definitions.");
        foreach (var definition in definitions)
        {
            Require(definition.IsConfigured, definition.name + ": missing Addressable prefab.");
            var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(definition.inspectionPrefab.AssetGUID);
            Require(entry != null && entry.parentGroup.Name == "Tour Reconstructions", definition.name + ": not registered for delivery.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(definition.inspectionPrefab.AssetGUID));
            var data = prefab.GetComponentInChildren<ArtifactData>(true);
            Require(data != null && data.modelContainer != null && data.uiAnchor != null && data.primaryModel != null,
                definition.name + ": missing model/anchors.");
            Require(data.alternateModel == null, definition.name + ": unexpected alternate.");
            var grab = data.GetComponent<Grabbable>();
            Require(grab != null && data.GetComponent<OneGrabFreeRotateTransformer>() != null, definition.name + ": missing rotation.");
            var serialized = new SerializedObject(grab);
            Require(serialized.FindProperty("_targetTransform").objectReferenceValue == data.modelContainer,
                definition.name + ": rotation pivot is not the model container.");
            Require(prefab.GetComponentInChildren<RayInteractable>(true) != null, definition.name + ": missing ray grab.");
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, definition.name + ": missing script.");
        }
        var tour = UnityEngine.Object.FindFirstObjectByType<TourManager>();
        Require(tour != null && tour.reconstructionPresenter != null, "Tour scene wiring is missing.");
        var presenter = tour.reconstructionPresenter;
        Require(tour.photoSphereRenderer != null && tour.photoSphereRenderer.sharedMaterial.HasProperty("_InspectionFade"),
            "Panorama shader cannot fade during inspection.");
        Require(!ShaderUtil.ShaderHasError(tour.photoSphereRenderer.sharedMaterial.shader), "Panorama shader has compilation errors.");
        Require(presenter.inspectionManager != null && presenter.placeholderPrefab != null && presenter.tourManager == tour,
            "Presenter is missing a required reference.");
        var globe = presenter.placeholderPrefab.transform.Find("globeRED");
        Require(globe != null && PrefabUtility.GetCorrespondingObjectFromSourceAtPath(globe.gameObject,
            TourReconstructionSetup.GlobePrefab) != null, "Marker does not use globeRED.");
        Require(presenter.placeholderPrefab.GetComponentsInChildren<RayInteractable>(true).Length == 1 &&
            presenter.placeholderPrefab.GetComponentInChildren<HoverIcon>(true) == null,
            "Globe marker has duplicate or legacy overlay interactions.");
        var panel = presenter.inspectionManager.inspectionCanvas.GetComponent<TourInspectionPanel>();
        Require(panel != null && panel.inspection == presenter.inspectionManager && panel.closeButton != null &&
            panel.zoomInButton != null && panel.zoomOutButton != null, "Panel buttons are not wired.");
        Require(panel.transform.Find("Panel/Toggle_btn") == null && panel.transform.Find("Panel/Model_State") == null,
            "Tour panel contains model comparison controls.");
        var areas = AssetDatabase.FindAssets("t:TourArea", new[] { "Assets/_LangkaLens/Tour" })
            .Select(g => AssetDatabase.LoadAssetAtPath<TourArea>(AssetDatabase.GUIDToAssetPath(g))).Where(a => a.reconstruction != null).ToArray();
        Require(areas.Length == 4, "Expected four configured areas.");
        foreach (int area in new[] { 2, 3, 8, 9 }) Require(areas.Any(a => a.startingNode != null && a.startingNode.areaNumber == area), "Missing area for " + area);
        var table = LocalizationEditorSettings.GetStringTableCollection("VTour_UI");
        foreach (var translated in table.StringTables.Where(t => t.LocaleIdentifier.Code == "en" || t.LocaleIdentifier.Code == "th"))
        {
            foreach (var key in table.SharedData.Entries.Where(e => e.Key.StartsWith("reconstruction_")))
                Require(!string.IsNullOrEmpty(translated.GetEntry(key.Id)?.Value), "Missing translation: " + key.Key);
        }
        Require(ControllerCanvasHints.ResolveKey("VR_Tour_Scene", ControllerCanvasHints.Control.LeftX,
            ControllerCanvasHints.Context.Inspecting) == "exit_inspection", "Tour inspection X hint is wrong.");
        Require(ControllerCanvasHints.ResolveKey("VR_Tour_Scene", ControllerCanvasHints.Control.LeftStick,
            ControllerCanvasHints.Context.Inspecting) == null, "Tour inspection still advertises teleport.");
        Require(ControllerCanvasHints.ResolveKey("VR_Tour_Scene", ControllerCanvasHints.Control.LeftY,
            ControllerCanvasHints.Context.Inspecting) == null, "Tour inspection advertises model switching.");
        Status = "Asset validation passed: " + Checks + " checks";
        Debug.Log(Status);
    }

    [MenuItem("LangkaLens/Validation/Tour Reconstruction Play Mode")]
    public static void StartTourSmoke()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode in VR_Tour_Scene first.");
        StartRoutine(TourSmoke());
    }

    [MenuItem("LangkaLens/Validation/Museum Inspection Play Mode")]
    public static void StartMuseumSmoke()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode in VR_Museum_Scene first.");
        StartRoutine(MuseumSmoke());
    }

    private static void StartRoutine(IEnumerator next)
    {
        if (routine != null) throw new InvalidOperationException("Validation is already running.");
        routine = next;
        Status = "Running";
        Checks = 0;
        deadline = EditorApplication.timeSinceStartup + 150;
        lastFrame = -1;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (routine == null || lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        try
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > deadline)
                throw new InvalidOperationException("Validation stopped or timed out.");
            if (routine.MoveNext()) return;
            Status = "Play Mode validation passed: " + Checks + " checks";
            Debug.Log(Status);
            Finish();
        }
        catch (Exception error)
        {
            Status = "Failed after " + Checks + " checks: " + error.Message;
            Debug.LogError(Status + "\n" + error.StackTrace);
            Finish();
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        (routine as IDisposable)?.Dispose();
        routine = null;
    }

    private static IEnumerator TourSmoke()
    {
        var tour = UnityEngine.Object.FindFirstObjectByType<TourManager>();
        Require(tour != null && tour.reconstructionPresenter != null, "Open the configured tour scene.");
        var presenter = tour.reconstructionPresenter;
        var inspection = presenter.inspectionManager;
        var panel = inspection.inspectionCanvas.GetComponent<TourInspectionPanel>();
        float originalFade = ReadPanoramaFade(tour);
        var areas = AssetDatabase.FindAssets("t:TourArea", new[] { "Assets/_LangkaLens/Tour" })
            .Select(g => AssetDatabase.LoadAssetAtPath<TourArea>(AssetDatabase.GUIDToAssetPath(g)))
            .OrderBy(a => a.startingNode.areaNumber).ToArray();
        foreach (var area in areas)
        {
            var node = area.startingNode;
            // Use the real menu path: it owns assignment of the area-level reconstruction.
            var menu = UnityEngine.Object.FindFirstObjectByType<VRMenuManager>(FindObjectsInactive.Include);
            var button = node.areaNumber == 2 ? menu.btnArea2 : node.areaNumber == 3 ? menu.btnArea3 :
                node.areaNumber == 8 ? menu.btnArea8 : menu.btnArea9;
            button.onClick.Invoke();
            while (tour.IsTransitioning) yield return null;
            Physics.SyncTransforms(); // Observe the moved marker's synchronized hitbox.
            Require(tour.currentArea == area, node.name + ": menu did not select the reconstruction area.");
            Require(presenter.State == TourReconstructionPresenter.PresentationState.Ready && presenter.PlaceholderInstance != null,
                node.name + ": marker did not spawn.");
            var marker = presenter.PlaceholderInstance;
            Require(marker.gameObject.activeInHierarchy, node.name + ": marker is inactive after arrival.");
            var hitbox = marker.GetComponent<Collider>();
            Require(!hitbox.bounds.Contains(tour.vrCamera.position), node.name + ": marker surrounds the eye.");
            Require(Vector3.Distance(marker.transform.position, tour.vrCamera.position) <= presenter.placeholderMaxDistance + 0.001f,
                node.name + ": marker is too distant to read/select.");
            marker.OnHover();
            Require(presenter.DisplayInstance == null, "Hover opened reconstruction.");
            marker.OnUnhover();
            Vector3 position = tour.vrRigParent.position;
            Quaternion rotation = tour.vrRigParent.rotation;
            marker.OnSelected();
            marker.OnSelected();
            Require(presenter.State == TourReconstructionPresenter.PresentationState.Loading, "Selection did not start one load.");
            Require(tour.ActiveArrows.All(a => !a.activeSelf) && tour.IsNavigationBlocked, "Navigation active during loading.");
            while (presenter.State == TourReconstructionPresenter.PresentationState.Loading) yield return null;
            Require(presenter.State == TourReconstructionPresenter.PresentationState.Inspecting && inspection.IsStationaryInspection,
                node.name + ": reconstruction did not open.");
            yield return null; // SDK Start validates serialized interaction wiring.
            float fadeDeadline = Time.unscaledTime + presenter.panoramaFadeDuration + 0.05f;
            while (Time.unscaledTime < fadeDeadline) yield return null;
            Require(Mathf.Abs(ReadPanoramaFade(tour) - Mathf.Lerp(originalFade, 1f, presenter.panoramaFade)) < 0.001f,
                "Inspection did not fade the panorama.");
            Require(tour.photoSphereRenderer.sharedMaterial.GetFloat("_InspectionFade") == 0f,
                "Inspection modified the shared panorama material.");
            Require(Vector3.Distance(tour.vrRigParent.position, position) < 0.001f &&
                Quaternion.Angle(tour.vrRigParent.rotation, rotation) < 0.01f, "Inspection moved the player.");
            Require(!inspection.CanToggleInspectedModel && !marker.gameObject.activeSelf, "Unexpected alternate or visible marker.");
            var head = tour.vrCamera;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            Quaternion openingView = Quaternion.LookRotation(forward, Vector3.up);
            Vector3 panelPosition = inspection.inspectionCanvas.transform.position;
            Vector3 panelOffset = Quaternion.Inverse(openingView) * (panelPosition - head.position);
            Require(panelOffset.x > 0f && Vector3.Distance(panelOffset, presenter.inspectionPanelOffset) < 0.001f,
                "Inspection panel is not on the right of the opening view.");
            Require(Vector3.Dot(inspection.inspectionCanvas.transform.forward,
                Vector3.ProjectOnPlane(panelPosition - head.position, Vector3.up).normalized) > 0.99f,
                "Right-side panel is not angled toward the user.");
            var data = presenter.DisplayInstance.GetComponentInChildren<ArtifactData>();
            Vector3 modelPosition = data.modelContainer.position;
            Quaternion before = data.modelContainer.rotation;
            var grab = data.GetComponent<Grabbable>();
            grab.ProcessPointerEvent(new PointerEvent(421, PointerEventType.Hover, new Pose(modelPosition, Quaternion.identity)));
            grab.ProcessPointerEvent(new PointerEvent(421, PointerEventType.Select, new Pose(modelPosition, Quaternion.identity)));
            grab.ProcessPointerEvent(new PointerEvent(421, PointerEventType.Move, new Pose(modelPosition, Quaternion.Euler(0f, 35f, 0f))));
            Require(Quaternion.Angle(before, data.modelContainer.rotation) > 20f, "SDK grab did not rotate the model.");
            Require(Vector3.Distance(modelPosition, data.modelContainer.position) < 0.001f, "Rotation translated the model.");
            Require(Vector3.Distance(panelPosition, inspection.inspectionCanvas.transform.position) < 0.001f,
                "Model rotation moved the information panel.");
            grab.ProcessPointerEvent(new PointerEvent(421, PointerEventType.Unselect, new Pose(modelPosition, Quaternion.Euler(0f, 35f, 0f))));
            grab.ProcessPointerEvent(new PointerEvent(421, PointerEventType.Unhover, new Pose(modelPosition, Quaternion.identity)));
            Vector3 scale = data.modelContainer.localScale;
            panel.zoomInButton.onClick.Invoke();
            Require(data.modelContainer.localScale.x > scale.x, "Panel zoom-in did not scale model.");
            panel.zoomOutButton.onClick.Invoke();
            Require(Vector3.Distance(data.modelContainer.localScale, scale) < 0.001f, "Panel zoom-out did not restore scale.");
            while (string.IsNullOrEmpty(inspection.titleText.text)) yield return null;
            Require(!string.IsNullOrEmpty(inspection.descriptionText.text), "Description did not localize.");
            panel.closeButton.onClick.Invoke();
            Require(!inspection.IsInspecting && presenter.DisplayInstance == null && marker.gameObject.activeSelf,
                "Close did not clear display and restore marker.");
            Require(!presenter.CanReturnToTourMenu, "Close input can also leave the node in the same frame.");
            yield return null;
            Require(presenter.CanReturnToTourMenu && tour.ActiveArrows.All(a => a.activeSelf), "Tour controls did not restore.");
            fadeDeadline = Time.unscaledTime + presenter.panoramaFadeDuration + 0.05f;
            while (Time.unscaledTime < fadeDeadline) yield return null;
            Require(Mathf.Abs(ReadPanoramaFade(tour) - originalFade) < 0.001f, "Close did not restore panorama brightness.");
            Debug.Log(node.name + ": spawn, select, SDK rotation, stationary rig, panel zoom, localization and close passed.");
        }
        // Placement regressions: unset nodes, sphere-wall positions, and valid authored positions.
        var placementNode = UnityEngine.Object.Instantiate(tour.CurrentNode);
        try
        {
            foreach (var position in new[] { Vector3.zero, new Vector3(7.92f, 0f, 12.47f),
                tour.vrCamera.position + new Vector3(0.5f, -0.2f, 2f) })
            {
                placementNode.iconPosition = position;
                presenter.EnterNode(placementNode, tour.currentArea.reconstruction);
                Physics.SyncTransforms();
                var marker = presenter.PlaceholderInstance;
                var eye = tour.vrCamera.position;
                Require(marker.gameObject.activeInHierarchy && !marker.GetComponent<Collider>().bounds.Contains(eye),
                    "Placement regression: globe is hidden or surrounds the eye.");
                Require(Vector3.Distance(marker.transform.position, eye) <= presenter.placeholderMaxDistance + 0.001f,
                    "Placement regression: globe remains on the panorama wall.");
                if (position != Vector3.zero)
                    Require(Vector3.Angle(position - eye, marker.transform.position - eye) < 0.01f,
                        "Placement changed the authored icon bearing.");
                if (Vector3.Distance(position, eye) > 1f && Vector3.Distance(position, eye) < presenter.placeholderMaxDistance)
                    Require(Vector3.Distance(position, marker.transform.position) < 0.001f,
                        "Placement changed a valid nearby icon position.");
            }
        }
        finally
        {
            presenter.EnterNode(tour.CurrentNode, tour.currentArea.reconstruction);
            UnityEngine.Object.Destroy(placementNode);
        }
        // Cancel a request, immediately open again, and reject the first request's late completion.
        presenter.OpenInspection();
        presenter.CloseInspection();
        presenter.OpenInspection();
        while (presenter.State == TourReconstructionPresenter.PresentationState.Loading) yield return null;
        yield return null;
        Require(inspection.IsInspecting && presenter.GetComponentsInChildren<ArtifactData>(true).Length == 1,
            "Cancelled load leaked or duplicated a display.");
        var heldData = presenter.DisplayInstance.GetComponentInChildren<ArtifactData>();
        heldData.GetComponent<Grabbable>().ProcessPointerEvent(new PointerEvent(422, PointerEventType.Select,
            new Pose(heldData.modelContainer.position, Quaternion.identity)));
        tour.ToggleMapMenu();
        Require(!inspection.IsInspecting && presenter.DisplayInstance == null && !presenter.PlaceholderInstance.gameObject.activeSelf,
            "Map opening did not clear inspection and hide marker.");
        tour.ToggleMapMenu();
        Require(presenter.PlaceholderInstance.gameObject.activeSelf, "Map close did not restore marker.");
        // Capture the deliberately invalid Addressable key as expected failure evidence.
        var invalid = ScriptableObject.CreateInstance<TourReconstructionDefinition>();
        invalid.inspectionPrefab = new UnityEngine.AddressableAssets.AssetReferenceGameObject("00000000000000000000000000000000");
        var invalidNode = UnityEngine.Object.Instantiate(tour.CurrentNode);
        var previousHandler = UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler;
        string loadFailure = null;
        try
        {
            UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = (handle, error) => loadFailure = error.Message;
            presenter.EnterNode(invalidNode, invalid);
            presenter.OpenInspection();
            while (presenter.State == TourReconstructionPresenter.PresentationState.Loading) yield return null;
            Require(loadFailure != null && presenter.State == TourReconstructionPresenter.PresentationState.Ready &&
                presenter.DisplayInstance == null && presenter.PlaceholderInstance.gameObject.activeSelf,
                "Missing content did not recover to a selectable retry marker.");
            Debug.Log("Expected invalid-key load failure recovered: " + loadFailure);
        }
        finally
        {
            UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = previousHandler;
            presenter.EnterNode(tour.CurrentNode, tour.currentArea?.reconstruction);
            UnityEngine.Object.Destroy(invalidNode);
            UnityEngine.Object.Destroy(invalid);
        }
        presenter.OpenInspection();
        var unconfigured = AssetDatabase.FindAssets("t:TourNode", new[] { "Assets/_LangkaLens/Tour/VR Tour Data/BN2" })
            .Select(g => AssetDatabase.LoadAssetAtPath<TourNode>(AssetDatabase.GUIDToAssetPath(g))).First(n => n.name == "2F2");
        
        var oldArea = tour.currentArea;
        tour.currentArea = null;
        tour.LoadNode(unconfigured);
        while (tour.IsTransitioning) yield return null;
        yield return null;
        Require(presenter.DisplayInstance == null && presenter.PlaceholderInstance == null && !inspection.IsInspecting,
            "Leaving while loading leaked content into another node.");
        Require(tour.ActiveArrows.Count > 0 && tour.ActiveArrows.All(a => a.activeSelf), "Unconfigured node navigation regressed.");
        Require(Mathf.Abs(ReadPanoramaFade(tour) - originalFade) < 0.001f, "Node change left panorama dimmed.");
        tour.currentArea = areas[0];
        tour.LoadNode(areas[0].startingNode);
        while (tour.IsTransitioning) yield return null;
        presenter.OpenInspection();
        while (presenter.State == TourReconstructionPresenter.PresentationState.Loading) yield return null;
        var previousLocale = LocalizationSettings.SelectedLocale;
        try
        {
            foreach (string code in new[] { "en", "th" })
            {
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales.Single(l => l.Identifier.Code == code);
                for (int frame = 0; frame < 10; ++frame) yield return null;
                Require(!string.IsNullOrEmpty(inspection.titleText.text) && !string.IsNullOrEmpty(inspection.descriptionText.text), code + ": empty info.");
                foreach (var text in panel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                {
                    text.ForceMeshUpdate();
                    Require(!text.isTextOverflowing, code + ": overflowing " + text.name);
                    uint[] missing;
                    string visibleCharacters = new string(text.GetParsedText().Where(c => !char.IsControl(c)).ToArray());
                    Require(text.font.HasCharacters(visibleCharacters, out missing, true, false), code + ": missing glyphs in " + text.name);
                }
            }
        }
        finally { LocalizationSettings.SelectedLocale = previousLocale; }
        presenter.enabled = false;
        yield return null;
        Require(Mathf.Abs(ReadPanoramaFade(tour) - originalFade) < 0.001f, "Disabling presenter left panorama dimmed.");
        Require(!inspection.IsInspecting && presenter.GetComponentsInChildren<ArtifactData>(true).Length == 0,
            "Disabling presenter leaked inspection content.");
        presenter.enabled = true;
        Require(presenter.State == TourReconstructionPresenter.PresentationState.Ready && presenter.PlaceholderInstance != null,
            "Re-enabling presenter did not restore the node marker.");
        tour.ReturnToMainMenu();
        while (tour.IsTransitioning) yield return null;
        Require(tour.IsAtMainMenu && presenter.PlaceholderInstance == null && presenter.DisplayInstance == null,
            "Returning to tour menu did not clear reconstruction.");
        tour.currentArea = areas[0];
        tour.LoadNode(areas[0].startingNode);
        while (tour.IsTransitioning) yield return null;
        presenter.OpenInspection();
        var sceneLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("VR_Museum_Scene");
        while (!sceneLoad.isDone) yield return null;
        for (int frame = 0; frame < 10; ++frame) yield return null;
        Require(UnityEngine.Object.FindFirstObjectByType<TourReconstructionPresenter>() == null &&
            UnityEngine.Object.FindObjectsByType<ArtifactData>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .All(a => !a.transform.root.name.EndsWith("_Inspection(Clone)")), "Scene unload leaked a reconstruction display.");
    }

    private static IEnumerator MuseumSmoke()
    {
        var inspection = UnityEngine.Object.FindFirstObjectByType<InspectionManager>();
        Require(inspection != null, "Museum inspection manager missing.");
        var data = UnityEngine.Object.FindObjectsByType<ArtifactData>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .First(a => a.primaryModel != null && a.alternateModel != null);
        Vector3 original = inspection.playerRig.position;
        Quaternion rotation = inspection.playerRig.rotation;
        data.TriggerInspection();
        Require(inspection.IsInspecting && !inspection.IsStationaryInspection, "Museum entered the tour presentation mode.");
        while (!inspection.inspectionCanvas.activeSelf) yield return null;
        Require(Vector3.Distance(original, inspection.playerRig.position) > 0.01f, "Museum glide did not move the player.");
        Require(inspection.inspectionCanvas.activeSelf && inspection.CanToggleInspectedModel, "Museum panel/toggle regressed.");
        bool primary = data.IsShowingPrimary;
        inspection.OnToggleModelButtonClicked();
        Require(data.IsShowingPrimary != primary, "Museum alternate toggle regressed.");
        inspection.ExitInspectionState();
        Require(Vector3.Distance(original, inspection.playerRig.position) < 0.001f &&
            Quaternion.Angle(rotation, inspection.playerRig.rotation) < 0.01f, "Museum exit did not restore rig.");
        Require(data.IsShowingPrimary && !inspection.inspectionCanvas.activeSelf && !inspection.IsInspecting,
            "Museum exit did not reset model/UI state.");
    }

    private static float ReadPanoramaFade(TourManager tour)
    {
        var properties = new MaterialPropertyBlock();
        tour.photoSphereRenderer.GetPropertyBlock(properties);
        return properties.HasFloat("_InspectionFade") ? properties.GetFloat("_InspectionFade") :
            tour.photoSphereRenderer.sharedMaterial.GetFloat("_InspectionFade");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        ++Checks;
    }
}
