using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

internal class CommandScript : IRunCommand
{
    ExecutionResult changes;
    TMP_FontAsset font;
    Color dark = Hex("2C201B"), brick = Hex("754638"), brown = Hex("533A2D"), brass = Hex("BB925C"), cream = Hex("F6E8D0"), paper = Hex("FFFBF2"), ink = Hex("32251F");
    static Color Hex(string value) { Color c; ColorUtility.TryParseHtmlString("#" + value, out c); return c; }
    void Touch(UnityEngine.Object obj) { changes.RegisterObjectModification(obj); }
    RectTransform Place(Transform t, float x, float y, float w, float h)
    {
        var r = (RectTransform)t; Touch(r); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f,.5f);
        r.localScale = Vector3.one; r.sizeDelta = new Vector2(w,h); r.anchoredPosition3D = new Vector3(x,y,0); return r;
    }
    void Stretch(Transform t, float left, float bottom, float right, float top)
    {
        var r=(RectTransform)t; Touch(r); r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.pivot=new Vector2(.5f,.5f);
        r.localScale=Vector3.one; r.offsetMin=new Vector2(left,bottom); r.offsetMax=new Vector2(-right,-top);
    }
    void Image(Transform t, Color color, bool square=true)
    {
        var im=t.GetComponent<UnityEngine.UI.Image>(); if(im==null)return; Touch(im); im.color=color;
        if(square){im.sprite=null;im.type=UnityEngine.UI.Image.Type.Simple;}
    }
    void Text(TMP_Text text, float size, Color color)
    {
        Touch(text); text.font=font; text.fontSize=size; text.enableAutoSizing=false; text.color=color; text.raycastTarget=false;
    }
    Transform Decoration(Transform parent,string name,float x,float y,float w,float h,Color color)
    {
        var existing=parent.Find(name); if(existing!=null){Place(existing,x,y,w,h);Image(existing,color);return existing;}
        var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); go.layer=parent.gameObject.layer;
        go.transform.SetParent(parent,false); changes.RegisterObjectCreation(go); Place(go.transform,x,y,w,h); Image(go.transform,color);
        go.GetComponent<UnityEngine.UI.Image>().raycastTarget=false; go.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout=true;return go.transform;
    }
    void Frame(Transform panel,float w,float h,bool brickBand=false)
    {
        Decoration(panel,"HeritageTopRule",0,h/2-14,w-28,3,brass);
        Decoration(panel,"HeritageBottomRule",0,-h/2+14,w-28,3,brass);
        Decoration(panel,"HeritageLeftRule",-w/2+14,0,3,h-28,brass);
        Decoration(panel,"HeritageRightRule",w/2-14,0,3,h-28,brass);
        if(brickBand) Decoration(panel,"HeritageBrickBand",0,h/2-37,w-60,20,brick);
    }
    void Button(Transform t, Color color)
    {
        var b=t.GetComponent<UnityEngine.UI.Button>(); if(b==null)return; Image(t,color); Touch(b);
        b.transition=UnityEngine.UI.Selectable.Transition.ColorTint;var cb=b.colors;cb.normalColor=Color.white;
        cb.highlightedColor=new Color(1.3f,1.22f,1.12f);cb.selectedColor=cb.highlightedColor;cb.pressedColor=new Color(.78f,.72f,.65f);
        cb.disabledColor=new Color(.65f,.58f,.5f,.7f);cb.colorMultiplier=1;cb.fadeDuration=.12f;b.colors=cb;
        var im=t.GetComponent<UnityEngine.UI.Image>();if(im!=null)im.raycastTarget=true;
    }
    void ColorSetting(MonoBehaviour component,string property,Color value)
    {
        if(component==null)return;var so=new SerializedObject(component);var p=so.FindProperty(property);if(p==null)return;Touch(component);p.colorValue=value;so.ApplyModifiedPropertiesWithoutUndo();
    }
    void Keyboard(GameObject root)
    {
        var panel=root.transform.Find("KeyboardPanel");Image(panel,dark);Frame(panel,2000,800);
        foreach(var b in panel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            Button(b.transform,b.name=="Enter"?brick:brown);
            var key=b.GetComponent<VirtualKeyboardKey>();ColorSetting(key,"restingColor",b.name=="Enter"?brick:brown);ColorSetting(key,"shiftedColor",Hex("A56C42"));
        }
        foreach(var text in panel.GetComponentsInChildren<TMP_Text>(true))Text(text,text.fontSize,cream);
        var preview=panel.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name=="Preview");if(preview!=null)Text(preview,preview.fontSize,cream);
    }
    void Chat(GameObject root)
    {
        var p=root.transform.Find("Panel");Image(p,dark);Frame(p,2000,1200,true);
        var title=p.Find("Title").GetComponent<TMP_Text>();Place(title.transform,-210,490,1310,110);Text(title,72,cream);title.alignment=TextAlignmentOptions.Left;
        Decoration(p,"HeritageHeadingRule",0,411,1860,3,brass);
        var scroll=p.Find("Scroll View");Place(scroll,0,65,1860,640);Image(scroll,paper);
        var view=scroll.Find("Viewport");Stretch(view,24,20,60,20);Image(view,Color.white,false);
        var content=view.Find("Content");var contentRT=(RectTransform)content;Touch(contentRT);contentRT.anchorMin=new Vector2(0,1);contentRT.anchorMax=Vector2.one;contentRT.pivot=new Vector2(.5f,1);contentRT.localScale=Vector3.one;contentRT.anchoredPosition=Vector2.zero;contentRT.sizeDelta=new Vector2(0,0);
        var layout=content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();Touch(layout);layout.padding=new RectOffset(18,18,12,12);layout.childControlWidth=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
        var textFitter=content.Find("Text").GetComponent<UnityEngine.UI.ContentSizeFitter>();Touch(textFitter);textFitter.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        var chatText=content.Find("Text").GetComponent<TMP_Text>();Text(chatText,56,ink);chatText.alignment=TextAlignmentOptions.TopLeft;
        var scrollbar=scroll.Find("Scrollbar Vertical");Stretch(scrollbar,1810,16,18,16);Image(scrollbar,Hex("E0D1BC"));Image(scrollbar.Find("Sliding Area/Handle"),brass);
        var input=p.Find("InputField (TMP)");Place(input,-260,-419,1340,240);Image(input,paper);
        Stretch(input.Find("Text Area"),24,18,24,18);
        foreach(var text in input.GetComponentsInChildren<TMP_Text>(true))Text(text,56,text.name=="Placeholder"?Hex("806B59"):ink);
        var field=input.GetComponent<TMP_InputField>();Touch(field);field.pointSize=56;field.caretColor=ink;field.selectionColor=new Color(.73f,.57f,.36f,.35f);
        ColorSetting(input.GetComponent<QuestKeyboardHandler>(),"activeBackgroundColor",cream);
        string[] names={"Speak_Button","Send_Button","Clear_Button"};
        for(int i=0;i<names.Length;i++)
        {
            var b=p.Find(names[i]);Place(b,i==2?765:710,i==0?-350:i==1?-500:490,i==2?330:430,i==2?110:135);Button(b,i==1?brick:brown);
            Place(b.Find("Image"),i==2?-112:-144,0,54,54);Image(b.Find("Image"),cream,false);
            var label=b.Find("Text (TMP)").GetComponent<TMP_Text>();Place(label.transform,32,0,i==2?230:312,100);Text(label,i==2?46:52,cream);
        }
        ColorSetting(root.GetComponent<GeminiChatUI>(),"userLabelColor",Hex("754638"));ColorSetting(root.GetComponent<GeminiChatUI>(),"guideLabelColor",Hex("533A2D"));
        ColorSetting(root.GetComponent<GeminiChatUI>(),"cancelButtonColor",Hex("8F4936"));ColorSetting(root.GetComponent<SpeechToTextHandler>(),"recordingButtonColor",Hex("A5543B"));
    }
    void Tablet(GameObject root)
    {
        var p=root.transform.Find("Panel");Image(p,dark);Frame(p,1200,1200,true);
        var lang=p.Find("btn_Lang");Place(lang,235,420,600,200);Button(lang,brown);
        Place(lang.Find("Icon"),-220,0,100,100);Image(lang.Find("Icon"),cream,false);
        var label=lang.Find("Text (TMP)").GetComponent<TMP_Text>();Place(label.transform,65,0,390,170);Text(label,48,cream);
        Decoration(p,"HeritageHeadingRule",0,285,1050,3,brass);
        foreach(string name in new[]{"btn_Chat","btn_home"})
        {
            var b=p.Find(name);Place(b,name=="btn_Chat"?-280:280,-25,480,490);Button(b,name=="btn_Chat"?brick:brown);
            Place(b.Find("Icon"),0,90,180,180);Image(b.Find("Icon"),cream,false);
            var text=b.Find("Text (TMP)").GetComponent<TMP_Text>();Place(text.transform,0,-126,430,142);Text(text,58,cream);
            Decoration(b,"HeritageCardRule",0,242,480,5,brass);
        }
        Decoration(p,"HeritageFooterRule",0,-390,650,3,brass);
    }
    void Hints(GameObject root)
    {
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)){Touch(text);text.color=cream;text.raycastTarget=false;}
        Image(root.transform.Find("Panel"),new Color(dark.r,dark.g,dark.b,.94f),false);
        var badge=root.transform.Find("MetaButtonHighlight");if(badge!=null){Image(badge,brick,false);var label=badge.GetComponentInChildren<TMP_Text>(true);Touch(label);label.color=cream;}
    }
    void Info(GameObject root)
    {
        var p=root.transform;Image(p,dark);Frame(p,1400,1180);
        var parchment=Decoration(p,"HeritageReadingPanel",0,-48,1320,790,paper);parchment.SetAsFirstSibling();
        Image(p.Find("Accent"),brass);
        Text(p.Find("Title").GetComponent<TMP_Text>(),64,cream);Text(p.Find("Subtitle").GetComponent<TMP_Text>(),40,cream);
        foreach(string page in new[]{"Page1","Page2","Page3"})foreach(var text in p.Find(page).GetComponentsInChildren<TMP_Text>(true))Text(text,text.fontSize,text.name=="Source"?Hex("6B5848"):ink);
        foreach(string b in new[]{"Previous","Next"}){Button(p.Find(b),brick);Text(p.Find(b).GetComponentInChildren<TMP_Text>(),48,cream);}
        Text(p.Find("PageNumber").GetComponent<TMP_Text>(),48,cream);
    }
    void Reminder(GameObject root)
    {
        Image(root.transform,dark);var r=(RectTransform)root.transform;Frame(root.transform,r.rect.width,r.rect.height);
        var highlight=root.transform.Find("Highlight");if(highlight!=null)Image(highlight,brass);
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)){Touch(text);text.color=cream;text.raycastTarget=false;}
    }
    // Compare functional configuration without storing credentials or changing scene-specific references.
    string Signature(Component component)
    {
        var sb=new StringBuilder();var so=new SerializedObject(component);var p=so.GetIterator();bool enter=true;
        while(p.NextVisible(enter))
        {
            enter=true;string n=p.propertyPath;
            if(p.propertyType==SerializedPropertyType.Color || n=="m_Colors" || n=="m_Transition" || n=="m_GlobalPointSize" || n=="m_CaretColor" || n=="m_SelectionColor") {enter=false;continue;}
            if(n=="m_Name"||n=="m_EditorHideFlags")continue;
            sb.Append(n).Append('=');
            switch(p.propertyType){case SerializedPropertyType.ObjectReference:sb.Append(p.objectReferenceValue?p.objectReferenceValue.GetInstanceID():0);break;case SerializedPropertyType.String:sb.Append(p.stringValue);break;case SerializedPropertyType.Boolean:sb.Append(p.boolValue);break;case SerializedPropertyType.Integer:case SerializedPropertyType.Enum:sb.Append(p.intValue);break;case SerializedPropertyType.Float:sb.Append(p.doubleValue);break;default:break;}
            sb.Append(';');
        }
        using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
    }
    Dictionary<Component,string> Snapshot(GameObject root)
    {
        var data=new Dictionary<Component,string>();foreach(var c in root.GetComponentsInChildren<Component>(true))
        {
            if(c==null)continue;bool custom=c is MonoBehaviour && c.GetType().Assembly.GetName().Name=="Assembly-CSharp";
            if(custom || c is UnityEngine.UI.Selectable || c is UnityEngine.UI.ScrollRect || c.GetType().Name=="LocalizeStringEvent")data[c]=Signature(c);
        }
        return data;
    }
    void Verify(Dictionary<Component,string> before)
    {
        foreach(var item in before)if(item.Key==null||Signature(item.Key)!=item.Value)throw new Exception("Functional configuration changed: "+(item.Key?item.Key.name:"deleted"));
    }
    void Prefab(string path,Action<GameObject> style)
    {
        var root=PrefabUtility.LoadPrefabContents(path);try{var before=Snapshot(root);style(root);Verify(before);PrefabUtility.SaveAsPrefabAsset(root,path);changes.Log("Updated shared prefab "+path+"; verified "+before.Count+" functional components.");}finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    TMP_Text LocalLabel(Transform parent,string name,string key,float x,float y,float w,float h,float size,Color color)
    {
        var existing=parent.Find(name);if(existing!=null)return existing.GetComponent<TMP_Text>();
        var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);changes.RegisterObjectCreation(go);
        var text=go.GetComponent<TextMeshProUGUI>();Place(go.transform,x,y,w,h);Text(text,size,color);text.alignment=TextAlignmentOptions.Center;
        var table=AssetDatabase.FindAssets("t:StringTable").Select(g=>AssetDatabase.LoadAssetAtPath<UnityEngine.Localization.Tables.StringTable>(AssetDatabase.GUIDToAssetPath(g))).First(t=>t.TableCollectionName=="VTour_UI"&&t.LocaleIdentifier.Code=="en");
        text.text=table.GetEntry(key).LocalizedValue;
        var local=go.AddComponent<UnityEngine.Localization.Components.LocalizeStringEvent>();local.StringReference.TableReference="VTour_UI";local.StringReference.TableEntryReference=key;
        UnityEditor.Events.UnityEventTools.AddPersistentListener<string>(local.OnUpdateString,text.SetText);
        return text;
    }
    void TourMenu(GameObject root)
    {
        var rt=(RectTransform)root.transform;Touch(rt);rt.sizeDelta=new Vector2(1800,1080);
        var panel=root.transform.Find("Panel");Place(panel,0,0,1800,1080);Image(panel,dark);Frame(panel,1800,1080,true);
        LocalLabel(panel,"HeritageTitle","vtour_menu_title",0,393,1660,100,64,cream);
        LocalLabel(panel,"HeritageSubtitle","vtour_menu_subtitle",0,305,1660,80,38,cream);
        Decoration(panel,"HeritageTitleRule",0,250,1640,3,brass);
        string[] names={"A2","A3","A8","A9"};string[] numbers={"02","03","08","09"};
        for(int i=0;i<4;i++)
        {
            var holder=panel.Find(names[i]);Place(holder,(i-1.5f)*420,-50,360,490);
            var button=holder.GetComponentInChildren<UnityEngine.UI.Button>(true).transform;Place(button,0,0,360,490);Button(button,i%2==0?brick:brown);
            var photo=button.Find("Image");Place(photo,0,55,320,230);Image(photo,Color.white,false);var graphic=photo.GetComponent<UnityEngine.UI.Image>();Touch(graphic);graphic.preserveAspect=true;graphic.raycastTarget=false;
            Decoration(button,"HeritagePhotoMount",0,55,330,240,paper).SetAsFirstSibling();
            Decoration(button,"HeritageCardRule",0,242,360,5,brass);
            var text=button.Find("Text (TMP)").GetComponent<TMP_Text>();Place(text.transform,0,-118,330,100);Text(text,42,cream);text.alignment=TextAlignmentOptions.Center;
            LocalLabel(button,"HeritageSiteNumber","vtour_site_num_"+numbers[i],0,204,324,50,28,cream);
            LocalLabel(button,"HeritageEnterLabel","vtour_btn_explore",0,-197,320,65,34,cream);
        }
        LocalLabel(panel,"HeritageHelp","vtour_menu_hint",0,-417,1670,75,34,cream);
    }
    void Inspection(GameObject root)
    {
        var rt=(RectTransform)root.transform;Touch(rt);rt.sizeDelta=new Vector2(1200,950);
        var panel=root.transform.Find("Panel");Place(panel,0,0,1200,950);Image(panel,dark);Frame(panel,1200,950);
        var title=panel.Find("Title").GetComponent<TMP_Text>();Place(title.transform,-102,374,880,124);Text(title,48,cream);title.alignment=TextAlignmentOptions.Left;
        var close=panel.Find("Back_btn");Place(close,480,380,160,110);Button(close,brown);var label=close.GetComponentInChildren<TMP_Text>(true);Place(label.transform,0,0,150,100);Text(label,36,cream);
        Decoration(panel,"HeritageHeadingRule",0,295,1110,3,brass);
        Decoration(panel,"HeritageReadingPanel",0,-10,1110,565,paper).SetAsFirstSibling();
        var description=panel.Find("Description").GetComponent<TMP_Text>();Place(description.transform,0,-10,1030,505);Text(description,38,ink);description.alignment=TextAlignmentOptions.TopLeft;
        var fitter=description.GetComponent<UnityEngine.UI.ContentSizeFitter>();if(fitter!=null){Touch(fitter);fitter.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;}
        foreach(var name in new[]{"ZoomOutButton","ZoomInButton"})
        {
            var button=panel.Find(name);Place(button,name=="ZoomOutButton"?-305:305,-377,505,115);Button(button,brick);
            var text=button.GetComponentInChildren<TMP_Text>(true);Place(text.transform,0,0,475,105);Text(text,40,cream);
        }
    }
    public void Execute(ExecutionResult result)
    {
        changes=result;var scene=EditorSceneManager.GetActiveScene();
        if(Application.isPlaying||scene.path!="Assets/_LangkaLens/_Scenes/VR_Tour_Scene.unity")throw new Exception("Open Tour Scene in edit mode.");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Sarabun-Medium SDF.asset");
        foreach(var lang in new[]{"en","th"})
        {
            var table=AssetDatabase.LoadAssetAtPath<UnityEngine.Localization.Tables.StringTable>("Assets/_LangkaLens/Tour/Localization/Tables/VTour_UI_"+lang+".asset");
            Touch(table);Touch(table.SharedData);table.AddEntry("vtour_main_attraction",lang=="en"?"MAIN ATTRACTION":"จุดเด่นหลัก");EditorUtility.SetDirty(table);EditorUtility.SetDirty(table.SharedData);
        }
        var menu=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(c=>c.gameObject.scene==scene&&c.name=="MenuCanvas");
        var before=Snapshot(menu.gameObject);var p=menu.transform.Find("Panel");
        var featured=p.Find("A3");Place(featured,-530,-85,580,610);
        var hero=featured.GetComponentInChildren<UnityEngine.UI.Button>(true).transform;Place(hero,0,0,580,610);Button(hero,brick);
        Decoration(hero,"HeritageCardRule",0,302,580,6,brass);
        Decoration(hero,"HeritageFeatureLeftRule",-288,0,4,610,brass);
        Decoration(hero,"HeritageFeatureRightRule",288,0,4,610,brass);
        Decoration(hero,"HeritageFeatureBottomRule",0,-303,580,4,brass);
        var badge=LocalLabel(hero,"HeritageFeaturedBadge","vtour_main_attraction",0,256,530,62,38,ink);
        var ribbon=Decoration(hero,"HeritageFeatureRibbon",0,256,568,68,brass);ribbon.SetSiblingIndex(badge.transform.GetSiblingIndex());

        Place(hero.Find("Image"),0,50,520,278);Place(hero.Find("HeritagePhotoMount"),0,50,536,294);
        var title=hero.Find("Text (TMP)").GetComponent<TMP_Text>();Place(title.transform,0,-165,530,98);Text(title,56,cream);
        var enter=hero.Find("HeritageEnterLabel").GetComponent<TMP_Text>();Place(enter.transform,0,-253,530,70);Text(enter,40,cream);
        string[] others={"A2","A8","A9"};
        for(int i=0;i<others.Length;i++)
        {
            var holder=p.Find(others[i]);Place(holder,-40+i*350,-140,300,500);
            var b=holder.GetComponentInChildren<UnityEngine.UI.Button>(true).transform;Place(b,0,0,300,500);Button(b,brown);
            Place(b.Find("Image"),0,104,264,240);Place(b.Find("HeritagePhotoMount"),0,104,276,252);
            Place(b.Find("HeritageCardRule"),0,248,300,4);

            var name=b.Find("Text (TMP)").GetComponent<TMP_Text>();Place(name.transform,0,-110,276,80);Text(name,40,cream);
            Place(b.Find("HeritageEnterLabel"),0,-198,290,90);Text(b.Find("HeritageEnterLabel").GetComponent<TMP_Text>(),32,cream);
        }
        Place(p.Find("HeritageHelp"),0,-460,1670,70);
        Verify(before);AssetDatabase.SaveAssets();Canvas.ForceUpdateCanvases();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        result.Log("Ban Ja-Lae 3 is now the larger lead card, with a localized main-attraction ribbon and brass border. Preserved "+before.Count+" existing functional components.");
    }
}




