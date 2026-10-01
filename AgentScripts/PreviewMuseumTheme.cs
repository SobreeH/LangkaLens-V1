using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;

internal class CommandScript : IRunCommand
{
    void Capture(RectTransform target,string filename,System.Action prepare=null)
    {
        var canvas=target.GetComponentInParent<Canvas>(true);
        var scene=UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var active=all.Select(t=>t.gameObject.activeSelf).ToArray();var layers=all.Select(t=>t.gameObject.layer).ToArray();
        var graphics=all.Select(t=>t.GetComponent<UnityEngine.UI.Graphic>()).Where(g=>g!=null).ToArray();var enabled=graphics.Select(g=>g.enabled).ToArray();
        var texts=graphics.OfType<TMPro.TMP_Text>().ToArray();var strings=texts.Select(t=>t.text).ToArray();
        var renderers=all.SelectMany(t=>t.GetComponents<Renderer>()).ToArray();var rendererEnabled=renderers.Select(r=>r.enabled).ToArray();
        var scrolls=target.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true);var positions=scrolls.Select(s=>s.content.anchoredPosition).ToArray();
        var previous=RenderTexture.active;var go=new GameObject("Temporary MR UI Preview");var cam=go.AddComponent<Camera>();
        var rt=new RenderTexture(1400,1000,24);Texture2D image=null;
        try
        {
            for(var t=target.transform;t!=null;t=t.parent)t.gameObject.SetActive(true);
            foreach(var t in all)t.gameObject.layer=30;
            foreach(var renderer in renderers)renderer.enabled=false;
            foreach(var g in graphics)if(!g.transform.IsChildOf(target)&&g.transform!=target)g.enabled=false;
            if(prepare!=null)prepare();
            Canvas.ForceUpdateCanvases();
            cam.cullingMask=1<<30;cam.orthographic=true;cam.aspect=1.4f;
            cam.orthographicSize=Mathf.Max(target.rect.height*target.lossyScale.y,target.rect.width*target.lossyScale.x/1.4f)*.54f;
            cam.nearClipPlane=.001f;cam.farClipPlane=30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.065f,.045f,.036f);
            cam.transform.SetPositionAndRotation(target.TransformPoint(target.rect.center)-target.forward*10,target.rotation);
            cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image=new Texture2D(1400,1000,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();File.WriteAllBytes("Temp/UIReview/"+filename+".png",image.EncodeToPNG());
        }
        finally
        {
            for(int i=0;i<texts.Length;i++)texts[i].text=strings[i];
            Canvas.ForceUpdateCanvases();for(int i=0;i<scrolls.Length;i++){UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scrolls[i].content);scrolls[i].content.anchoredPosition=positions[i];}
            for(int i=0;i<graphics.Length;i++)graphics[i].enabled=enabled[i];
            for(int i=0;i<renderers.Length;i++)renderers[i].enabled=rendererEnabled[i];
            for(int i=0;i<all.Length;i++){all[i].gameObject.layer=layers[i];all[i].gameObject.SetActive(active[i]);}
            RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(go);Object.DestroyImmediate(rt);if(image)Object.DestroyImmediate(image);
        }
    }

    public void Execute(ExecutionResult result)
    {
        Directory.CreateDirectory("Temp/UIReview");
        var inspection=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(c=>c.name=="InspectCanvas");
        var tables=AssetDatabase.FindAssets("t:StringTable").Select(g=>AssetDatabase.LoadAssetAtPath<UnityEngine.Localization.Tables.StringTable>(AssetDatabase.GUIDToAssetPath(g))).Where(t=>t.TableCollectionName=="Artifacts_UI").ToArray();
        var title=inspection.transform.Find("Panel/Title").GetComponent<TMPro.TMP_Text>();
        var body=inspection.transform.Find("Panel/DescriptionBox/Viewport/Content/Description").GetComponent<TMPro.TMP_Text>();
        var scroll=inspection.transform.Find("Panel/DescriptionBox").GetComponent<UnityEngine.UI.ScrollRect>();
        var state=inspection.transform.Find("Panel/Model_State").GetComponent<TMPro.TMP_Text>();
        int checks=0;
        foreach(string lang in new[]{"en","th"})Capture((RectTransform)inspection.transform,"Museum-Inspection-"+lang,()=>{
            var table=tables.Single(t=>t.LocaleIdentifier.Code==lang);
            foreach(var artifact in Object.FindObjectsByType<ArtifactData>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                var titleRef=artifact.localizedTitle.TableEntryReference;var bodyRef=artifact.localizedDescription.TableEntryReference;
                var te=titleRef.KeyId!=0?table.GetEntry(titleRef.KeyId):table.GetEntry(titleRef.Key);
                var be=bodyRef.KeyId!=0?table.GetEntry(bodyRef.KeyId):table.GetEntry(bodyRef.Key);
                if(te==null||be==null)throw new System.Exception("Missing artifact translation.");
                title.text=te.LocalizedValue;body.text=be.LocalizedValue;
                Canvas.ForceUpdateCanvases();UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);Canvas.ForceUpdateCanvases();
                title.ForceMeshUpdate();body.ForceMeshUpdate();
                if(title.isTextOverflowing||title.textBounds.size.y>title.rectTransform.rect.height)throw new System.Exception("Title overflow: "+title.text);
                if(body.textBounds.size.x>body.rectTransform.rect.width+1||scroll.content.rect.height<body.textBounds.size.y)throw new System.Exception("Description scrolling bounds invalid.");
                checks++;
            }
            title.text=table.GetEntry("artifact_title_stupa_water_pot").LocalizedValue;
            body.text=table.GetEntry("artifact_desc_stupa_water_pot").LocalizedValue;
            state.text=table.GetEntry("model_state_current").LocalizedValue;
            inspection.transform.Find("Panel/Toggle_btn").GetComponentInChildren<TMPro.TMP_Text>(true).text=table.GetEntry("toggle_btn_model").LocalizedValue;
            Canvas.ForceUpdateCanvases();UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);scroll.Rebuild(UnityEngine.UI.CanvasUpdate.PostLayout);scroll.verticalNormalizedPosition=1;Canvas.ForceUpdateCanvases();body.ForceMeshUpdate();
        });
        result.Log("Validated "+checks+" artifact translations: titles fit and descriptions have complete scrollable content. Rendered English and Thai previews.");
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}




