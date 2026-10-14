using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;

/// <summary>One-time migration of the existing shared guide; keeps its original text pages intact.</summary>
public static class IllustratedGuideSetup
{
    public const string PrefabPath = "Assets/_LangkaLens/Mixed/3D Models/Prefabs/Center/UserGuide_Canvas.prefab";
    private const string Tables = "Assets/_LangkaLens/Misc/Localization/Tables/";
    private static readonly string[,] Copy =
    {
        { "Point and select", "ชี้และเลือก", "Point at a button, then press the index trigger to select it.", "ชี้ไปที่ปุ่ม แล้วกดไกด้วยนิ้วชี้เพื่อเลือก" },
        { "Choose an experience", "เลือกประสบการณ์", "Choose Mixed Reality, VR Tour, or VR Museum from the main menu.", "เลือก Mixed Reality, VR Tour หรือ VR Museum จากเมนูหลัก" },
        { "Open or hide the tablet", "เปิดหรือซ่อนแท็บเล็ต", "Press the Menu button on the left controller to open or hide the tablet. Select Home on the tablet to return to the main menu.", "กดปุ่ม Menu บนคอนโทรลเลอร์ซ้ายเพื่อเปิดหรือซ่อนแท็บเล็ต เลือก Home บนแท็บเล็ตเพื่อกลับเมนูหลัก" },
        { "Change language", "เปลี่ยนภาษา", "Select Change Language on the tablet to switch between English and Thai.", "เลือก Change Language บนแท็บเล็ตเพื่อสลับระหว่างภาษาอังกฤษและภาษาไทย" },
        { "Position the entrance outline", "วางโครงร่างทางเข้า", "Hold the RIGHT GRIP and move the controller to match the outline to the real entrance sign or table.", "กดปุ่ม GRIP ด้านขวาค้างไว้ แล้วขยับคอนโทรลเลอร์ให้โครงร่างตรงกับป้ายหรือโต๊ะทางเข้าจริง" },
        { "Fine-tune the alignment", "ปรับแนวให้ตรง", "Keep holding RIGHT GRIP. Move the right stick left/right to rotate, or up/down to adjust reach. Match the edges and height.", "กด RIGHT GRIP ค้างไว้ โยกจอยสติ๊กขวาซ้าย/ขวาเพื่อหมุน หรือขึ้น/ลงเพื่อปรับระยะ ให้ขอบและความสูงตรงกัน" },
        { "Release grip and confirm", "ปล่อย GRIP และยืนยัน", "Release RIGHT GRIP, then press A. Wait for the alignment to lock before exploring.", "ปล่อย RIGHT GRIP แล้วกด A รอให้การจัดแนวเสร็จสิ้นก่อนเริ่มสำรวจ" },
        { "Show or hide the restoration", "แสดงหรือซ่อนแบบจำลอง", "After alignment, press A to show or hide the restoration model.", "หลังจัดแนวเสร็จ กด A เพื่อแสดงหรือซ่อนแบบจำลองการบูรณะ" },
        { "View site information", "ดูข้อมูลโบราณสถาน", "While the model is visible, press B to show or hide site information. Use the page buttons to read more.", "ขณะแสดงแบบจำลอง กด B เพื่อแสดงหรือซ่อนข้อมูลโบราณสถาน ใช้ปุ่มเปลี่ยนหน้าเพื่ออ่านเพิ่มเติม" },
        { "Realign the site", "จัดแนวโบราณสถานใหม่", "Hold X for two seconds to align again.\nTo return Home, tap X twice, confirming the prompt, or use Home on the tablet.", "กด X ค้างไว้สองวินาทีเพื่อจัดแนวใหม่\nหากต้องการกลับเมนูหลัก ให้กด X แล้วกด X อีกครั้งเมื่อมีข้อความยืนยัน หรือเลือก Home บนแท็บเล็ต" },
        { "Select a location", "เลือกสถานที่", "Select an area from the tour menu to begin exploring.", "เลือกพื้นที่จากเมนูทัวร์เพื่อเริ่มสำรวจ" },
        { "Aim at a navigation arrow", "เล็งไปที่ลูกศรนำทาง", "Push the LEFT STICK forward, then aim the arc at a navigation arrow until it highlights.", "โยกจอยสติ๊กซ้ายไปข้างหน้า แล้วเล็งเส้นโค้งไปที่ลูกศรนำทางจนลูกศรเปลี่ยนสี" },
        { "Release to travel", "ปล่อยเพื่อเดินทาง", "Release the LEFT STICK while an arrow is highlighted to travel there. Pull backward, or release without a selected arrow, to cancel.", "ปล่อยจอยสติ๊กซ้ายขณะลูกศรเปลี่ยนสีเพื่อเดินทางไปจุดนั้น โยกกลับหรือปล่อยโดยไม่เลือกลูกศรเพื่อยกเลิก" },
        { "Zoom the panorama", "ซูมภาพพาโนรามา", "Hold B to zoom in. Hold A to zoom out.", "กด B ค้างไว้เพื่อซูมเข้า กด A ค้างไว้เพื่อซูมออก" },
        { "Inspect a reconstruction", "ตรวจดูแบบจำลอง", "Where inspection is available, press Y to enter or leave it. X also exits inspection. Navigation pauses while inspecting.", "เมื่อมีแบบจำลองให้ตรวจดู กด Y เพื่อเข้าหรือออกจากโหมดตรวจดู กด X เพื่อออกได้เช่นกัน การเดินทางจะหยุดระหว่างตรวจดู" },
        { "Move or turn the reconstruction", "ย้ายหรือหมุนแบบจำลอง", "During inspection, hold GRIP on either controller and move or turn your hand to adjust the model.", "ขณะตรวจดู กด GRIP บนคอนโทรลเลอร์ข้างใดก็ได้ค้างไว้ แล้วขยับหรือหมุนมือเพื่อปรับแบบจำลอง" },
        { "Rotate with the thumbstick", "หมุนด้วยจอยสติ๊ก", "During inspection, move the RIGHT STICK left or right to rotate the model.", "ขณะตรวจดู โยกจอยสติ๊กขวาไปทางซ้ายหรือขวาเพื่อหมุนแบบจำลอง" },
        { "Reset the inspection model", "คืนค่าแบบจำลอง", "Click the RIGHT STICK to reset the model's position, rotation, and size.", "กดจอยสติ๊กขวาลงเพื่อคืนค่าตำแหน่ง การหมุน และขนาดของแบบจำลอง" },
        { "Enlarge or reduce the reconstruction", "ขยายหรือย่อแบบจำลอง", "Hold B to enlarge; hold A to reduce.\nOutside inspection, press X for the tour menu. Use Home on the tablet to return to the main menu.", "กด B ค้างไว้เพื่อขยาย กด A ค้างไว้เพื่อย่อ\nเมื่อออกจากโหมดตรวจดู กด X เพื่อเปิดเมนูทัวร์ เลือก Home บนแท็บเล็ตเพื่อกลับเมนูหลัก" },
        { "Move around the gallery", "เดินชมพิพิธภัณฑ์", "Use the LEFT STICK to move and the RIGHT STICK to turn. Look for an exhibit's information icon.", "ใช้จอยสติ๊กซ้ายเพื่อเดิน และจอยสติ๊กขวาเพื่อหัน มองหาไอคอนข้อมูลของวัตถุจัดแสดง" },
        { "Select an exhibit", "เลือกวัตถุจัดแสดง", "Point at an exhibit's information icon and press the index trigger to inspect it.", "ชี้ไปที่ไอคอนข้อมูลของวัตถุจัดแสดง แล้วกดไกด้วยนิ้วชี้เพื่อเข้าสู่โหมดตรวจดู" },
        { "Grip and rotate the artifact", "จับและหมุนโบราณวัตถุ", "During inspection, hold GRIP on either controller and turn your hand to rotate the artifact.", "ขณะตรวจดู กด GRIP บนคอนโทรลเลอร์ข้างใดก็ได้ค้างไว้ แล้วหมุนมือเพื่อหมุนโบราณวัตถุ" },
        { "Rotate and reset supported exhibits", "หมุนและคืนค่าวัตถุที่รองรับ", "On supported exhibits, move the RIGHT STICK left/right to rotate. Click it to reset position, rotation, and size.", "สำหรับวัตถุที่รองรับ โยกจอยสติ๊กขวาซ้าย/ขวาเพื่อหมุน กดจอยสติ๊กลงเพื่อคืนค่าตำแหน่ง การหมุน และขนาด" },
        { "Enlarge or reduce the artifact", "ขยายหรือย่อโบราณวัตถุ", "During inspection, hold B to enlarge the artifact or hold A to reduce it.", "ขณะตรวจดู กด B ค้างไว้เพื่อขยายโบราณวัตถุ หรือกด A ค้างไว้เพื่อย่อ" },
        { "Compare alternate models", "เปรียบเทียบแบบจำลอง", "Press Y to switch models when an alternate version is available.", "กด Y เพื่อสลับแบบจำลองเมื่อมีแบบจำลองอีกเวอร์ชันให้เปรียบเทียบ" },
        { "Leave artifact inspection", "ออกจากโหมดตรวจดู", "Press X to leave inspection and resume moving.\nOutside inspection, tap X and confirm with X again to return Home, or use Home on the tablet.", "กด X เพื่อออกจากโหมดตรวจดูและกลับมาเดินได้\nนอกโหมดตรวจดู กด X แล้วกด X อีกครั้งเพื่อยืนยันกลับเมนูหลัก หรือเลือก Home บนแท็บเล็ต" },
        { "Type and send a question", "พิมพ์และส่งคำถาม", "Type your question in the AI chat, then select Send.\nClear removes displayed messages and cancels the current response.", "พิมพ์คำถามในแชต AI แล้วเลือก Send\nClear จะลบข้อความที่แสดงและยกเลิกคำตอบที่กำลังสร้าง" },
        { "Ask a question by voice", "ถามคำถามด้วยเสียง", "Select Speak, ask your question, then select Speak again to stop. Wait for the text to appear, then select Send.", "เลือก Speak แล้วพูดคำถาม เลือก Speak อีกครั้งเพื่อหยุด รอให้ข้อความปรากฏ แล้วเลือก Send" }
    };

    [MenuItem("LangkaLens/User Guide/Install Illustrated Pages")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before installing guide pages.");
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (asset.transform.Find("Panel/IllustratedContent") != null)
            throw new InvalidOperationException("Illustrated pages are already installed. Edit the prefab and UserGuide tables directly.");
        var files = Directory.GetFiles("Assets/Guide images", "*.jpg").OrderBy(p => p, StringComparer.Ordinal).ToArray();
        if (files.Length != 28) throw new InvalidOperationException("Expected exactly 28 numbered guide JPGs.");
        var sprites = new Sprite[28];
        for (int i = 0; i < files.Length; i++)
        {
            string path = files[i].Replace('\\', '/');
            if (!Path.GetFileName(path).StartsWith((i + 1).ToString("00") + "_"))
                throw new InvalidOperationException("Guide numbering is incomplete: " + path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.isReadable = false;
            importer.maxTextureSize = 2048;
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprites[i] == null) throw new InvalidOperationException("Sprite import failed: " + path);
        }

        var en = AssetDatabase.LoadAssetAtPath<StringTable>(Tables + "UserGuide_en.asset");
        var th = AssetDatabase.LoadAssetAtPath<StringTable>(Tables + "UserGuide_th.asset");
        for (int i = 0; i < 28; i++)
        {
            Add(en, th, "step" + (i + 1).ToString("00") + ".title", Copy[i, 0], Copy[i, 1]);
            Add(en, th, "step" + (i + 1).ToString("00") + ".body", Copy[i, 2], Copy[i, 3]);
        }
        Add(en, th, "gettingStarted", "Getting Started", "เริ่มต้นใช้งาน");
        Add(en, th, "aiGuide", "AI Guide", "คู่มือ AI");
        Add(en, th, "previous", "Previous", "ก่อนหน้า");
        Add(en, th, "next", "Next", "ถัดไป");

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var guide = root.GetComponent<UserGuideCanvas>();
            var panel = root.transform.Find("Panel");
            var templateTab = panel.Find("Tab_MR_Scene").GetComponent<UnityEngine.UI.Button>();
            var templateTitle = panel.Find("MR_Scene/Heading").GetComponent<TextMeshProUGUI>();
            var templateBody = panel.Find("MR_Scene/Steps/Instructions").GetComponent<TextMeshProUGUI>();
            var so = new SerializedObject(guide);
            var pages = so.FindProperty("pages");
            pages.arraySize = 5;
            // Preserve indices 0-2: existing tab events and scene overrides keep their meaning.
            string[] scenes = { "MR_Scene", "VR_Tour_Scene", "VR_Museum_Scene", "Main_Menu_Scene", "" };
            int[] first = { 4, 10, 19, 0, 26 };
            int[] count = { 6, 9, 7, 4, 2 };
            string[] tabKeys = { "tab1", "tab2", "tab3", "gettingStarted", "aiGuide" };
            string[] tabText = { "Mixed Reality", "VR Tour", "VR Museum", "Getting Started", "AI Guide" };
            int[] visualOrder = { 1, 2, 3, 0, 4 };
            for (int i = 0; i < 5; i++)
            {
                var page = pages.GetArrayElementAtIndex(i);
                page.FindPropertyRelative("sceneName").stringValue = scenes[i];
                UnityEngine.UI.Button tab;
                if (i < 3)
                {
                    tab = (UnityEngine.UI.Button)page.FindPropertyRelative("tab").objectReferenceValue;
                    ((GameObject)page.FindPropertyRelative("content").objectReferenceValue).SetActive(false);
                }
                else
                {
                    tab = UnityEngine.Object.Instantiate(templateTab, panel);
                    tab.name = i == 3 ? "Tab_GettingStarted" : "Tab_AIGuide";
                    page.FindPropertyRelative("content").objectReferenceValue = null;
                }
                Place((RectTransform)tab.transform, -984 + visualOrder[i] * 492, 620, 468, 125);
                tab.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                UnityEventTools.AddIntPersistentListener(tab.onClick, guide.ShowPage, i);
                var label = tab.GetComponentInChildren<TextMeshProUGUI>(true);
                Place(label.rectTransform, 0, 0, 440, 110);
                label.fontSize = 48;
                label.enableAutoSizing = false;
                label.raycastTarget = false;
                Localize(label, tabKeys[i], tabText[i]);
                page.FindPropertyRelative("tab").objectReferenceValue = tab;
                var steps = page.FindPropertyRelative("steps");
                steps.arraySize = count[i];
                for (int j = 0; j < count[i]; j++)
                {
                    int n = first[i] + j;
                    var step = steps.GetArrayElementAtIndex(j);
                    step.FindPropertyRelative("illustration").objectReferenceValue = sprites[n];
                    step.FindPropertyRelative("titleKey").stringValue = "step" + (n + 1).ToString("00") + ".title";
                    step.FindPropertyRelative("instructionKey").stringValue = "step" + (n + 1).ToString("00") + ".body";
                }
            }
            panel.Find("SharedControls").gameObject.SetActive(false);
            var content = new GameObject("IllustratedContent", typeof(RectTransform));
            content.transform.SetParent(panel, false);
            Place((RectTransform)content.transform, 0, 0, 2600, 1800);
            var heading = UnityEngine.Object.Instantiate(templateTitle, content.transform);
            heading.name = "StepTitle";
            Place(heading.rectTransform, 0, 463, 2380, 110);
            heading.fontSize = 64;
            heading.enableAutoSizing = false;
            heading.raycastTarget = false;
            Localize(heading, "step01.title", Copy[0, 0]);

            var picture = new GameObject("Illustration", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            picture.transform.SetParent(content.transform, false);
            Place((RectTransform)picture.transform, -455, -145, 1490, 1080);
            var img = picture.GetComponent<UnityEngine.UI.Image>();
            img.sprite = sprites[0];
            img.preserveAspect = true;
            img.raycastTarget = false;

            var body = UnityEngine.Object.Instantiate(templateBody, content.transform);
            body.name = "StepInstruction";
            Place(body.rectTransform, 795, -120, 780, 960);
            body.fontSize = 56;
            body.color = heading.color;
            body.enableAutoSizing = false;
            body.alignment = TextAlignmentOptions.MidlineLeft;
            body.raycastTarget = false;
            Localize(body, "step01.body", Copy[0, 2]);

            var prev = MakeButton(templateTab, content.transform, "Previous", -890, "previous", "Previous");
            var next = MakeButton(templateTab, content.transform, "Next", 890, "next", "Next");
            UnityEventTools.AddPersistentListener(prev.onClick, guide.PreviousStep);
            UnityEventTools.AddPersistentListener(next.onClick, guide.NextStep);
            var counter = UnityEngine.Object.Instantiate(templateBody, content.transform);
            counter.name = "StepCounter";
            var counterLocale = counter.GetComponent<LocalizeStringEvent>();
            if (counterLocale != null) UnityEngine.Object.DestroyImmediate(counterLocale);
            Place(counter.rectTransform, 0, -780, 460, 110);
            counter.fontSize = 56;
            counter.color = heading.color;
            counter.enableAutoSizing = false;
            counter.alignment = TextAlignmentOptions.Center;
            counter.raycastTarget = false;
            counter.text = "1 / 4";

            so.FindProperty("defaultPage").intValue = 3;
            so.FindProperty("illustratedContent").objectReferenceValue = content;
            so.FindProperty("illustration").objectReferenceValue = img;
            so.FindProperty("stepTitle").objectReferenceValue = heading.GetComponent<LocalizeStringEvent>();
            so.FindProperty("stepInstruction").objectReferenceValue = body.GetComponent<LocalizeStringEvent>();
            so.FindProperty("stepCounter").objectReferenceValue = counter;
            so.FindProperty("previousButton").objectReferenceValue = prev;
            so.FindProperty("nextButton").objectReferenceValue = next;
            so.ApplyModifiedPropertiesWithoutUndo();
            // Keep the prefab inactive so launchers can configure its parent and follow behaviour.
            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        EditorUtility.SetDirty(en);
        EditorUtility.SetDirty(th);
        EditorUtility.SetDirty(en.SharedData);
        AssetDatabase.SaveAssets();
        Debug.Log("Installed 28 illustrated steps in five sections, with English and Thai captions.");
    }

    private static void Add(StringTable en, StringTable th, string key, string english, string thai)
    {
        en.AddEntry(key, english);
        th.AddEntry(key, thai);
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
    }

    private static void Localize(TextMeshProUGUI label, string key, string fallback)
    {
        label.text = fallback;
        var localize = label.GetComponent<LocalizeStringEvent>();
        if (localize == null)
        {
            localize = label.gameObject.AddComponent<LocalizeStringEvent>();
            UnityEventTools.AddPersistentListener(localize.OnUpdateString, label.SetText);
        }
        localize.StringReference.SetReference("UserGuide", key);
    }

    private static UnityEngine.UI.Button MakeButton(UnityEngine.UI.Button template, Transform parent,
        string name, float x, string key, string text)
    {
        var button = UnityEngine.Object.Instantiate(template, parent);
        button.name = name;
        Place((RectTransform)button.transform, x, -780, 560, 130);
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        Place(label.rectTransform, 0, 0, 520, 110);
        label.fontSize = 56;
        Localize(label, key, text);
        return button;
    }
}

