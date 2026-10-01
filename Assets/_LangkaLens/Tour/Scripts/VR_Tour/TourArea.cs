using UnityEngine;

[CreateAssetMenu(fileName = "New Tour Area", menuName = "Virtual Tour/Tour Area")]
public class TourArea : ScriptableObject
{
    public string areaName;
    public TourReconstructionDefinition reconstruction;
    public TourNode startingNode; 
}