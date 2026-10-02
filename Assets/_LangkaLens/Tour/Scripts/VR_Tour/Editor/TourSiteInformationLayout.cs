using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Shared by initial setup and later theme passes so the reading area stays intact.
public static class TourSiteInformationLayout
{
    [MenuItem("LangkaLens/Tour/Update Site Information Panels")]
    public static void UpdatePanels()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != TourReconstructionSetup.ScenePath || scene.isDirty)
            throw new InvalidOperationException("Open and save VR_Tour_Scene outside Play Mode before updating its panels.");

        string path = TourReconstructionSetup.PrefabFolder + "/TourInspectionPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Apply(root);
            if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                throw new InvalidOperationException("Could not save the tour information panel.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        TourReconstructionDescriptions.Apply();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the tour scene.");
        Debug.Log("Four bilingual site summaries applied; panel zoom buttons removed. Controller scaling retained.");
    }

    public static void Apply(GameObject root)
    {
        Place(root.transform, 0, 0, 1200, 950, false);
        var panel = root.transform.Find("Panel");
        Place(panel, 0, 0, 1200, 950);
        var background = panel.GetComponent<Image>();
        background.color = Colour("2C201B");
        background.sprite = null;
        background.raycastTarget = false;

        foreach (string name in new[] { "ZoomInButton", "ZoomOutButton" })
        {
            var button = panel.Find(name);
            if (button != null) UnityEngine.Object.DestroyImmediate(button.gameObject);
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/Sarabun-Medium SDF.asset");
        if (font == null) throw new InvalidOperationException("Sarabun font is required for the bilingual tour panel.");
        var title = panel.Find("Title").GetComponent<TMP_Text>();
        Place(title.transform, -102, 374, 880, 124);
        Style(title, font, 48, Colour("F6E8D0"));
        title.alignment = TextAlignmentOptions.Left;

        var close = panel.Find("Back_btn");
        Place(close, 480, 380, 160, 110);
        close.GetComponent<Image>().color = Colour("533A2D");
        var label = close.GetComponentInChildren<TMP_Text>(true);
        Place(label.transform, 0, 0, 150, 100);
        Style(label, font, 36, Colour("F6E8D0"));

        var reading = Decoration(panel, "HeritageReadingPanel", 0, -48, 1110, 760, Colour("FFFBF2"));
        reading.SetAsFirstSibling();
        var description = panel.Find("Description").GetComponent<TMP_Text>();
        Place(description.transform, 0, -48, 1030, 710);
        Style(description, font, 32, Colour("32251F"));
        description.alignment = TextAlignmentOptions.TopLeft;
        description.textWrappingMode = TextWrappingModes.Normal;
        description.overflowMode = TextOverflowModes.Overflow;
        description.richText = true;
        description.paragraphSpacing = 0;
        var fitter = description.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        Color brass = Colour("BB925C");
        Decoration(panel, "HeritageTopRule", 0, 461, 1172, 3, brass);
        Decoration(panel, "HeritageBottomRule", 0, -461, 1172, 3, brass);
        Decoration(panel, "HeritageLeftRule", -586, 0, 3, 922, brass);
        Decoration(panel, "HeritageRightRule", 586, 0, 3, 922, brass);
        Decoration(panel, "HeritageHeadingRule", 0, 295, 1110, 3, brass);
    }

    private static Color Colour(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var colour);
        return colour;
    }

    private static void Place(Transform transform, float x, float y, float width, float height, bool resetScale = true)
    {
        var rect = (RectTransform)transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition3D = new Vector3(x, y, 0);
        if (resetScale) rect.localScale = Vector3.one;
    }

    private static void Style(TMP_Text text, TMP_FontAsset font, float size, Color colour)
    {
        text.font = font;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.color = colour;
        text.raycastTarget = false;
    }

    private static Transform Decoration(Transform parent, string name, float x, float y, float width, float height, Color colour)
    {
        var item = parent.Find(name);
        if (item == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            item = go.transform;
        }
        Place(item, x, y, width, height);
        var image = item.GetComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = colour;
        image.raycastTarget = false;
        var layout = item.GetComponent<LayoutElement>();
        if (layout != null) layout.ignoreLayout = true;
        return item;
    }
}
