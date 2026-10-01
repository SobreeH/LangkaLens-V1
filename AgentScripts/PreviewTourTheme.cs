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
            for(int i=0;i<graphics.Length;i++)graphics[i].enabled=enabled[i];
            for(int i=0;i<renderers.Length;i++)renderers[i].enabled=rendererEnabled[i];
            for(int i=0;i<all.Length;i++){all[i].gameObject.layer=layers[i];all[i].gameObject.SetActive(active[i]);}
            RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(go);Object.DestroyImmediate(rt);if(image)Object.DestroyImmediate(image);
        }
    }

    public void Execute(ExecutionResult result)
    {
        Directory.CreateDirectory("Temp/UIReview");
        var canvases=Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var menu=canvases.Single(c=>c.name=="MenuCanvas");
        var inspection=canvases.Single(c=>c.name=="TourInspectionPanel");
        var tables=AssetDatabase.FindAssets("t:StringTable").Select(g=>AssetDatabase.LoadAssetAtPath<UnityEngine.Localization.Tables.StringTable>(AssetDatabase.GUIDToAssetPath(g))).Where(t=>t.TableCollectionName=="VTour_UI").ToArray();
        Capture((RectTransform)menu.transform,"Tour-Menu");
        Capture((RectTransform)menu.transform,"Tour-Menu-Thai",()=>{
            var table=tables.First(t=>t.LocaleIdentifier.Code=="th");
            foreach(var local in menu.GetComponentsInChildren<UnityEngine.Localization.Components.LocalizeStringEvent>(true)){
                var r=local.StringReference.TableEntryReference;var entry=r.KeyId!=0?table.GetEntry(r.KeyId):table.GetEntry(r.Key);
                if(entry!=null)local.GetComponent<TMPro.TMP_Text>().text=entry.LocalizedValue;
            }
        });
        foreach(string language in new[]{"en","th"})
        Capture((RectTransform)inspection.transform,"Tour-Inspection-"+language,()=>{
            var table=tables.First(t=>t.LocaleIdentifier.Code==language);
            inspection.transform.Find("Panel/Title").GetComponent<TMPro.TMP_Text>().text=table.GetEntry("reconstruction_title_3").LocalizedValue;
            inspection.transform.Find("Panel/Description").GetComponent<TMPro.TMP_Text>().text=table.GetEntry("reconstruction_description_3").LocalizedValue;
            foreach(var local in inspection.GetComponentsInChildren<UnityEngine.Localization.Components.LocalizeStringEvent>(true)){
                var r=local.StringReference.TableEntryReference;var entry=r.KeyId!=0?table.GetEntry(r.KeyId):table.GetEntry(r.Key);
                if(entry!=null)local.GetComponent<TMPro.TMP_Text>().text=entry.LocalizedValue;
            }
        });
        result.Log("Rendered tour menu and inspection panel in both languages; restored original state.");
    }
}

