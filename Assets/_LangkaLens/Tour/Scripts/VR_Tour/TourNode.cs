using UnityEngine;
using UnityEngine.AddressableAssets;

public enum NodePosition { F, B, L, R, C }

[CreateAssetMenu(fileName = "New Tour Node", menuName = "Virtual Tour/Tour Node")]
public class TourNode : ScriptableObject
{
    [Header("Naming Convention (xPy)")]
    public int areaNumber;
    public NodePosition position;
    public int positionNumber;

    [Header("The 360 Image")]
    public AssetReferenceTexture2D photoReference; 
    
    [Header("3D Overlay (Optional)")]
    [Tooltip("Drag the Addressable 3D Prefab here")]
    public AssetReferenceGameObject overlayModel; 
    public Vector3 modelPosition;
    public Vector3 modelRotation;
    public Vector3 modelScale = Vector3.one;

    // Inside TourNode.cs, add these lines right under your 3D Overlay variables:

    [Header("Hover Icon Placement")]
    public Vector3 iconPosition;
    public Vector3 iconRotation;
    public Vector3 iconScale = Vector3.one;

    [Header("Previous Node (Leave blank if start of tour)")]
    public NodeConnection previousPath;

    [Header("Next Node (Leave blank if end of tour)")]
    public NodeConnection nextPath;

    public string GetNodeName()
    {
        return $"{areaNumber}{position}{positionNumber}";
    }
}

// We changed this to a 'class' instead of 'struct' so Unity can check if it's empty
[System.Serializable]
public class NodeConnection
{
    public TourNode targetNode;
    public Vector3 arrowPosition;
    public Vector3 arrowRotation;
}