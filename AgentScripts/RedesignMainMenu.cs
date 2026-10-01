using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Linq;

// Run through the connected Unity Editor's RunCommand tool, outside Assets.
internal class CommandScript : IRunCommand
{
    ExecutionResult changes;
    TMP_FontAsset font;
    Color cream = Hex("F6E8D0"), brass = Hex("BB925C"), ink = Hex("32251F");
    static Color Hex(string value) { Color c; ColorUtility.TryParseHtmlString("#" + value, out c); return c; }
    RectTransform Place(Transform t, float x, float y, float w, float h)
    {
        var r = (RectTransform)t; changes.RegisterObjectModification(r);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
        r.localScale = Vector3.one; r.localRotation = Quaternion.identity;
        r.sizeDelta = new Vector2(w, h); r.anchoredPosition3D = new Vector3(x, y, 0);
        return r;
    }
    void ColorImage(Transform t, Color color)
    {
        var im = t.GetComponent<UnityEngine.UI.Image>(); changes.RegisterObjectModification(im);
        im.sprite = null; im.type = UnityEngine.UI.Image.Type.Simple; im.color = color;
        im.raycastTarget = t.GetComponent<UnityEngine.UI.Button>() != null;
    }
    Transform Bar(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false);
        changes.RegisterObjectCreation(go); Place(go.transform, x, y, w, h); ColorImage(go.transform, color);
        return go.transform;
    }
    void TextStyle(TMP_Text text, float x, float y, float w, float h, float size, Color color)
    {
        Place(text.transform, x, y, w, h); changes.RegisterObjectModification(text);
        text.font = font; text.enableAutoSizing = false; text.fontSize = size;
        text.color = color; text.alignment = TextAlignmentOptions.Center;
        text.margin = Vector4.zero; text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow; text.raycastTarget = false;
    }
    void Label(Transform parent, string name, string value, float x, float y, float w, float h, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false); changes.RegisterObjectCreation(go);
        var text = go.GetComponent<TextMeshProUGUI>(); text.text = value; TextStyle(text,x,y,w,h,size,color);
    }
    void Button(Transform t, float x, float y, float w, float h, Color color)
    {
        Place(t,x,y,w,h); ColorImage(t,color);
        var b = t.GetComponent<UnityEngine.UI.Button>(); changes.RegisterObjectModification(b);
        b.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        var colors = b.colors; colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.28f,1.22f,1.12f,1); colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.78f,.72f,.65f,1); colors.disabledColor = new Color(.55f,.5f,.45f,.65f);
        colors.colorMultiplier = 1; colors.fadeDuration = .12f; b.colors = colors;
        Bar(t,"BrassTopEdge",0,h/2-3,w,6,brass);
    }
    void Raw(Transform t, float x, float y, float maxW, float maxH)
    {
        var image = t.GetComponent<UnityEngine.UI.RawImage>(); changes.RegisterObjectModification(image);
        float aspect = image.texture != null ? (float)image.texture.width / image.texture.height : 1;
        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(image.texture)) as TextureImporter;
        if (importer != null) { int sourceW, sourceH; importer.GetSourceTextureWidthAndHeight(out sourceW, out sourceH); aspect = (float)sourceW / sourceH; }
        float w = Mathf.Min(maxW,maxH*aspect); Place(t,x,y,w,w/aspect);
        image.color = Color.white; image.raycastTarget = false;
    }
    public void Execute(ExecutionResult result)
    {
        changes=result;
        var scene=EditorSceneManager.GetActiveScene();
        if(Application.isPlaying || scene.path!="Assets/_LangkaLens/_Scenes/Main_Menu_Scene.unity") throw new System.InvalidOperationException("Open the main menu in Edit mode.");
        var root=GameObject.Find("MainCanvas").transform;
        var panel=root.Find("Panel"); var menu=panel.Find("mainMenuPanel"); var language=panel.Find("languagePanel");
        if(panel.Find("HeritageFrameTop")!=null) throw new System.InvalidOperationException("Redesign already applied.");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Sarabun-Medium SDF.asset");
        if(font==null) throw new System.InvalidOperationException("Existing bilingual font missing.");
        Place(root,0,0,2400,1500); root.localScale=Vector3.one*.001f; root.position=new Vector3(0,-.10f,2.6f);
        var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>(); if(scaler!=null) result.DestroyObject(scaler);
        Place(panel,0,0,2400,1500); ColorImage(panel,Hex("2C201B"));
        // Shared architectural frame: a restrained brick frieze, brass rules and parchment inset.
        Bar(panel,"HeritageFrameTop",0,726,2352,3,brass).SetAsFirstSibling();
        Bar(panel,"HeritageFrameBottom",0,-726,2352,3,brass).SetAsFirstSibling();
        Bar(panel,"HeritageFrameLeft",-1176,0,3,1452,brass).SetAsFirstSibling();
        Bar(panel,"HeritageFrameRight",1176,0,3,1452,brass).SetAsFirstSibling();
        Bar(panel,"BrickFrieze",0,680,2270,44,Hex("623C30")).SetAsFirstSibling();
        for(int i=0;i<16;i++) Bar(panel,"BrickJoint"+i,-1050+i*140,680,3,44,Hex("2C201B"));
        Place(menu,0,0,2240,1360); Place(language,0,0,2240,1360);
        TextStyle(menu.Find("Title").GetComponent<TMP_Text>(),0,440,2100,150,90,cream);
        Bar(menu,"TitleRuleLeft",-625,324,800,2,brass);
        Bar(menu,"TitleRuleRight",625,324,800,2,brass);
        var diamond=Bar(menu,"TitleSeal",0,324,18,18,brass); diamond.localRotation=Quaternion.Euler(0,0,45);
        var lang=menu.Find("btn_Lang"); Button(lang,-950,580,300,110,Hex("533A2D"));
        Raw(lang.Find("RawImage"),-58,-2,125,64);
        TextStyle(lang.Find("Text (TMP)").GetComponent<TMP_Text>(),76,-2,118,75,44,cream);
        var exit=menu.Find("btn_Exit"); Button(exit,910,580,380,110,Hex("533A2D"));
        TextStyle(exit.Find("Text (TMP)").GetComponent<TMP_Text>(),-28,0,285,85,38,cream);
        Raw(exit.Find("RawImage"),140,0,42,42);
        string[] modes={"btn_VRT","btn_VRM","btn_MR"};
        string[] colors={"754638","654031","553B2E"};
        for(int i=0;i<3;i++)
        {
            var card=menu.Find(modes[i]); Button(card,(i-1)*730,0,660,530,Hex(colors[i]));
            Bar(card,"IconMedallion",0,105,220,184,cream).SetAsFirstSibling();
            Raw(card.Find("RawImage"),0,105,166,142);
            var labels=card.GetComponentsInChildren<TMP_Text>(true);
            foreach(var text in labels) TextStyle(text,0,text.gameObject.activeSelf?-88:-177,596,text.gameObject.activeSelf?130:84,text.gameObject.activeSelf?48:32,cream);
            Bar(card,"CardBaseline",0,-204,90,4,brass);
            Label(card,"ChapterNumber","0"+(i+1),-260,211,85,50,34,cream);
        }
        // Keep Banner and its existing localized logo components in place.
        var banner=menu.Find("Banner"); Place(banner,0,-510,2180,330); ColorImage(banner,Hex("FFFBF2"));
        Bar(banner,"SponsorTopRule",0,164,2180,5,brass);
        Bar(banner,"SponsorBottomRule",0,-164,2180,5,brass);
        Label(banner,"SponsorHeading","PROJECT PARTNERS  /  ผู้สนับสนุนโครงการ",0,115,2020,66,38,ink);
        Raw(banner.Find("Comsci_LOGO"),-755,-34,510,155);
        Raw(banner.Find("PSU_LOGO"),-270,-34,200,206);
        Raw(banner.Find("TSRI_LOGO"),130,-34,230,206);
        Raw(banner.Find("ThaiRise_LOGO"),695,-34,535,200);
        foreach(float x in new[]{-455f,-55f,360f}) Bar(banner,"SponsorDivider"+x,x,-34,2,165,Hex("E0D1BC"));
        TextStyle(language.Find("Title").GetComponent<TMP_Text>(),0,440,1900,150,90,cream);
        Bar(language,"LanguageTitleRule",0,324,1600,2,brass);
        foreach(string name in new[]{"btn_EN","btn_TH"})
        {
            var b=language.Find(name); Button(b,name=="btn_EN"?-400:400,10,670,470,Hex("754638"));
            Raw(b.Find("RawImage"),0,55,420,210);
            TextStyle(b.Find("Text (TMP)").GetComponent<TMP_Text>(),0,-140,590,95,60,cream);
        }
        var ok=language.Find("btn_OK"); Button(ok,0,-390,450,145,Hex("78523A"));
        TextStyle(ok.Find("Text (TMP)").GetComponent<TMP_Text>(),0,0,410,120,58,cream);
        // Non-interactive graphics never intercept the existing ISDK pointer.
        foreach(var g in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        { result.RegisterObjectModification(g); g.raycastTarget=g.GetComponent<UnityEngine.UI.Button>()!=null; }
        Canvas.ForceUpdateCanvases(); EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject=root.gameObject;
        result.Log("Historical menu restyled. All eight existing buttons, localized labels and sponsor texture bindings retained.");
    }
}
