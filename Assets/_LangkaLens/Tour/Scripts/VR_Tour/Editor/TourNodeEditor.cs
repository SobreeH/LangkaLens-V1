using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[CustomEditor(typeof(TourNode))]
public class TourNodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); // Draw default inspector

        TourNode node = (TourNode)target;

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Tour Setup Helpers", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Sync from 'PrevDummy'"))
        {
            GameObject prev = FindDummy("PrevDummy");
            if (prev != null)
            {
                Undo.RecordObject(node, "Sync PrevDummy");
                if (node.previousPath == null) node.previousPath = new NodeConnection();
                node.previousPath.arrowPosition = prev.transform.position;
                node.previousPath.arrowRotation = prev.transform.eulerAngles;
                EditorUtility.SetDirty(node);
                Debug.Log($"[{node.name}] Successfully grabbed transform from PrevDummy!");
            }
            else
            {
                Debug.LogWarning("Could not find a GameObject named 'PrevDummy' in the scene (active or inactive).");
            }
        }

        if (GUILayout.Button("Sync from 'NextDummy'"))
        {
            GameObject next = FindDummy("NextDummy");
            if (next != null)
            {
                Undo.RecordObject(node, "Sync NextDummy");
                if (node.nextPath == null) node.nextPath = new NodeConnection();
                node.nextPath.arrowPosition = next.transform.position;
                node.nextPath.arrowRotation = next.transform.eulerAngles;
                EditorUtility.SetDirty(node);
                Debug.Log($"[{node.name}] Successfully grabbed transform from NextDummy!");
            }
            else
            {
                Debug.LogWarning("Could not find a GameObject named 'NextDummy' in the scene (active or inactive).");
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Sync from 'IconDummy'"))
        {
            GameObject icon = FindDummy("IconDummy");
            if (icon != null)
            {
                Undo.RecordObject(node, "Sync IconDummy");
                node.iconPosition = icon.transform.position;
                node.iconRotation = icon.transform.eulerAngles;
                node.iconScale = icon.transform.localScale;
                EditorUtility.SetDirty(node);
                Debug.Log($"[{node.name}] Successfully grabbed transform from IconDummy!");
            }
            else
            {
                Debug.LogWarning("Could not find a GameObject named 'IconDummy' in the scene (active or inactive).");
            }
        }

        EditorGUILayout.EndHorizontal();



        GUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Setup Scene for this Node", GUILayout.Height(30)))
        {
            SetupSceneForNode(node);
        }

        if (GUILayout.Button("Revert Scene to Default", GUILayout.Height(30)))
        {
            RevertScene();
        }
        EditorGUILayout.EndHorizontal();
    }

    private GameObject FindDummy(string dummyName)
    {
        // Finds active and inactive GameObjects in the current scene
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
                continue;
            
            if (go.scene.isLoaded && go.name == dummyName)
            {
                return go;
            }
        }
        return null;
    }

    private void SetupSceneForNode(TourNode node)
    {
        // 1. Try to set the 360 material texture via TourManager's photoSphereRenderer
        if (node.photoReference != null && !string.IsNullOrEmpty(node.photoReference.AssetGUID))
        {
            string path = AssetDatabase.GUIDToAssetPath(node.photoReference.AssetGUID);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            
            if (tex != null)
            {
                TourManager tourManager = FindObjectOfType<TourManager>();
                if (tourManager != null && tourManager.photoSphereRenderer != null)
                {
                    if (tourManager.photoSphereRenderer.sharedMaterial != null)
                    {
                        Undo.RecordObject(tourManager.photoSphereRenderer.sharedMaterial, "Change 360 Texture");
                        tourManager.photoSphereRenderer.sharedMaterial.mainTexture = tex;
                        Debug.Log($"[{node.name}] Updated Photo Sphere material with node's 360 image.");
                    }
                }
            }
        }

        List<Object> toSelect = new List<Object>();

        // 2. Enable PrevDummy and move it to node's saved position
        GameObject prev = FindDummy("PrevDummy");
        if (prev != null)
        {
            Undo.RecordObject(prev, "Enable PrevDummy");
            prev.SetActive(true);
            Undo.RecordObject(prev.transform, "Move PrevDummy");
            if (node.previousPath != null)
            {
                prev.transform.position = node.previousPath.arrowPosition;
                prev.transform.eulerAngles = node.previousPath.arrowRotation;
            }
            toSelect.Add(prev);
        }

        // 3. Enable NextDummy and move it to node's saved position
        GameObject next = FindDummy("NextDummy");
        if (next != null)
        {
            Undo.RecordObject(next, "Enable NextDummy");
            next.SetActive(true);
            Undo.RecordObject(next.transform, "Move NextDummy");
            if (node.nextPath != null)
            {
                next.transform.position = node.nextPath.arrowPosition;
                next.transform.eulerAngles = node.nextPath.arrowRotation;
            }
            toSelect.Add(next);
        }

        // 4. Enable IconDummy and move it to node's saved position
        GameObject icon = FindDummy("IconDummy");
        if (icon != null)
        {
            Undo.RecordObject(icon, "Enable IconDummy");
            icon.SetActive(true);
            Undo.RecordObject(icon.transform, "Move IconDummy");
            icon.transform.position = node.iconPosition;
            icon.transform.eulerAngles = node.iconRotation;
            icon.transform.localScale = node.iconScale;
            toSelect.Add(icon);
        }

        if (toSelect.Count > 0)
        {
            Selection.objects = toSelect.ToArray();
        }

        Debug.Log($"[{node.name}] Scene prepared for editing node paths.");
    }

    private void RevertScene()
    {
        // 1. Revert the Photo Sphere texture to Title Screen Photo
        TourManager tourManager = FindObjectOfType<TourManager>();
        if (tourManager != null && tourManager.photoSphereRenderer != null)
        {
            if (tourManager.titleScreenPhoto != null && !string.IsNullOrEmpty(tourManager.titleScreenPhoto.AssetGUID))
            {
                string path = AssetDatabase.GUIDToAssetPath(tourManager.titleScreenPhoto.AssetGUID);
                Texture2D defaultTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                
                if (defaultTex != null && tourManager.photoSphereRenderer.sharedMaterial != null)
                {
                    Undo.RecordObject(tourManager.photoSphereRenderer.sharedMaterial, "Revert 360 Texture");
                    tourManager.photoSphereRenderer.sharedMaterial.mainTexture = defaultTex;
                }
            }
        }

        // 2. Disable dummies
        GameObject prev = FindDummy("PrevDummy");
        if (prev != null)
        {
            Undo.RecordObject(prev, "Disable PrevDummy");
            prev.SetActive(false);
        }

        GameObject next = FindDummy("NextDummy");
        if (next != null)
        {
            Undo.RecordObject(next, "Disable NextDummy");
            next.SetActive(false);
        }

        GameObject icon = FindDummy("IconDummy");
        if (icon != null)
        {
            Undo.RecordObject(icon, "Disable IconDummy");
            icon.SetActive(false);
        }

        Debug.Log("Scene reverted to default state.");
    }
}
