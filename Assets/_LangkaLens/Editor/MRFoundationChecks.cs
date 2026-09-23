using System;
using LangkaLens.MR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LangkaLens.Editor
{
    public static class MRFoundationChecks
    {
        [MenuItem("Tools/LangkaLens/MR/Run four-corner calibration checks")]
        public static void Run()
        {
            int passed = 0;
            void Check(bool value, string name)
            { if (!value) throw new Exception("Foundation check failed: " + name); passed++; }
            Vector3[] source = { new Vector3(-.46f, .005f, -.44f), new Vector3(.46f, .005f, -.44f),
                new Vector3(.46f, .005f, .36f), new Vector3(-.46f, .005f, .36f) };
            var target = new Vector3[4];
            foreach (float yaw in new[] { 0f, 37f, 180f, -125f })
            foreach (float scale in new[] { 1f, 14f, 23f })
            {
                Quaternion rotation = Quaternion.Euler(0, yaw, 0);
                Vector3 position = new Vector3(17, -2.3f, 8);
                for (int i = 0; i < 4; i++) target[i] = position + rotation * (source[i] * scale);
                Check(MRFoundationFit.TrySolve(source, target, true, 1, out var fit), "Known pose is solvable");
                Check(Vector3.Distance(fit.position, position) < .0001f && Quaternion.Angle(fit.rotation, rotation) < .05f &&
                    Mathf.Abs(fit.scale - scale) < .0001f && fit.maxError < .0001f, "Known pose recovered including height");
            }
            for (int i = 0; i < 4; i++) target[i] = source[i] * 14;
            target[0] += new Vector3(.25f, .08f, -.1f);
            Check(MRFoundationFit.TrySolve(source, target, true, 1, out var noisy) && noisy.maxError < .3f, "Approximate corner capture remains usable");
            Check(Vector3.Angle(noisy.rotation * Vector3.up, Vector3.up) < .001f, "Uneven captured height does not tilt building");
            target[1] += Vector3.right * 1;
            Check(MRFoundationFit.TrySolve(source, target, false, 14, out var locked) && locked.scale == 14, "Tracking never changes locked scale");
            var reversed = new[] { source[0], source[3], source[2], source[1] };
            Check(!MRFoundationFit.TrySolve(source, reversed, true, 1, out _), "Reversed corner order rejected");
            Check(!MRFoundationFit.TrySolve(source, new Vector3[4], true, 1, out _), "Collapsed capture rejected");
            var line = new[] { Vector3.zero, Vector3.right, Vector3.right * 2, Vector3.right * 3 };
            Check(!MRFoundationFit.TrySolve(line, line, true, 1, out _), "Collinear points rejected");
            target[0] = new Vector3(float.NaN, 0, 0);
            Check(!MRFoundationFit.TrySolve(source, target, true, 1, out _), "Nonfinite capture rejected");
            Check(!MRFoundationFit.TrySolve(source, new Vector3[3], true, 1, out _), "Mismatched point counts rejected");

            var installed = UnityEngine.Object.FindFirstObjectByType<MRModelSpawner>();
            Check(installed != null && installed.Foundation != null && installed.Foundation.ValidateConfiguration(), "Scene calibration references valid");
            Check(installed.localStructureIndex == 2 && installed.Foundation.modelPrefab == installed.modelPrefabs[2], "BJ3 assigned as local structure");
            Check(!installed.Foundation.referenceModel.gameObject.activeSelf, "Authoring model is hidden");
            var view = installed.Foundation.GetComponent<MRFoundationPreview>();
            Check(view != null && view.font != null && view.ghostMaterial != null && view.lineMaterial != null, "Preview assets assigned");
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Foundation test"); SceneManager.MoveGameObjectToScene(root, scene);
                var cal = root.AddComponent<MRFourPointCalibration>();
                var align = root.AddComponent<MRMeshReferenceAlignment>(); align.fourPoint = cal;
                var spawner = root.AddComponent<MRModelSpawner>(); spawner.alignment = align;
                var content = new GameObject("Content").transform; content.SetParent(root.transform, false);
                content.SetPositionAndRotation(new Vector3(20, 3, -7), Quaternion.Euler(0, 63, 0));
                content.localScale = Vector3.one * 14; cal.contentRoot = content;
                Array.Copy(source, cal.ReferencePoints, 4);
                for (int i = 0; i < 4; i++)
                {
                    var model = UnityEngine.Object.Instantiate(installed.modelPrefabs[i], content);
                    spawner.ApplyFoundationPlacement(model.transform, i);
                    Check(model.transform.parent == content, "Model shares calibrated root");
                    if (i == 2)
                        Check(model.transform.localPosition == Vector3.zero && model.transform.localRotation == Quaternion.identity &&
                            model.transform.localScale == Vector3.one, "BJ3 uses calibration without recentering/resizing");
                    else
                    {
                        MRModelSpawner.TryGetBounds(model.transform, content, out var bounds);
                        Check(Mathf.Abs(bounds.center.x) < .001f && Mathf.Abs(bounds.center.z + .04f) < .001f &&
                            Mathf.Abs(bounds.min.y - .005f) < .001f, "Demo centered and grounded in captured base");
                        Check(bounds.size.x <= .921f && bounds.size.z <= .801f, "Demo fits common display footprint");
                        var scale = model.transform.localScale;
                        Check(Mathf.Abs(scale.x - scale.y) < .001f && Mathf.Abs(scale.x - scale.z) < .001f, "Demo proportions preserved");
                    }
                    UnityEngine.Object.DestroyImmediate(model);
                }
                // Meter-based per-model offsets remain meters even under the normalized model root.
                spawner.demoPlacements[0] = new MRModelSpawner.DemoPlacement { heightMetres = .5f, offsetMetres = new Vector2(1, -2), sizeMultiplier = .8f, yaw = 90 };
                var demo = UnityEngine.Object.Instantiate(installed.modelPrefabs[0], content);
                spawner.ApplyFoundationPlacement(demo.transform, 0);
                MRModelSpawner.TryGetBounds(demo.transform, content, out var adjusted);
                Check(Mathf.Abs(adjusted.center.x * 14 - 1) < .001f && Mathf.Abs((adjusted.center.z + .04f) * 14 + 2) < .001f &&
                    Mathf.Abs((adjusted.min.y - .005f) * 14 - .5f) < .001f, "Demo offsets use world meters");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            Debug.Log($"PASS: {passed} foundation calibration and demo placement checks.");
        }
    }
}
