using System;
using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;

/// <summary>
/// Handles Meta Horizon OS Entitlement Check (VRC compliance).
/// Ensures that the user running the application is properly entitled (has acquired/licensed the app).
/// Automatically initializes before the first scene loads and persists across scenes.
/// </summary>
public class MetaEntitlementCheck : MonoBehaviour
{
    [Tooltip("If true, the application will exit if the entitlement check fails on a device.")]
    [SerializeField] private bool quitOnFailure = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // Avoid duplicate initialization
        if (FindFirstObjectByType<MetaEntitlementCheck>() != null)
            return;

        GameObject bootstrapObj = new GameObject("MetaEntitlementCheck");
        bootstrapObj.AddComponent<MetaEntitlementCheck>();
        DontDestroyOnLoad(bootstrapObj);
    }

    private void Awake()
    {
        try
        {
            Core.AsyncInitialize().OnComplete(OnInitCallback);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MetaEntitlementCheck] Platform initialization threw exception: {e.Message}");
            if (!UnityEngine.Application.isEditor && quitOnFailure)
            {
                UnityEngine.Application.Quit();
            }
        }
    }

    private void OnInitCallback(Message<PlatformInitialize> msg)
    {
        if (msg.IsError)
        {
            Debug.LogError($"[MetaEntitlementCheck] Platform init failed: {msg.GetError().Message}");
            if (!UnityEngine.Application.isEditor && quitOnFailure)
            {
                UnityEngine.Application.Quit();
            }
            return;
        }

        Entitlements.IsUserEntitledToApplication().OnComplete(OnEntitlementCallback);
    }

    private void OnEntitlementCallback(Message msg)
    {
        if (msg.IsError)
        {
            Debug.LogError($"[MetaEntitlementCheck] Entitlement check failed: {msg.GetError().Message}");
            if (!UnityEngine.Application.isEditor && quitOnFailure)
            {
                UnityEngine.Application.Quit();
            }
            return;
        }

        Debug.Log("[MetaEntitlementCheck] User entitlement check succeeded.");
    }
}
