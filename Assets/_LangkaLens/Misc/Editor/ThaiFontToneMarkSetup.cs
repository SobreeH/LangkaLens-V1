using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace LangkaLens.Editor
{
    public static class ThaiFontToneMarkSetup
    {
        private const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Sarabun-Medium SDF.asset";
        private const string SourceTtfPath = "Assets/TextMesh Pro/Fonts/Sarabun-Medium.ttf";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("Tools/LangkaLens/Fix Thai 3rd-Level Tone Marks (Font & Canvases)")]
        public static void ApplyAll()
        {
            PopulateSarabunOpenTypeFeatures();
            EnableOpenTypeFeaturesInProject();
        }

        [InitializeOnLoadMethod]
        private static void EnsureSarabunFeatureTableLoaded()
        {
            EditorApplication.delayCall += () =>
            {
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
                if (fontAsset != null &&
                    (fontAsset.fontFeatureTable.MarkToMarkAdjustmentRecords == null ||
                     fontAsset.fontFeatureTable.MarkToMarkAdjustmentRecords.Count == 0))
                {
                    PopulateSarabunOpenTypeFeatures();
                }
            };
        }

        public static void PopulateSarabunOpenTypeFeatures()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null || !File.Exists(SourceTtfPath))
                return;

            var packedGlyphs = new HashSet<uint>(fontAsset.glyphTable.Select(g => g.index));
            byte[] d = File.ReadAllBytes(SourceTtfPath);
            int numTables = ReadU16(d, 4);
            int gposOff = 0, headOff = 0;
            for (int i = 0; i < numTables; i++)
            {
                int rec = 12 + i * 16;
                string tag = System.Text.Encoding.ASCII.GetString(d, rec, 4);
                if (tag == "GPOS") gposOff = ReadU32(d, rec + 8);
                if (tag == "head") headOff = ReadU32(d, rec + 8);
            }

            if (gposOff == 0 || headOff == 0)
                return;

            int upem = ReadU16(d, headOff + 18);
            float emScale = (float)fontAsset.faceInfo.pointSize / upem;

            int lookupListOff = gposOff + ReadU16(d, gposOff + 8);
            int lookupCount = ReadU16(d, lookupListOff);

            var m2bList = new List<TMPro.MarkToBaseAdjustmentRecord>();
            var m2mList = new List<TMPro.MarkToMarkAdjustmentRecord>();
            var m2bKeys = new HashSet<uint>();
            var m2mKeys = new HashSet<uint>();

            for (int l = 0; l < lookupCount; l++)
            {
                int lOff = lookupListOff + ReadU16(d, lookupListOff + 2 + l * 2);
                int lType = ReadU16(d, lOff);
                int subCount = ReadU16(d, lOff + 4);

                for (int s = 0; s < subCount; s++)
                {
                    int subOff = lOff + ReadU16(d, lOff + 6 + s * 2);
                    int fmt = ReadU16(d, subOff);
                    if (fmt != 1) continue;

                    if (lType == 4) // MarkToBase
                    {
                        var markCov = ReadCoverage(d, subOff + ReadU16(d, subOff + 2));
                        var baseCov = ReadCoverage(d, subOff + ReadU16(d, subOff + 4));
                        int classCount = ReadU16(d, subOff + 6);
                        int markArrayOff = subOff + ReadU16(d, subOff + 8);
                        int baseArrayOff = subOff + ReadU16(d, subOff + 10);

                        for (int m = 0; m < markCov.Length; m++)
                        {
                            uint markGlyph = markCov[m];
                            if (!packedGlyphs.Contains(markGlyph)) continue;

                            int mRec = markArrayOff + 2 + m * 4;
                            int mClass = ReadU16(d, mRec);
                            int mAnchorOff = markArrayOff + ReadU16(d, mRec + 2);
                            short mx = ReadS16(d, mAnchorOff + 2);
                            short my = ReadS16(d, mAnchorOff + 4);

                            for (int b = 0; b < baseCov.Length; b++)
                            {
                                uint baseGlyph = baseCov[b];
                                if (!packedGlyphs.Contains(baseGlyph)) continue;

                                int bAnchorRel = ReadU16(d, baseArrayOff + 2 + (b * classCount + mClass) * 2);
                                if (bAnchorRel == 0) continue;
                                int bAnchorOff = baseArrayOff + bAnchorRel;
                                short bx = ReadS16(d, bAnchorOff + 2);
                                short by = ReadS16(d, bAnchorOff + 4);

                                bool isToneMark = (markGlyph == 735 || markGlyph == 738 || markGlyph == 741 || markGlyph == 744 || markGlyph == 746);
                                bool isTallOrSpecialBase = (baseGlyph == 464 || baseGlyph == 466 || baseGlyph == 468 || baseGlyph == 483 || baseGlyph == 489);
                                if (isToneMark && !isTallOrSpecialBase && by == my)
                                    continue;

                                uint key = (markGlyph << 16) | baseGlyph;
                                if (m2bKeys.Add(key))
                                {
                                    m2bList.Add(new TMPro.MarkToBaseAdjustmentRecord
                                    {
                                        baseGlyphID = baseGlyph,
                                        baseGlyphAnchorPoint = new GlyphAnchorPoint { xCoordinate = bx * emScale, yCoordinate = by * emScale },
                                        markGlyphID = markGlyph,
                                        markPositionAdjustment = new MarkPositionAdjustment { xPositionAdjustment = mx * emScale, yPositionAdjustment = my * emScale }
                                    });
                                }
                            }
                        }
                    }
                    else if (lType == 6) // MarkToMark
                    {
                        var mark1Cov = ReadCoverage(d, subOff + ReadU16(d, subOff + 2));
                        var mark2Cov = ReadCoverage(d, subOff + ReadU16(d, subOff + 4));
                        int classCount = ReadU16(d, subOff + 6);
                        int mark1ArrayOff = subOff + ReadU16(d, subOff + 8);
                        int mark2ArrayOff = subOff + ReadU16(d, subOff + 10);

                        for (int m1 = 0; m1 < mark1Cov.Length; m1++)
                        {
                            uint combGlyph = mark1Cov[m1];
                            if (!packedGlyphs.Contains(combGlyph)) continue;

                            int m1Rec = mark1ArrayOff + 2 + m1 * 4;
                            int m1Class = ReadU16(d, m1Rec);
                            int m1AnchorOff = mark1ArrayOff + ReadU16(d, m1Rec + 2);
                            short m1x = ReadS16(d, m1AnchorOff + 2);
                            short m1y = ReadS16(d, m1AnchorOff + 4);

                            for (int m2 = 0; m2 < mark2Cov.Length; m2++)
                            {
                                uint baseMarkGlyph = mark2Cov[m2];
                                if (!packedGlyphs.Contains(baseMarkGlyph)) continue;

                                int m2AnchorRel = ReadU16(d, mark2ArrayOff + 2 + (m2 * classCount + m1Class) * 2);
                                if (m2AnchorRel == 0) continue;
                                int m2AnchorOff = mark2ArrayOff + m2AnchorRel;
                                short m2x = ReadS16(d, m2AnchorOff + 2);
                                short m2y = ReadS16(d, m2AnchorOff + 4);

                                uint key = (combGlyph << 16) | baseMarkGlyph;
                                if (m2mKeys.Add(key))
                                {
                                    m2mList.Add(new TMPro.MarkToMarkAdjustmentRecord
                                    {
                                        baseMarkGlyphID = baseMarkGlyph,
                                        baseMarkGlyphAnchorPoint = new GlyphAnchorPoint { xCoordinate = m2x * emScale, yCoordinate = m2y * emScale },
                                        combiningMarkGlyphID = combGlyph,
                                        combiningMarkPositionAdjustment = new MarkPositionAdjustment { xPositionAdjustment = m1x * emScale, yPositionAdjustment = m1y * emScale }
                                    });
                                }
                            }
                        }
                    }
                }
            }

            var pairList = new List<GlyphPairAdjustmentRecord>();
            uint[] toneGlyphs = { 735, 738, 741, 744, 746 };
            float saraAmYLift = (900 - 576) * emScale;
            float saraAmXShift = (-85 - (-71)) * emScale;
            foreach (uint tg in toneGlyphs)
            {
                var first = new GlyphAdjustmentRecord(tg, new GlyphValueRecord(saraAmXShift, saraAmYLift, 0, 0));
                var second = new GlyphAdjustmentRecord(489, new GlyphValueRecord(0, 0, 0, 0));
                pairList.Add(new GlyphPairAdjustmentRecord(first, second));
            }

            fontAsset.fontFeatureTable.MarkToBaseAdjustmentRecords = m2bList;
            fontAsset.fontFeatureTable.MarkToMarkAdjustmentRecords = m2mList;
            fontAsset.fontFeatureTable.glyphPairAdjustmentRecords = pairList;
            fontAsset.fontFeatureTable.SortMarkToBaseAdjustmentRecords();
            fontAsset.fontFeatureTable.SortMarkToMarkAdjustmentRecords();
            fontAsset.fontFeatureTable.SortGlyphPairAdjustmentRecords();

            var so = new SerializedObject(fontAsset);
            var incProp = so.FindProperty("m_CreationSettings.includeFontFeatures");
            if (incProp != null) incProp.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            fontAsset.ReadFontAssetDefinition();
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
        }

        public static void EnableOpenTypeFeaturesInProject()
        {
            var sarabun = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            var tmpSettings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (tmpSettings != null)
            {
                var soSettings = new SerializedObject(tmpSettings);
                EnsureFontFeatures(soSettings);
                var defFont = soSettings.FindProperty("m_defaultFontAsset");
                if (defFont != null && sarabun != null && defFont.objectReferenceValue != sarabun)
                {
                    defFont.objectReferenceValue = sarabun;
                    soSettings.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorUtility.SetDirty(tmpSettings);
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_LangkaLens" });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("XR_Submenu_System.prefab")) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                var texts = go.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length == 0) continue;

                foreach (var t in texts)
                {
                    var so = new SerializedObject(t);
                    EnsureFontFeatures(so);
                    EditorUtility.SetDirty(t);
                }
                PrefabUtility.SavePrefabAsset(go);
            }

            string origScenePath = EditorSceneManager.GetActiveScene().path;
            string[] scenePaths = {
                "Assets/_LangkaLens/_Scenes/Main_Menu_Scene.unity",
                "Assets/_LangkaLens/_Scenes/MR_Scene.unity",
                "Assets/_LangkaLens/_Scenes/VR_Tour_Scene.unity",
                "Assets/_LangkaLens/_Scenes/VR_Museum_Scene.unity"
            };

            foreach (string sp in scenePaths)
            {
                if (!File.Exists(sp)) continue;
                var scene = EditorSceneManager.OpenScene(sp, OpenSceneMode.Single);
                foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var so = new SerializedObject(t);
                    EnsureFontFeatures(so);
                    EditorUtility.SetDirty(t);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(origScenePath) && EditorSceneManager.GetActiveScene().path != origScenePath)
            {
                EditorSceneManager.OpenScene(origScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
        }

        private static void EnsureFontFeatures(SerializedObject so)
        {
            var af = so.FindProperty("m_ActiveFontFeatures");
            if (af == null) return;

            int[] required = { (int)OTL_FeatureTag.kern, (int)OTL_FeatureTag.mark, (int)OTL_FeatureTag.mkmk };
            var current = new List<int>();
            for (int i = 0; i < af.arraySize; i++)
                current.Add(af.GetArrayElementAtIndex(i).intValue);

            bool changed = false;
            foreach (int tag in required)
            {
                if (!current.Contains(tag))
                {
                    current.Add(tag);
                    changed = true;
                }
            }

            if (changed)
            {
                af.ClearArray();
                for (int i = 0; i < current.Count; i++)
                {
                    af.InsertArrayElementAtIndex(i);
                    af.GetArrayElementAtIndex(i).intValue = current[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static ushort ReadU16(byte[] d, int o) => (ushort)((d[o] << 8) | d[o + 1]);
        private static short ReadS16(byte[] d, int o) => (short)((d[o] << 8) | d[o + 1]);
        private static int ReadU32(byte[] d, int o) => (d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3];

        private static ushort[] ReadCoverage(byte[] d, int off)
        {
            int fmt = ReadU16(d, off);
            if (fmt == 1)
            {
                int cnt = ReadU16(d, off + 2);
                ushort[] res = new ushort[cnt];
                for (int i = 0; i < cnt; i++) res[i] = ReadU16(d, off + 4 + i * 2);
                return res;
            }
            else if (fmt == 2)
            {
                int rCnt = ReadU16(d, off + 2);
                var list = new List<ushort>();
                for (int r = 0; r < rCnt; r++)
                {
                    ushort start = ReadU16(d, off + 4 + r * 6);
                    ushort end = ReadU16(d, off + 4 + r * 6 + 2);
                    for (ushort g = start; g <= end; g++) list.Add(g);
                }
                return list.ToArray();
            }
            return new ushort[0];
        }
    }
}
