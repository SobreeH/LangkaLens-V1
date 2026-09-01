using UnityEngine;
using UnityEngine.Events;

public class ToggleModelAction : MonoBehaviour
{
    private void Start()
    {
        Component wrapper = GetComponent("InteractableUnityEventWrapper");
        if (wrapper != null)
        {
            // First try property
            var propInfo = wrapper.GetType().GetProperty("WhenSelect");
            if (propInfo != null)
            {
                UnityEvent evt = propInfo.GetValue(wrapper) as UnityEvent;
                if (evt != null)
                {
                    evt.AddListener(OnToggle);
                }
            }
            else
            {
                // Try field
                var fieldInfo = wrapper.GetType().GetField("WhenSelect");
                if (fieldInfo != null)
                {
                    UnityEvent evt = fieldInfo.GetValue(wrapper) as UnityEvent;
                    if (evt != null)
                    {
                        evt.AddListener(OnToggle);
                    }
                }
            }
        }
    }

    public void OnToggle()
    {
        if (InspectionManager.Instance != null)
        {
            InspectionManager.Instance.OnToggleModelButtonClicked();
        }
    }
}
