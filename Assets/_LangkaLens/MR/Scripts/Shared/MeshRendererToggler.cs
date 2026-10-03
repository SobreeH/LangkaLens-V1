using UnityEngine;

public class MeshRendererToggler : MonoBehaviour
{
    [Tooltip("Check or uncheck to toggle all child MeshRenderers")]
    public bool showMeshes = false;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Delay call to safely modify scene objects from OnValidate
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            
            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                if (r != null && r.enabled != showMeshes)
                {
                    UnityEditor.Undo.RecordObject(r, "Toggle Mesh Renderer");
                    r.enabled = showMeshes;
                }
            }
            
            // Force repaint to show changes immediately in the Scene view
            UnityEditor.SceneView.RepaintAll();
        };
    }
#endif
}
