using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "Tour Reconstruction", menuName = "Virtual Tour/Reconstruction")]
public class TourReconstructionDefinition : ScriptableObject
{
    public AssetReferenceGameObject inspectionPrefab;
    [Min(0.1f)] public float displaySize = 0.65f;
    [Min(0.5f)] public float viewingDistance = 1.2f;
    public float verticalOffset = -0.2f;

    public bool IsConfigured => inspectionPrefab != null && inspectionPrefab.RuntimeKeyIsValid();
}
