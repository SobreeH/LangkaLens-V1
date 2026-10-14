using System;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Components;

/// <summary>Scene-specific pages for the tablet's localized interaction guide.</summary>
[DisallowMultipleComponent]
public sealed class UserGuideCanvas : MonoBehaviour
{
    [Serializable]
    private class Page
    {
        public string sceneName;
        public GameObject content;
        public UnityEngine.UI.Button tab;
        public Step[] steps = Array.Empty<Step>();
    }

    [Serializable]
    private class Step
    {
        public Sprite illustration;
        public string titleKey;
        public string instructionKey;
    }

    [SerializeField] private Page[] pages = Array.Empty<Page>();
    [SerializeField] private int defaultPage;
    [SerializeField] private GameObject illustratedContent;
    [SerializeField] private UnityEngine.UI.Image illustration;
    [SerializeField] private LocalizeStringEvent stepTitle;
    [SerializeField] private LocalizeStringEvent stepInstruction;
    [SerializeField] private TMP_Text stepCounter;
    [SerializeField] private UnityEngine.UI.Button previousButton;
    [SerializeField] private UnityEngine.UI.Button nextButton;

    private int selectedPage;
    private int selectedStep;

    public event Action Closed;

    private void OnEnable()
    {
        int selected = Array.FindIndex(pages, page => page.sceneName == gameObject.scene.name);
        ShowPage(selected < 0 ? defaultPage : selected);
    }

    public void ShowPage(int index)
    {
        if (index < 0 || index >= pages.Length) return;
        selectedPage = index;
        selectedStep = 0;
        bool illustrated = illustratedContent != null && pages[index].steps.Length > 0;
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i].content != null) pages[i].content.SetActive(i == index && !illustrated);
            if (pages[i].tab != null) pages[i].tab.interactable = i != index;
        }
        if (illustratedContent != null) illustratedContent.SetActive(illustrated);
        if (illustrated) RefreshStep();
    }

    public void PreviousStep() => MoveStep(-1);

    public void NextStep() => MoveStep(1);

    private void MoveStep(int direction)
    {
        if (selectedPage < 0 || selectedPage >= pages.Length) return;
        int count = pages[selectedPage].steps.Length;
        if (count == 0) return;
        selectedStep = Mathf.Clamp(selectedStep + direction, 0, count - 1);
        RefreshStep();
    }

    private void RefreshStep()
    {
        Step[] steps = pages[selectedPage].steps;
        Step step = steps[selectedStep];
        illustration.sprite = step.illustration;
        illustration.enabled = step.illustration != null;
        stepTitle.StringReference.SetReference("UserGuide", step.titleKey);
        stepInstruction.StringReference.SetReference("UserGuide", step.instructionKey);
        stepCounter.text = $"{selectedStep + 1} / {steps.Length}";
        previousButton.interactable = selectedStep > 0;
        nextButton.interactable = selectedStep < steps.Length - 1;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        Closed?.Invoke();
    }
}
