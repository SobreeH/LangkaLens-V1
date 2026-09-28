using System;
using System.Collections.Generic;
using System.Linq;
using LangkaLens.MR;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LangkaLens.Editor
{
    public static class MRMeshAreaTools
    {
        private struct Segment { public Vector2 a, b; public Segment(Vector2 x, Vector2 y) { a = x; b = y; } }
        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        [MenuItem("Tools/LangkaLens/MR/Set up mesh interior and Mesh_ref")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "MR_Scene") throw new InvalidOperationException("Open MR_Scene first.");
            var root = scene.GetRootGameObjects().Single(x => x.name == "PlayableBoundary");
            if (root.GetComponent<MRMeshPlayableArea>() != null) throw new InvalidOperationException("Already set up. Use Rebuild interior in the Inspector.");
            var reference = root.GetComponentsInChildren<MeshFilter>(true).Single(x => x.name == "Mesh_ref");
            var meshes = root.GetComponentsInChildren<MeshFilter>(true).Where(x => x.name.Contains("GLOBAL_MESH")).ToArray();
            if (meshes.Length != 8) throw new InvalidOperationException("Expected the eight BJ3 scan meshes.");
            var rig = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<OVRCameraRig>(true)).Single();
            var passthrough = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<OVRPassthroughLayer>(true)).Single();
            var spawner = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<MRModelSpawner>(true)).Single();
            var placement = scene.GetRootGameObjects().Single(x => x.name == "Model_Placement");

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Set up mesh interior with entrance reference");
            var alignment = Undo.AddComponent<MRMeshReferenceAlignment>(root);
            alignment.meshReference = reference;
            alignment.rightController = rig.rightControllerAnchor;
            alignment.head = rig.centerEyeAnchor;
            var area = Undo.AddComponent<MRMeshPlayableArea>(root);
            area.boundaryMeshes = meshes;
            area.head = rig.centerEyeAnchor;
            area.alignment = alignment;
            area.passthrough = passthrough;
            var cornerMeshes = meshes.Where(x => x.transform.GetComponentsInParent<Transform>().Any(p => p.name.StartsWith("BJ3C"))).ToArray();
            Vector3 centre = Vector3.zero;
            foreach (var mesh in cornerMeshes) centre += mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center);
            centre /= cornerMeshes.Length;
            centre = root.transform.InverseTransformPoint(centre);
            area.interiorSeed = new Vector2(centre.x, centre.z);
            Bake(area);

            Undo.SetTransformParent(placement.transform, root.transform, "Keep model placement with the aligned site");
            Undo.RecordObject(spawner, "Connect playable interior");
            spawner.playableArea = area;
            spawner.alignment = alignment;
            spawner.modelPlacement = placement.transform;
            spawner.centerEyeCamera = rig.centerEyeAnchor;
            DisableVirtualLocomotion(rig);

            const string materialPath = "Assets/_LangkaLens/Materials/MRBoundaryLine.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) throw new InvalidOperationException("URP particle line shader is missing.");
                material = new Material(shader) { name = "MRBoundaryLine" };
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var feedback = Undo.AddComponent<MRMeshAreaFeedback>(root);
            feedback.area = area;
            feedback.alignment = alignment;
            feedback.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            feedback.lineMaterial = material;
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(spawner);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root;
            Debug.Log($"Mesh interior ready: {area.outline.Length} vertices, {area.squareMetres:F1} m². Reference: {reference.name}. Scene has not been saved by this command.");
        }

        public static void DisableVirtualLocomotion(OVRCameraRig rig)
        {
            foreach (var child in rig.GetComponentsInChildren<Transform>(true))
                if (child.name == "LocomotionControllerInteractorGroup" || child.name == "MicroGesturesLocomotionHandInteractorGroup" || child.name == "LocomotionOutput")
                {
                    Undo.RecordObject(child.gameObject, "Disable virtual locomotion for physical MR walking");
                    child.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                }
        }

        // The meshes are separate exported room shells, not one watertight volume. Intersect their
        // real triangles with a horizontal plane, then find the visible enclosed region from the
        // centre. Endpoint rays preserve concavities; uniform rays verify all-around enclosure.
        public static void Bake(MRMeshPlayableArea area)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Bake outside Play mode.");
            if (area.UsesHallways || area.UsesMeshFloors) throw new InvalidOperationException("Only legacy ScanInterior mode uses the wall cross-section bake.");
            if (area.boundaryMeshes == null || area.boundaryMeshes.Length == 0) throw new InvalidOperationException("Assign the scan meshes first.");
            var segments = new List<Segment>();
            foreach (var filter in area.boundaryMeshes)
            {
                if (filter == null || filter.sharedMesh == null) throw new InvalidOperationException("Missing scan mesh.");
                var mesh = filter.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                Matrix4x4 matrix = area.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (int i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
                var hits = new Vector2[3];
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int count = 0;
                    for (int edge = 0; edge < 3; edge++)
                    {
                        Vector3 a = vertices[triangles[i + edge]], b = vertices[triangles[i + (edge + 1) % 3]];
                        if ((a.y < area.sliceHeight) == (b.y < area.sliceHeight)) continue;
                        Vector3 p = Vector3.Lerp(a, b, (area.sliceHeight - a.y) / (b.y - a.y));
                        hits[count++] = new Vector2(p.x, p.z);
                    }
                    if (count == 2 && Vector2.Distance(hits[0], hits[1]) > 0.0001f)
                        segments.Add(new Segment(hits[0], hits[1]));
                }
            }
            if (segments.Count == 0) throw new InvalidOperationException("No mesh surfaces cross this slice height.");
            var angles = new List<float>();
            for (int i = 0; i < 720; i++) angles.Add(i * Mathf.PI / 360);
            foreach (var segment in segments)
                foreach (var point in new[] { segment.a, segment.b })
                {
                    float angle = Mathf.Atan2(point.y - area.interiorSeed.y, point.x - area.interiorSeed.x);
                    for (int i = -1; i <= 1; i++) angles.Add(Mathf.Repeat(angle + i * 0.00001f, Mathf.PI * 2));
                }
            angles.Sort();
            var outline = new List<Vector2>();
            foreach (float angle in angles)
            {
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float nearest = float.PositiveInfinity;
                foreach (var line in segments)
                {
                    Vector2 edge = line.b - line.a;
                    float divisor = Cross(direction, edge);
                    if (Mathf.Abs(divisor) < 0.0000001f) continue;
                    Vector2 v = line.a - area.interiorSeed;
                    float distance = Cross(v, edge) / divisor, along = Cross(v, direction) / divisor;
                    if (distance > 0.001f && along >= -0.00001f && along <= 1.00001f)
                        nearest = Mathf.Min(nearest, distance);
                }
                if (float.IsInfinity(nearest)) throw new InvalidOperationException($"Open mesh enclosure at {angle * Mathf.Rad2Deg:F2}°. Close the mesh gap or choose another cross-section; no bounding-box fallback was used.");
                Vector2 point = area.interiorSeed + direction * nearest;
                if (outline.Count == 0 || Vector2.Distance(outline[outline.Count - 1], point) > 0.002f) outline.Add(point);
            }
            if (Vector2.Distance(outline[0], outline[outline.Count - 1]) < 0.002f) outline.RemoveAt(outline.Count - 1);
            // Remove only nearly collinear points, with at most 2 mm geometric deviation.
            for (int i = outline.Count - 1; i >= 0 && outline.Count > 3; i--)
            {
                Vector2 a = outline[(i + outline.Count - 1) % outline.Count], b = outline[i], c = outline[(i + 1) % outline.Count];
                Vector2 edge = c - a;
                float t = edge.sqrMagnitude > 0 ? Vector2.Dot(b - a, edge) / edge.sqrMagnitude : -1;
                if (t >= 0 && t <= 1 && Vector2.Distance(b, a + t * edge) < 0.002f) outline.RemoveAt(i);
            }
            float surface = 0;
            for (int i = 0; i < outline.Count; i++) surface += Cross(outline[i], outline[(i + 1) % outline.Count]);
            surface = Mathf.Abs(surface) * 0.5f;
            if (surface < 1 || !MRMeshPlayableArea.ContainsPoint(outline.ToArray(), area.interiorSeed, out _))
                throw new InvalidOperationException("The selected seed did not produce a usable interior.");
            Undo.RecordObject(area, "Bake mesh interior");
            area.outline = outline.ToArray();
            area.squareMetres = surface;
            area.bakedMeshTransforms = area.boundaryMeshes.Select(x => area.transform.worldToLocalMatrix * x.transform.localToWorldMatrix).ToArray();
            EditorUtility.SetDirty(area);
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/LangkaLens/MR/Run playable interior checks")]
        public static void RunChecks()
        {
            int passed = 0;
            void Check(bool condition, string name) { if (!condition) throw new Exception("Failed: " + name); passed++; }
            var concave = new[] { new Vector2(0, 0), new Vector2(4, 0), new Vector2(4, 1), new Vector2(1, 1), new Vector2(1, 4), new Vector2(0, 4) };
            Check(MRMeshPlayableArea.ContainsPoint(concave, new Vector2(0.5f, 3), out float clearance) && Mathf.Abs(clearance - 0.5f) < 0.001f, "Interior clearance");
            Check(!MRMeshPlayableArea.ContainsPoint(concave, new Vector2(3, 3), out _), "Concavity excluded, despite being inside AABB");
            Check(!MRMeshPlayableArea.ContainsPoint(concave, new Vector2(-0.1f, 2), out _), "Outside excluded");
            Check(MRMeshPlayableArea.ContainsPoint(concave, new Vector2(0, 2), out clearance) && clearance == 0, "Edge has zero clearance");
            Array.Reverse(concave);
            Check(MRMeshPlayableArea.ContainsPoint(concave, new Vector2(0.5f, 3), out _), "Both winding orders supported");
            Check(!MRMeshPlayableArea.ContainsPoint(concave, new Vector2(float.NaN, 0), out _), "Invalid tracking position rejected");
            Check(!MRMeshPlayableArea.ContainsPoint(Array.Empty<Vector2>(), Vector2.zero, out _), "Unconfigured outline rejected");

            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Coordinate checks");
                SceneManager.MoveGameObjectToScene(go, preview);
                var area = go.AddComponent<MRMeshPlayableArea>();
                area.outline = concave;
                go.transform.SetPositionAndRotation(new Vector3(35, 2, -19), Quaternion.Euler(0, 73, 0));
                Check(area.ContainsWorldPoint(go.transform.TransformPoint(new Vector3(0.5f, 1.7f, 3)), out clearance) && Mathf.Abs(clearance - 0.5f) < 0.001f, "Alignment preserves inside and clearance");
                Check(!area.ContainsWorldPoint(go.transform.TransformPoint(new Vector3(3, 1.7f, 3)), out _), "Alignment preserves outside");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            var installed = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<MRMeshPlayableArea>(true)).Single();
            Check(installed.ValidateConfiguration(out string error), "Scene wiring: " + error);
            if (installed.IsUnrestricted)
            {
                foreach (var local in new[] { new Vector3(-100, 1.7f, -100), new Vector3(100, 1.7f, 100),
                    new Vector3(installed.ruinExclusion.center.x, 1.7f, installed.ruinExclusion.center.y) })
                {
                    Vector3 world = installed.transform.TransformPoint(local);
                    Check(installed.ContainsWorldPoint(world, out clearance) && float.IsPositiveInfinity(clearance), "Unrestricted position has no mesh boundary");
                    Check(installed.EvaluatePositionState(world, false) == MRMeshPlayableArea.AreaState.Inside, "Playback resumes outside scan and in central opening");
                }
                Check(installed.EvaluatePositionState(new Vector3(float.NaN, 0, 0), true) == MRMeshPlayableArea.AreaState.TrackingUnavailable, "Invalid unrestricted position pauses playback");
                Check(installed.OuterOutline.Length == 0 && installed.ExclusionOutline.Length == 0, "Unrestricted mode has no boundary outlines");
            }
            else if (installed.UsesMeshFloors)
            {
                Vector2 centre = installed.ruinExclusion.center;
                Check(!installed.ContainsWorldPoint(installed.transform.TransformPoint(new Vector3(centre.x, 1.7f, centre.y)), out _), "Central model opening is not walking space");
                foreach (var floor in installed.floorPlanes)
                    Check(installed.ContainsWorldPoint(floor.position, out clearance) && clearance > installed.stopDistance + installed.recoveryMargin, "Imported floor centre is traversable");
            }
            else if (installed.UsesHallways)
            {
                Check(!installed.ContainsWorldPoint(installed.transform.TransformPoint(new Vector3(installed.ruinExclusion.center.x, 0, installed.ruinExclusion.center.y)), out _), "Actual central ruin is excluded");
                foreach (var p in HallwaySamples(installed.hallwayOuter, installed.ruinExclusion))
                    Check(installed.ContainsWorldPoint(installed.transform.TransformPoint(new Vector3(p.x, 1.7f, p.y)), out clearance) && clearance > installed.stopDistance + installed.recoveryMargin, "Each installed hallway and corner is traversable");
            }
            else
                Check(MRMeshPlayableArea.ContainsPoint(installed.outline, installed.interiorSeed, out clearance) && clearance > 1, "Actual central area is playable");
            var reference = installed.alignment.meshReference;
            Check(reference != null && reference.name == "Mesh_ref", "Exact entrance reference connected");
            if (!installed.UsesHallways && !installed.UsesMeshFloors && !installed.IsUnrestricted)
                Check(!installed.ContainsWorldPoint(reference.transform.position, out _), "Entrance reference does not accidentally expand the interior");
            var spawner = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<MRModelSpawner>(true)).Single();
            Check(spawner.playableArea == installed && spawner.modelPlacement.IsChildOf(installed.transform), "Spawned content shares site alignment");
            var rig = installed.head.GetComponentInParent<OVRCameraRig>();
            Check(!rig.GetComponentsInChildren<Transform>(true).Any(x => (x.name == "LocomotionControllerInteractorGroup" || x.name == "MicroGesturesLocomotionHandInteractorGroup" || x.name == "LocomotionOutput") && x.gameObject.activeInHierarchy), "Virtual locomotion disabled in MR");
            passed += CheckContentGate();
            passed += CheckHallways();
            Debug.Log($"PASS: {passed} playable interior checks. {installed.footprintMode}; {installed.PlayableSquareMetres:F1} m². Headset alignment/input/anchor behaviour still needs device testing.");
        }

        private static Vector2[] HallwaySamples(Rect outer, Rect inner)
        {
            float left = (outer.xMin + inner.xMin) / 2, right = (outer.xMax + inner.xMax) / 2;
            float front = (outer.yMin + inner.yMin) / 2, back = (outer.yMax + inner.yMax) / 2;
            return new[] { new Vector2(left, inner.center.y), new Vector2(right, inner.center.y),
                new Vector2(inner.center.x, front), new Vector2(inner.center.x, back),
                new Vector2(left, front), new Vector2(left, back), new Vector2(right, front), new Vector2(right, back) };
        }

        private static int CheckHallways()
        {
            int passed = 0;
            void Check(bool condition, string name) { if (!condition) throw new Exception("Failed: " + name); passed++; }
            var outer = new Rect(-10, -12, 20, 24);
            var inner = new Rect(-7, -9, 14, 18);
            foreach (var p in HallwaySamples(outer, inner))
                Check(MRMeshPlayableArea.ContainsHallwayPoint(outer, inner, p, out float d) && d >= 1.5f, "Hallways and connected corners are inside");
            Check(!MRMeshPlayableArea.ContainsHallwayPoint(outer, inner, Vector2.zero, out _), "Ruin centre excluded");
            Check(!MRMeshPlayableArea.ContainsHallwayPoint(outer, inner, new Vector2(11, 0), out _), "Beyond outer edge excluded");
            Check(!MRMeshPlayableArea.ContainsHallwayPoint(outer, inner, new Vector2(7, 0), out float clearance) && clearance == 0, "Ruin edge excluded with zero clearance");
            Check(MRMeshPlayableArea.ContainsHallwayPoint(outer, inner, new Vector2(7.2f, 0), out clearance) && Mathf.Abs(clearance - 0.2f) < 0.001f, "Inner-edge stop clearance");
            Check(MRMeshPlayableArea.ContainsHallwayPoint(outer, inner, new Vector2(9.8f, 0), out clearance) && Mathf.Abs(clearance - 0.2f) < 0.001f, "Outer-edge stop clearance");
            Check(MRMeshPlayableArea.ContainsHallwayPoint(outer, inner, new Vector2(7.3f, 9.4f), out clearance) && Mathf.Abs(clearance - 0.5f) < 0.001f, "Diagonal distance to ruin corner");
            Check(MRMeshPlayableArea.ContainsHallwayPoint(outer, inner, new Vector2(10, 0), out clearance) && clearance == 0, "Outer edge has zero clearance");
            Check(!MRMeshPlayableArea.ContainsHallwayPoint(outer, inner, new Vector2(float.NaN, 0), out _), "Invalid head position rejected");
            Check(!MRMeshPlayableArea.ValidHallways(outer, new Rect(-11, -9, 14, 18)), "Protruding ruin rejected");
            Check(!MRMeshPlayableArea.ValidHallways(outer, new Rect(-10, -9, 14, 18)), "Closed hallway rejected");
            Check(!MRMeshPlayableArea.ValidHallways(outer, new Rect(0, 0, -1, 2)), "Negative rectangle size rejected");
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Hallway coordinate checks");
                SceneManager.MoveGameObjectToScene(go, preview);
                var area = go.AddComponent<MRMeshPlayableArea>();
                area.footprintMode = MRMeshPlayableArea.FootprintMode.RectangularHallways;
                area.hallwayOuter = outer; area.ruinExclusion = inner;
                go.transform.SetPositionAndRotation(new Vector3(35, 2, -19), Quaternion.Euler(0, 73, 0));
                Check(area.ContainsWorldPoint(go.transform.TransformPoint(new Vector3(8.5f, 1.7f, 0)), out clearance) && Mathf.Abs(clearance - 1.5f) < 0.001f, "Aligned hallway preserves clearance");
                Check(!area.ContainsWorldPoint(go.transform.TransformPoint(Vector3.zero), out _), "Aligned ruin remains excluded");
                Check(Mathf.Abs(area.PlayableSquareMetres - 228) < 0.001f, "Area subtracts central exclusion");
                Vector3 World(float x) => go.transform.TransformPoint(new Vector3(x, 1.7f, 0));
                Check(area.EvaluatePositionState(World(7.2f), true) == MRMeshPlayableArea.AreaState.Outside, "Inner edge pauses playback");
                Check(area.EvaluatePositionState(World(7.3f), false) == MRMeshPlayableArea.AreaState.Outside, "Recovery hysteresis holds pause");
                Check(area.EvaluatePositionState(World(7.4f), false) == MRMeshPlayableArea.AreaState.NearBoundary, "Recovery restores with warning");
                Check(area.EvaluatePositionState(World(8.5f), true) == MRMeshPlayableArea.AreaState.Inside, "Hallway centre plays without warning");
                Check(area.EvaluatePositionState(World(9.8f), true) == MRMeshPlayableArea.AreaState.Outside, "Outer edge pauses playback");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            return passed;
        }

        // Exercise the spawner without starting XR or entering Play mode in the user's scene.
        public static int CheckContentGate()
        {
            int passed = 0;
            void Check(bool condition, string name) { if (!condition) throw new Exception("Failed: " + name); passed++; }
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Gate test");
                SceneManager.MoveGameObjectToScene(root, scene);
                var area = root.AddComponent<MRMeshPlayableArea>();
                var spawner = root.AddComponent<MRModelSpawner>();
                var template = GameObject.CreatePrimitive(PrimitiveType.Cube);
                SceneManager.MoveGameObjectToScene(template, scene);
                spawner.playableArea = area;
                spawner.siteModelPrefab = template;
                spawner.modelPlacement = root.transform;
                var current = typeof(MRModelSpawner).GetField("_currentSpawnedModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var lateUpdate = typeof(MRModelSpawner).GetMethod("LateUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var state = typeof(MRMeshPlayableArea).GetProperty("State");
                lateUpdate.Invoke(spawner, null);
                Check(current.GetValue(spawner) == null, "Spawning blocked before alignment");
                state.SetValue(area, MRMeshPlayableArea.AreaState.Inside);
                lateUpdate.Invoke(spawner, null);
                var instance = (GameObject)current.GetValue(spawner);
                Check(instance != null && instance.transform.parent == area.transform, "Site model follows aligned root");
                lateUpdate.Invoke(spawner, null);
                spawner.SpawnModel();
                Check(current.GetValue(spawner) == instance && root.transform.childCount == 1,
                    "Repeated automatic and explicit spawning keep one instance");
                Check(instance.GetComponent<MRSpatialObject>() == null, "No conflicting per-model anchor");
                area.footprintMode = MRMeshPlayableArea.FootprintMode.Unrestricted;
                Vector3 anchoredPosition = instance.transform.position;
                state.SetValue(area, area.EvaluatePositionState(new Vector3(100, 1.7f, 100), true));
                lateUpdate.Invoke(spawner, null);
                Check(instance.activeSelf && instance.transform.position == anchoredPosition, "Walking beyond scan keeps model visible and anchored");
                state.SetValue(area, MRMeshPlayableArea.AreaState.Aligning);
                lateUpdate.Invoke(spawner, null);
                Check(!instance.activeSelf && !area.CanPlay, "Unrestricted mode still requires alignment");
                state.SetValue(area, MRMeshPlayableArea.AreaState.Inside);
                lateUpdate.Invoke(spawner, null);
                Check(instance.activeSelf && current.GetValue(spawner) == instance && instance.transform.position == anchoredPosition,
                    "Realignment restores the same calibrated instance");
                state.SetValue(area, MRMeshPlayableArea.AreaState.Outside);
                lateUpdate.Invoke(spawner, null);
                Check(!instance.activeSelf, "Leaving interior hides content");
                state.SetValue(area, MRMeshPlayableArea.AreaState.Inside);
                lateUpdate.Invoke(spawner, null);
                Check(instance.activeSelf && current.GetValue(spawner) == instance, "Re-entry restores the same model");
                state.SetValue(area, MRMeshPlayableArea.AreaState.TrackingUnavailable);
                lateUpdate.Invoke(spawner, null);
                Check(!instance.activeSelf, "Tracking loss hides content");
                state.SetValue(area, MRMeshPlayableArea.AreaState.Inside);
                lateUpdate.Invoke(spawner, null);
                Check(instance.activeSelf && current.GetValue(spawner) == instance,
                    "Tracking recovery restores the same model");
                area.enabled = false;
                lateUpdate.Invoke(spawner, null);
                Check(!instance.activeSelf, "Disabled area fails closed");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            return passed;
        }
    }

    [CustomEditor(typeof(MRMeshPlayableArea))]
    public class MRMeshPlayableAreaEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var area = (MRMeshPlayableArea)target;
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("footprintMode"));
                bool hallways = serializedObject.FindProperty("footprintMode").enumValueIndex == (int)MRMeshPlayableArea.FootprintMode.RectangularHallways;
                bool meshFloors = serializedObject.FindProperty("footprintMode").enumValueIndex == (int)MRMeshPlayableArea.FootprintMode.MeshFloors;
                bool unrestricted = serializedObject.FindProperty("footprintMode").enumValueIndex == (int)MRMeshPlayableArea.FootprintMode.Unrestricted;
                if (unrestricted)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ruinExclusion"), new GUIContent("Model opening reference (X/Z)"));
                    EditorGUILayout.HelpBox("Visitors can play inside and outside the scanned mesh. The mesh anchors the model; alignment, tracking and passthrough are still required.", MessageType.Info);
                }
                else if (meshFloors)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("floorPlanes"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ruinExclusion"), new GUIContent("Model opening reference (X/Z)"));
                    EditorGUILayout.HelpBox("Walking space follows the union of the imported floor planes. The rectangle is only a reference for sizing and centering the displayed model.", MessageType.Info);
                }
                else if (hallways)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("hallwayOuter"), new GUIContent("Outer hallway bounds (X/Z)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ruinExclusion"), new GUIContent("Central exclusion (X/Z)"));
                    EditorGUILayout.HelpBox("Rect Y means local Z. Cyan: outer limit. Orange: excluded ruin/enclosure. Drag the edge handles in Scene view. Dimensions are provisional until measured on site.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("boundaryMeshes"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sliceHeight"));
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("interiorSeed"), new GUIContent("Entrance arrow target (X/Z)"));
            }
            if (serializedObject.FindProperty("footprintMode").enumValueIndex == (int)MRMeshPlayableArea.FootprintMode.Unrestricted)
                DrawPropertiesExcluding(serializedObject, "m_Script", "footprintMode", "floorPlanes", "hallwayOuter", "ruinExclusion", "boundaryMeshes", "sliceHeight", "interiorSeed", "stopDistance", "warningDistance", "recoveryMargin");
            else
                DrawPropertiesExcluding(serializedObject, "m_Script", "footprintMode", "floorPlanes", "hallwayOuter", "ruinExclusion", "boundaryMeshes", "sliceHeight", "interiorSeed");
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox(area.IsUnrestricted ? "Playable area: unrestricted. Scan edges do not pause playback or show boundary warnings." : $"Playable footprint: {area.PlayableSquareMetres:F1} m². " + (area.UsesMeshFloors ? "Imported mesh floor planes, with overlap seams removed." : area.UsesHallways ? "Outer rectangle minus central exclusion; scan partitions are ignored." : "Legacy scan-derived interior."), MessageType.Info);
            if (!area.ValidateConfiguration(out string error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (area.developmentMode) EditorGUILayout.HelpBox("Development mode bypasses alignment, tracking and walking-area restrictions.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (area.UsesMeshFloors)
                {
                    if (GUILayout.Button("Refresh mesh floor footprint")) area.RebuildFloorFootprint();
                }
                else if (!area.IsUnrestricted && !area.UsesHallways && GUILayout.Button("Rebuild interior from scan meshes")) MRMeshAreaTools.Bake(area);
            if (Application.isPlaying) EditorGUILayout.LabelField("State", area.State.ToString());
        }

        private void OnSceneGUI()
        {
            var area = (MRMeshPlayableArea)target;
            if (area.IsUnrestricted)
            {
                DrawLoop(area, MRMeshPlayableArea.RectangleCorners(area.ruinExclusion), new Color(1, 0.6f, 0.1f));
                Vector2 centre = area.ruinExclusion.center;
                Handles.Label(area.transform.TransformPoint(new Vector3(centre.x, 0.1f, centre.y)), "MODEL DISPLAY\nUnrestricted visitor area");
                return;
            }
            if (area.UsesMeshFloors)
            {
                Vector2[] edges = area.FloorFootprint?.Edges ?? Array.Empty<Vector2>();
                Handles.color = Color.cyan;
                for (int i = 0; i < edges.Length; i += 2)
                    Handles.DrawAAPolyLine(4, area.transform.TransformPoint(new Vector3(edges[i].x, 0.05f, edges[i].y)),
                        area.transform.TransformPoint(new Vector3(edges[i + 1].x, 0.05f, edges[i + 1].y)));
                DrawLoop(area, MRMeshPlayableArea.RectangleCorners(area.ruinExclusion), new Color(1, 0.6f, 0.1f));
                Vector2 centre = area.ruinExclusion.center;
                Handles.Label(area.transform.TransformPoint(new Vector3(centre.x, 0.1f, centre.y)), "MODEL DISPLAY\nFootprint-based scale reference");
                return;
            }
            DrawLoop(area, area.OuterOutline, Color.cyan);
            DrawLoop(area, area.ExclusionOutline, new Color(1, 0.4f, 0.1f));
            if (area.UsesHallways)
            {
                Rect o = area.hallwayOuter, r = area.ruinExclusion;
                if (MRMeshPlayableArea.ValidHallways(o, r))
                {
                    DrawHallwayFill(area, Rect.MinMaxRect(o.xMin, o.yMin, o.xMax, r.yMin));
                    DrawHallwayFill(area, Rect.MinMaxRect(o.xMin, r.yMax, o.xMax, o.yMax));
                    DrawHallwayFill(area, Rect.MinMaxRect(o.xMin, r.yMin, r.xMin, r.yMax));
                    DrawHallwayFill(area, Rect.MinMaxRect(r.xMax, r.yMin, o.xMax, r.yMax));
                }
                if (!Application.isPlaying)
                {
                    EditRectangle(area, false);
                    EditRectangle(area, true);
                }
                Vector2 c = area.ruinExclusion.center;
                Handles.Label(area.transform.TransformPoint(new Vector3(c.x, 0.1f, c.y)), "RUIN / ENCLOSURE\nExcluded from walking area");
            }
            Handles.Label(area.transform.TransformPoint(new Vector3(area.interiorSeed.x, 0.1f, area.interiorSeed.y)), $"{area.footprintMode}\n{area.PlayableSquareMetres:F1} m² (provisional)");
        }

        private static void DrawLoop(MRMeshPlayableArea area, Vector2[] polygon, Color color)
        {
            if (polygon == null || polygon.Length < 3) return;
            var points = new Vector3[polygon.Length + 1];
            for (int i = 0; i < polygon.Length; i++) points[i] = area.transform.TransformPoint(new Vector3(polygon[i].x, 0.05f, polygon[i].y));
            points[points.Length - 1] = points[0];
            Handles.color = color;
            Handles.DrawAAPolyLine(4, points);
        }

        private static void DrawHallwayFill(MRMeshPlayableArea area, Rect rect)
        {
            Vector3[] points = MRMeshPlayableArea.RectangleCorners(rect)
                .Select(p => area.transform.TransformPoint(new Vector3(p.x, 0.03f, p.y))).ToArray();
            Handles.DrawSolidRectangleWithOutline(points, new Color(0.1f, 0.9f, 0.7f, 0.12f), Color.clear);
        }

        private static void EditRectangle(MRMeshPlayableArea area, bool exclusion)
        {
            Rect rect = exclusion ? area.ruinExclusion : area.hallwayOuter;
            Handles.color = exclusion ? new Color(1, 0.4f, 0.1f) : Color.cyan;
            for (int edge = 0; edge < 4; edge++)
            {
                bool xAxis = edge < 2;
                Vector2 local = xAxis ? new Vector2(edge == 0 ? rect.xMin : rect.xMax, rect.center.y) : new Vector2(rect.center.x, edge == 2 ? rect.yMin : rect.yMax);
                Vector3 world = area.transform.TransformPoint(new Vector3(local.x, 0.08f, local.y));
                Vector3 direction = area.transform.TransformDirection(xAxis ? Vector3.right : Vector3.forward);
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.Slider(world, direction, HandleUtility.GetHandleSize(world) * 0.09f, Handles.CubeHandleCap, 0.1f);
                if (!EditorGUI.EndChangeCheck()) continue;
                Vector3 next = area.transform.InverseTransformPoint(moved);
                Undo.RecordObject(area, "Resize rectangular hallway");
                if (edge == 0) rect.xMin = Mathf.Min(next.x, rect.xMax - 0.1f);
                if (edge == 1) rect.xMax = Mathf.Max(next.x, rect.xMin + 0.1f);
                if (edge == 2) rect.yMin = Mathf.Min(next.z, rect.yMax - 0.1f);
                if (edge == 3) rect.yMax = Mathf.Max(next.z, rect.yMin + 0.1f);
                if (exclusion) area.ruinExclusion = rect; else area.hallwayOuter = rect;
                EditorUtility.SetDirty(area);
                EditorSceneManager.MarkSceneDirty(area.gameObject.scene);
            }
        }
    }
}
