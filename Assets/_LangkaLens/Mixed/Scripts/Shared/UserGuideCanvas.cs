using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-specific pages for the tablet's localized interaction guide.</summary>
[DisallowMultipleComponent]
public sealed class UserGuideCanvas : MonoBehaviour
{
    [Serializable]
    private class Page
    {
        public string sceneName;
        public GameObject content;
        public Button tab;
    }

    [SerializeField] private Page[] pages = Array.Empty<Page>();

    private void OnEnable()
    {
        int selected = Array.FindIndex(pages, page => page.sceneName == gameObject.scene.name);
        ShowPage(selected < 0 ? 0 : selected);
    }

    public void ShowPage(int index)
    {
        if (index < 0 || index >= pages.Length) return;
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i].content != null) pages[i].content.SetActive(i == index);
            if (pages[i].tab != null) pages[i].tab.interactable = i != index;
        }
    }

    public void Close() => gameObject.SetActive(false);
}
