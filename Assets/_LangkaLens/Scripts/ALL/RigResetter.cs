using UnityEngine;
using UnityEngine.SceneManagement;

public class RigResetter : MonoBehaviour
{
    void OnEnable()
    {
        // Subscribe to the scene load event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Unsubscribe when destroyed to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Reset the Camera Rig's transform in the Unity scene
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one; // VR scale should always be 1,1,1

        // 2. Recenter the physical headset tracking space
        if (OVRManager.display != null)
        {
            OVRManager.display.RecenterPose();
        }
    }
}