using System;
using LangkaLens.MR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LangkaLens.Editor
{
    public static class MRDisplayChecks
    {
        [MenuItem("Tools/LangkaLens/MR/Run central display checks")]
        public static void Run()
        {
            int passed = 0;
            void Check(bool condition, string name)
            {
                if (!condition) throw new Exception("Failed: " + name);
                passed++;
            }
            var installed = UnityEngine.Object.FindFirstObjectByType<MRModelSpawner>();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Display test site");
                SceneManager.MoveGameObjectToScene(root, preview);
                var area = root.AddComponent<MRMeshPlayableArea>();
                area.footprintMode = MRMeshPlayableArea.FootprintMode.MeshFloors;
                Transform Floor(float x, float z, float width, float depth)
                {
                    var floor = new GameObject("Floor").transform;
                    floor.SetParent(root.transform, false);
                    floor.localPosition = new Vector3(x, 0, z);
                    floor.localRotation = Quaternion.Euler(90, 0, 0);
                    floor.localScale = new Vector3(width, depth, 1);
                    return floor;
                }
                area.floorPlanes = new[] { Floor(0, -4, 10, 2), Floor(0, 4, 10, 2), Floor(-4, 0, 2, 10), Floor(4, 0, 2, 10) };
                Check(Mathf.Abs(area.PlayableSquareMetres - 64) < 0.01f, "Floor overlaps counted once");
                Check(!area.ContainsWorldPoint(Vector3.zero, out _), "Central hole excluded");
                Check(!area.ContainsWorldPoint(new Vector3(6, 0, 0), out _), "Outside mesh floors excluded");
                Check(area.ContainsWorldPoint(new Vector3(-4, 1.7f, -4), out float distance) && distance > 0.99f, "Overlapping floor seam has no false boundary");
                Check(area.ContainsWorldPoint(new Vector3(-3, 1.7f, -4), out distance) && distance > 0.99f, "Union edge intersections have correct clearance");
                Check(!area.ContainsWorldPoint(new Vector3(float.NaN, 0, 0), out _), "Invalid position rejected");
                root.transform.SetPositionAndRotation(new Vector3(20, 3, -7), Quaternion.Euler(0, 57, 0));
                Check(area.ContainsWorldPoint(root.transform.TransformPoint(new Vector3(4, 1.7f, 0)), out distance) && Mathf.Abs(distance - 1) < 0.001f, "Mesh footprint follows site alignment");

                // Test coincident edges and adjacent sections, as well as overlaps.
                area.floorPlanes = new[] { Floor(-1, 0, 2, 2), Floor(1, 0, 2, 2), Floor(1, 0, 2, 2) };
                area.RebuildFloorFootprint();
                Check(Mathf.Abs(area.PlayableSquareMetres - 8) < 0.01f, "Duplicate floor planes do not duplicate union edges");
                Check(area.ContainsWorldPoint(root.transform.TransformPoint(Vector3.zero), out distance) && distance > 0.99f, "Touching floor seam is traversable");

                area.ruinExclusion = new Rect(-5, 4.5f, 14, 16.5f);
                foreach (var prefab in installed.modelPrefabs)
                {
                    var model = UnityEngine.Object.Instantiate(prefab, root.transform);
                    model.transform.localPosition = new Vector3(10, -5, 3);
                    model.transform.localRotation = Quaternion.Euler(0, 17, 0);
                    model.transform.localScale = Vector3.one * 10;
                    Check(MRModelSpawner.FitModelToOpening(model.transform, area, 0), prefab.name + " fits successfully");
                    MRModelSpawner.TryGetBounds(model.transform, root.transform, out Bounds bounds);
                    Check(Mathf.Abs(bounds.center.x - 2) < 0.001f && Mathf.Abs(bounds.center.z - 12.75f) < 0.001f && Mathf.Abs(bounds.min.y) < 0.001f,
                        prefab.name + " centered and grounded despite offset pivot/alignment");
                    Check(bounds.size.x <= 14.001f && bounds.size.z <= 16.501f &&
                        (Mathf.Abs(bounds.size.x - 14) < 0.001f || Mathf.Abs(bounds.size.z - 16.5f) < 0.001f), prefab.name + " maximizes fit without overflow");
                    Vector3 scale = model.transform.localScale;
                    Check(Mathf.Abs(scale.x - scale.y) < 0.001f && Mathf.Abs(scale.y - scale.z) < 0.001f, prefab.name + " proportions preserved");
                    Vector3 fittedSize = bounds.size;
                    Vector3 adjustment = new Vector3(0.25f, 0.1f, -0.4f);
                    foreach (float multiplier in new[] { 1.05f, 0.95f })
                    {
                        Check(MRModelSpawner.FitModelToOpening(model.transform, area, 0, multiplier, adjustment),
                            prefab.name + " adjusted fit succeeds");
                        MRModelSpawner.TryGetBounds(model.transform, root.transform, out bounds);
                        Check(Vector3.Distance(bounds.size, fittedSize * multiplier) < 0.002f,
                            prefab.name + " adjustment scales all dimensions proportionally");
                        Check(Mathf.Abs(bounds.center.x - 2.25f) < 0.001f &&
                            Mathf.Abs(bounds.center.z - 12.35f) < 0.001f && Mathf.Abs(bounds.min.y - 0.1f) < 0.001f,
                            prefab.name + " adjustment follows rotated site and preserves base height");
                        Vector3 adjustedPosition = model.transform.position;
                        Vector3 adjustedScale = model.transform.localScale;
                        MRModelSpawner.FitModelToOpening(model.transform, area, 0, multiplier, adjustment);
                        Check(Vector3.Distance(model.transform.position, adjustedPosition) < 0.002f &&
                            Vector3.Distance(model.transform.localScale, adjustedScale) < 0.002f,
                            prefab.name + " repeated fit does not accumulate adjustments");
                    }
                    UnityEngine.Object.DestroyImmediate(model);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Debug.Log($"PASS: {passed} mesh floor and central model display checks.");
        }
    }
}
