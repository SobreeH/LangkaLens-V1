using System;
using System.Linq;
using LangkaLens.MR;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace LangkaLens.Editor
{
    public static class MRSiteInformationTools
    {
        private const string Table = "MR_UI";
        private static readonly string[,] Copy =
        {
            { "SiteInfo_Title", "Ban Jalae 3", "บ้านจาเละ หมายเลข 3" },
            { "SiteInfo_Subtitle", "Ancient Yarang | Pattani, Thailand", "เมืองโบราณยะรัง | ปัตตานี ประเทศไทย" },
            { "SiteInfo_AboutTitle", "The place", "รู้จักโบราณสถาน" },
            { "SiteInfo_About", "Ban Jalae 3 is a Buddhist brick monument in ancient Yarang, Pattani. Excavation revealed two building phases, with the first dated to around the 7th century CE.\n\nVotive tablets and miniature stupas found in the central chamber show its religious role.", "บ้านจาเละ หมายเลข 3 เป็นพุทธสถานก่ออิฐในเมืองโบราณยะรัง จังหวัดปัตตานี การขุดค้นพบการก่อสร้างสองสมัย โดยสมัยแรกสันนิษฐานว่าราวพุทธศตวรรษที่ 12\n\nพระพิมพ์และสถูปจำลองจำนวนมากที่พบในห้องกลางเป็นหลักฐานของการใช้พื้นที่เพื่อบูชา" },
            { "SiteInfo_EvidenceTitle", "Surviving evidence", "หลักฐานที่ยังคงอยู่" },
            { "SiteInfo_Evidence", "Look at the stepped base, projecting sides and window-like recesses. The excavation record describes these features, helping relate the model to the surviving ruin.\n\nThe books record a base about 13.5 x 13.6 m and a central chamber 5.2 x 5.2 m.", "ลองเปรียบเทียบฐานซ้อนชั้น มุขที่ยื่นออกมา และช่องตกแต่งคล้ายหน้าต่างกับซากจริง ลักษณะเหล่านี้ปรากฏในบันทึกการขุดค้น จึงช่วยเชื่อมโยงแบบจำลองกับหลักฐานที่เหลืออยู่\n\nหนังสือระบุฐานขนาดประมาณ 13.5 x 13.6 เมตร และห้องกลาง 5.2 x 5.2 เมตร" },
            { "SiteInfo_InterpretationTitle", "Reading the restoration", "ทำความเข้าใจรูปแบบสันนิษฐาน" },
            { "SiteInfo_Interpretation", "The complete upper structure has not survived. Its restored form is an interpretation; the books suggest a possible comparison with Candi Mendut in Java.\n\nThis display is fitted to the viewing space. Its size, upper tiers, colours and fine details are not a verified, exact replica.", "ส่วนบนของอาคารไม่เหลือครบ รูปแบบที่เติมขึ้นจึงเป็นการตีความ\nหนังสือเสนอว่าอาจมีรูปแบบคล้ายจันทิเมนดุตในชวา\n\nแบบจำลองนี้ปรับขนาดให้พอดีกับพื้นที่แสดง\nขนาด ส่วนยอด สี และรายละเอียดจึงไม่ใช่สำเนา\nที่ผ่านการยืนยันว่าตรงกับของเดิมทุกประการ" },
            { "SiteInfo_SourceAbout", "Sources: Yarang, p. 180\nSouthern Border Provinces, p. 66", "ที่มา: เมืองโบราณยะรัง หน้า 180\nพัฒนาการฯ 3 จังหวัดชายแดนใต้ หน้า 66" },
            { "SiteInfo_SourceEvidence", "Sources: Yarang, pp. 180, 192-194\nSouthern Border Provinces, p. 66", "ที่มา: เมืองโบราณยะรัง หน้า 180, 192-194\nพัฒนาการฯ 3 จังหวัดชายแดนใต้ หน้า 66" },
            { "SiteInfo_SourceInterpretation", "Sources: Yarang, p. 180\nSouthern Border Provinces, p. 66\n3D Model: Regional Office of Fine Arts No. 11, Songkhla", "ที่มา: เมืองโบราณยะรัง หน้า 180\nพัฒนาการฯ 3 จังหวัดชายแดนใต้ หน้า 66\nแบบจำลอง 3 มิติ: สำนักศิลปากรที่ ๑๑ สงขลา" },
            { "SiteInfo_Previous", "Previous", "ย้อนกลับ" },
            { "SiteInfo_Next", "Next", "ถัดไป" }
        };

        [MenuItem("Tools/LangkaLens/MR/Add Ban Jalae information panel")]
        public static void Setup()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.name != "MR_Scene")
                throw new InvalidOperationException("Open MR_Scene in Edit Mode first.");
            if (scene.GetRootGameObjects().Any(x => x.GetComponent<MRSiteInformationPanel>() != null))
                throw new InvalidOperationException("The information panel already exists; edit its children and MR_UI entries.");
            var spawner = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<MRModelSpawner>(true)).Single();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Sarabun-Medium SDF.asset");
            if (font == null || spawner.centerEyeCamera == null) throw new InvalidOperationException("Missing font or viewer.");
            PopulateTables();

            var root = new GameObject("BanJalaeInformation");
            Undo.RegisterCreatedObjectUndo(root, "Add Ban Jalae information panel");
            var controller = root.AddComponent<MRSiteInformationPanel>();
            controller.spawner = spawner;
            controller.head = spawner.centerEyeCamera;
            var canvasObject = new GameObject("BanJalaeInfoCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            canvasObject.layer = LayerMask.NameToLayer("UI");
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1400, 1180);
            rect.localScale = Vector3.one * 0.001f;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = controller.head.GetComponent<Camera>();
            controller.view = canvasObject;
            var background = canvasObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.035f, 0.075f, 0.08f, 1);
            background.raycastTarget = false;

            var accent = Rect("Accent", rect, 0, 0, 1400, 10);
            var accentImage = accent.gameObject.AddComponent<UnityEngine.UI.Image>();
            accentImage.color = new Color(0.82f, 0.65f, 0.37f, 1);
            accentImage.raycastTarget = false;
            Label(rect, "Title", "SiteInfo_Title", font, 60, 40, 1280, 90, 64, new Color(0.96f, 0.84f, 0.61f));
            Label(rect, "Subtitle", "SiteInfo_Subtitle", font, 60, 137, 1280, 75, 48, new Color(0.72f, 0.84f, 0.82f));
            string[] headings = { "SiteInfo_AboutTitle", "SiteInfo_EvidenceTitle", "SiteInfo_InterpretationTitle" };
            string[] bodies = { "SiteInfo_About", "SiteInfo_Evidence", "SiteInfo_Interpretation" };
            controller.pages = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                var page = Rect("Page" + (i + 1), rect, 0, 0, 1400, 1000);
                controller.pages[i] = page.gameObject;
                Label(page, "Heading", headings[i], font, 60, 238, 1280, 88, 56, new Color(0.96f, 0.84f, 0.61f));
                Label(page, "Body", bodies[i], font, 60, 340, 1280, 540, 48, new Color(0.92f, 0.94f, 0.91f));
                Label(page, "Source", i == 1 ? "SiteInfo_SourceEvidence" : i == 2 ? "SiteInfo_SourceInterpretation" : "SiteInfo_SourceAbout", font, 60, 910, 1280, 130, 48, new Color(0.72f, 0.84f, 0.82f));
                page.gameObject.SetActive(i == 0);
            }
            Button(rect, "Previous", "SiteInfo_Previous", font, 60, controller.PreviousPage);
            Button(rect, "Next", "SiteInfo_Next", font, 1000, controller.NextPage);
            var number = Label(rect, "PageNumber", null, font, 540, 1050, 320, 110, 48, new Color(0.92f, 0.94f, 0.91f));
            number.alignment = TextAlignmentOptions.Center;
            number.text = "1 / 3";
            controller.pageNumber = number;
            var flatForward = Vector3.ProjectOnPlane(controller.head.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
            root.transform.position = controller.head.position + Quaternion.LookRotation(flatForward) * controller.viewerOffset;
            root.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(root.transform.position - controller.head.position, Vector3.up));
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = canvasObject;
        }

        [MenuItem("Tools/LangkaLens/MR/Update site information translations")]
        public static void UpdateTranslations()
        {
            PopulateTables();
            UpdatePage3SourceReference();
        }

        private static void UpdatePage3SourceReference()
        {
            var page3 = GameObject.Find("Page3");
            if (page3 == null)
            {
                foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (go.name == "Page3" && go.scene.isLoaded)
                    {
                        page3 = go;
                        break;
                    }
                }
            }

            if (page3 == null) return;

            Transform sourceTrans = page3.transform.Find("Source");
            if (sourceTrans == null) return;

            var localize = sourceTrans.GetComponent<LocalizeStringEvent>();
            var tmp = sourceTrans.GetComponent<TextMeshProUGUI>();

            if (localize != null)
            {
                Undo.RecordObject(localize, "Update Page 3 Source Key");
                localize.StringReference = new LocalizedString(Table, "SiteInfo_SourceInterpretation");
                if (tmp != null)
                {
                    localize.OnUpdateString.RemoveAllListeners();
                    var setter = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), tmp, "set_text");
                    UnityEventTools.AddPersistentListener(localize.OnUpdateString, setter);
                    localize.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
                }
                localize.RefreshString();
                EditorUtility.SetDirty(localize);
            }

            if (tmp != null)
            {
                Undo.RecordObject(tmp, "Update Page 3 Source Text");
                string enText = Enumerable.Range(0, Copy.GetLength(0))
                    .Where(i => Copy[i, 0] == "SiteInfo_SourceInterpretation")
                    .Select(i => Copy[i, 1])
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(enText))
                {
                    tmp.text = enText;
                }
                EditorUtility.SetDirty(tmp);
            }

            EditorSceneManager.MarkSceneDirty(page3.scene);
        }

        public static void PopulateTables()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(Table);
            if (collection == null) throw new InvalidOperationException("MR_UI table collection is missing.");
            foreach (var table in collection.StringTables)
            {
                int column = table.LocaleIdentifier.Code == "th" ? 2 : table.LocaleIdentifier.Code == "en" ? 1 : -1;
                if (column < 0) throw new InvalidOperationException("Provide a translation for " + table.LocaleIdentifier.Code);
                Undo.RecordObject(table, "Add site information translations");
                Undo.RecordObject(collection.SharedData, "Add site information keys");
                for (int i = 0; i < Copy.GetLength(0); i++) table.AddEntry(Copy[i, 0], Copy[i, column]);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(typeof(MRSiteInformationTools), collection);
            AssetDatabase.SaveAssets();
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string key, TMP_FontAsset font,
            float x, float y, float width, float height, float size, Color color)
        {
            var rect = Rect(name, parent, x, y, width, height);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.enableAutoSizing = false;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            if (key != null)
            {
                text.text = Enumerable.Range(0, Copy.GetLength(0)).Where(i => Copy[i, 0] == key).Select(i => Copy[i, 1]).Single();
                var localized = rect.gameObject.AddComponent<LocalizeStringEvent>();
                localized.StringReference = new LocalizedString(Table, key);
                var setter = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), text, "set_text");
                UnityEventTools.AddPersistentListener(localized.OnUpdateString, setter);
                localized.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
                localized.RefreshString();
            }
            return text;
        }

        private static void Button(Transform parent, string name, string key, TMP_FontAsset font, float x, UnityAction action)
        {
            var rect = Rect(name, parent, x, 1050, 340, 110);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.14f, 0.25f, 0.25f, 1);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            UnityEventTools.AddPersistentListener(button.onClick, action);
            var label = Label(rect, "Label", key, font, 10, 0, 320, 110, 48, new Color(0.95f, 0.94f, 0.88f));
            label.alignment = TextAlignmentOptions.Center;
        }
    }
}
