#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LangkaLens.Editor
{
    public static class LogoLocalizationSetup
    {
        private const string ScenePath = "Assets/_LangkaLens/_Scenes/Main_Menu_Scene.unity";

        private const string ComsciEnPath = "Assets/_LangkaLens/Main_menu/Images/Logo_Subbrand_Faculty-of-Communication_Sciences_[Full]_EN.png";
        private const string ComsciThPath = "Assets/_LangkaLens/Main_menu/Images/Logo_Subbrand_Faculty-of-Communication_Sciences_[Full]_TH.png";

        private const string TsriEnPath = "Assets/_LangkaLens/Main_menu/Images/TSRI_logo_EN.png";
        private const string TsriThPath = "Assets/_LangkaLens/Main_menu/Images/TSRI_logo_TH.png";

        [MenuItem("Tools/LangkaLens/Main Menu/Setup Logo Localization")]
        public static void SetupLogoLocalization()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            bool needToSave = false;

            if (activeScene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var comsciTexEn = AssetDatabase.LoadAssetAtPath<Texture>(ComsciEnPath);
            var comsciTexTh = AssetDatabase.LoadAssetAtPath<Texture>(ComsciThPath);
            var tsriTexEn = AssetDatabase.LoadAssetAtPath<Texture>(TsriEnPath);
            var tsriTexTh = AssetDatabase.LoadAssetAtPath<Texture>(TsriThPath);

            if (comsciTexEn == null) Debug.LogError($"[LogoLocalizationSetup] Missing Comsci EN texture at: {ComsciEnPath}");
            if (comsciTexTh == null) Debug.LogError($"[LogoLocalizationSetup] Missing Comsci TH texture at: {ComsciThPath}");
            if (tsriTexEn == null) Debug.LogError($"[LogoLocalizationSetup] Missing TSRI EN texture at: {TsriEnPath}");
            if (tsriTexTh == null) Debug.LogError($"[LogoLocalizationSetup] Missing TSRI TH texture at: {TsriThPath}");

            // Configure Comsci_LOGO
            var comsciObj = GameObject.Find("Comsci_LOGO");
            if (comsciObj != null)
            {
                var loc = comsciObj.GetComponent<LocalizedRawImage>();
                if (loc == null)
                {
                    loc = Undo.AddComponent<LocalizedRawImage>(comsciObj);
                }
                else
                {
                    Undo.RecordObject(loc, "Update Comsci_LOGO LocalizedRawImage");
                }

                loc.TargetImage = comsciObj.GetComponent<UnityEngine.UI.RawImage>();
                loc.EnglishTexture = comsciTexEn;
                loc.ThaiTexture = comsciTexTh;

                EditorUtility.SetDirty(loc);
                EditorUtility.SetDirty(comsciObj);
                needToSave = true;
                Debug.Log("[LogoLocalizationSetup] Successfully configured LocalizedRawImage on Comsci_LOGO.");
            }
            else
            {
                Debug.LogError("[LogoLocalizationSetup] Could not find GameObject named 'Comsci_LOGO' in current scene!");
            }

            // Configure TSRI_LOGO
            var tsriObj = GameObject.Find("TSRI_LOGO");
            if (tsriObj != null)
            {
                var loc = tsriObj.GetComponent<LocalizedRawImage>();
                if (loc == null)
                {
                    loc = Undo.AddComponent<LocalizedRawImage>(tsriObj);
                }
                else
                {
                    Undo.RecordObject(loc, "Update TSRI_LOGO LocalizedRawImage");
                }

                loc.TargetImage = tsriObj.GetComponent<UnityEngine.UI.RawImage>();
                loc.EnglishTexture = tsriTexEn;
                loc.ThaiTexture = tsriTexTh;

                EditorUtility.SetDirty(loc);
                EditorUtility.SetDirty(tsriObj);
                needToSave = true;
                Debug.Log("[LogoLocalizationSetup] Successfully configured LocalizedRawImage on TSRI_LOGO.");
            }
            else
            {
                Debug.LogError("[LogoLocalizationSetup] Could not find GameObject named 'TSRI_LOGO' in current scene!");
            }

            if (needToSave)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                AssetDatabase.SaveAssets();
                Debug.Log("[LogoLocalizationSetup] Scene saved successfully.");
            }
        }
    }
}
#endif
