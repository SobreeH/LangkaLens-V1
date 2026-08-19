using UnityEngine;

public class ArrowPlacementHelper : MonoBehaviour
{
    [Header("Which photo are we currently standing in?")]
    public TourNode sourceNode; 

    [Header("Where should this arrow take us?")]
    public TourNode targetNode; 

    [ContextMenu("1. Save as PREVIOUS Arrow")]
    public void SaveAsPrevious()
    {
        SaveArrow(true);
    }

    [ContextMenu("2. Save as NEXT Arrow")]
    public void SaveAsNext()
    {
        SaveArrow(false);
    }

    private void SaveArrow(bool isPrevious)
    {
        if (sourceNode == null || targetNode == null)
        {
            Debug.LogError("Helper Tool: Please assign both the Source and Target nodes!");
            return;
        }

        NodeConnection newConnection = new NodeConnection
        {
            targetNode = this.targetNode,
            arrowPosition = transform.position,
            arrowRotation = transform.eulerAngles
        };

        if (isPrevious)
        {
            sourceNode.previousPath = newConnection;
            Debug.Log($"SUCCESS: Saved PREVIOUS arrow pointing to {targetNode.name}!");
        }
        else
        {
            sourceNode.nextPath = newConnection;
            Debug.Log($"SUCCESS: Saved NEXT arrow pointing to {targetNode.name}!");
        }

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(sourceNode);
        UnityEditor.AssetDatabase.SaveAssets();
        #endif
    }
}