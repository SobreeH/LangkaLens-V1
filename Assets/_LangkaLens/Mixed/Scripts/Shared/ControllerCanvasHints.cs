using System;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using LangkaLens.MR;

/// <summary>Owns controller hints for the four experience scenes. Localization owns translated text.</summary>
[DisallowMultipleComponent]
public class ControllerCanvasHints : MonoBehaviour
{
    public const string TableName = "ControllerHints";
    public enum Control { LeftX, LeftY, LeftMenu, LeftStick, LeftTrigger, LeftGrip, RightA, RightB, RightStick, RightTrigger, RightGrip }
    [Flags]
    public enum Context
    {
        None = 0, Inspecting = 1, AlternateModel = 2, Aligning = 4,
        AlignmentPlaced = 8, Busy = 16, TourMenu = 32, ConfirmExit = 64,
        Tablet = 128, Move = 256, Turn = 512, AlignmentAvailable = 1024, ReconstructionAvailable = 2048
    }
    [Serializable]
    public class Label
    {
        public Control control;
        public LocalizeStringEvent localized;
    }

    [SerializeField] private Label[] labels = Array.Empty<Label>();
    [SerializeField] private InspectionManager inspection;
    [SerializeField] private TourManager tour;
    [SerializeField] private MRMeshReferenceAlignment alignment;
    [SerializeField] private MRMainMenuConfirmation exitConfirmation;
    [SerializeField] private SubMenuToggle tablet;
    [SerializeField] private Behaviour moveInput;
    [SerializeField] private Behaviour turnInput;

    public Label[] Labels => labels;

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        RefreshHints();
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene previous, Scene current) => RefreshHints();

    // State owners update in Update; sampling here avoids listener ordering and scene reload races.
    // References are serialized, and unchanged hints never request localization again.
    private void LateUpdate() => RefreshHints();

    public Context ReadContext()
    {
        var context = Context.None;
        bool inspecting = inspection != null && inspection.isActiveAndEnabled &&
                          inspection.IsInspecting;
        if (inspecting)
        {
            context |= Context.Inspecting;
            if (inspection.CanToggleInspectedModel)
                context |= Context.AlternateModel;
        }
        if (alignment != null && alignment.isActiveAndEnabled)
        {
            context |= Context.AlignmentAvailable;
            if (!alignment.IsConfirmed) context |= Context.Aligning;
            if (alignment.HasPlacedPreview) context |= Context.AlignmentPlaced;
            if (alignment.IsBusy) context |= Context.Busy;
        }
        if (tour != null && tour.isActiveAndEnabled)
        {
            if (tour.IsAtMainMenu) context |= Context.TourMenu;
            if (tour.reconstructionPresenter != null && tour.reconstructionPresenter.CanToggleInspection)
                context |= Context.ReconstructionAvailable;
            if (tour.IsTransitioning || (tour.IsReconstructionBusy && !inspecting)) context |= Context.Busy;
        }
        if (exitConfirmation != null && exitConfirmation.isActiveAndEnabled && exitConfirmation.IsShowing)
            context |= Context.ConfirmExit;
        if (tablet != null && tablet.isActiveAndEnabled && tablet.menuCanvas != null)
            context |= Context.Tablet;
        bool locomotion = inspection == null || inspection.locomotionScript == null ||
                          inspection.locomotionScript.isActiveAndEnabled;
        if (!inspecting && locomotion)
        {
            if (moveInput != null && moveInput.isActiveAndEnabled) context |= Context.Move;
            if (turnInput != null && turnInput.isActiveAndEnabled) context |= Context.Turn;
        }
        return context;
    }

    public void RefreshHints()
    {
        // Use the owning scene, not a global search: additive scenes cannot steal each other's hints.
        ApplyContext(gameObject.scene.name, ReadContext());
    }

    public void ApplyContext(string sceneName, Context context)
    {
        foreach (var label in labels)
        {
            if (label == null || label.localized == null) continue;
            string key = ResolveKey(sceneName, label.control, context);
            var localized = label.localized;
            bool visible = key != null;
            if (visible && (localized.StringReference.TableReference.TableCollectionName != TableName ||
                            localized.StringReference.TableEntryReference.Key != key))
            {
                // Do not display the previous context's translation while the new string is loading.
                localized.OnUpdateString.Invoke(string.Empty);
                localized.StringReference.SetReference(TableName, key);
            }
            if (localized.gameObject.activeSelf != visible)
                localized.gameObject.SetActive(visible);
        }
    }

    public static string ResolveKey(string scene, Control control, Context context)
    {
        bool main = scene == "Main_Menu_Scene";
        bool mr = scene == "MR_Scene";
        bool museum = scene == "VR_Museum_Scene";
        bool tourScene = scene == "VR_Tour_Scene";
        if (!main && !mr && !museum && !tourScene) return null;
        bool inspecting = (context & Context.Inspecting) != 0;
        bool busy = (context & Context.Busy) != 0;
        bool tourMenu = (context & Context.TourMenu) != 0;
        bool aligning = mr && (context & Context.Aligning) != 0 && !busy;
        switch (control)
        {
            case Control.LeftTrigger:
            case Control.RightTrigger: return "select";
            case Control.LeftMenu: return (context & Context.Tablet) != 0 ? "tablet" : null;
            case Control.LeftX:
                if (main) return null;
                if ((museum || tourScene) && inspecting) return "exit_inspection";
                if (tourScene && busy) return null;
                if (tourScene && !tourMenu) return "exit_tour";
                if (mr && (context & Context.AlignmentAvailable) != 0)
                    return (context & Context.ConfirmExit) != 0 ? "mr_confirm_home" : "mr_home_realign";
                return (context & Context.ConfirmExit) != 0 ? "confirm_home" : "home";
            case Control.LeftY:
                if (mr) return null;
                if (tourScene && (context & Context.ReconstructionAvailable) != 0)
                    return inspecting ? "exit_inspection" : busy ? "cancel_reconstruction" : "inspect_reconstruction";
                return museum && inspecting && (context & Context.AlternateModel) != 0 ? "switch_model" : null;
            case Control.LeftStick:
                if (tourScene) return !tourMenu && !busy && !inspecting ? "teleport" : null;
                return (context & Context.Move) != 0 ? "move" : null;
            case Control.RightStick:
                if ((tourScene || museum) && inspecting) return "inspect_turn_reset";
                if (aligning) return "align_turn_reach";
                if (mr) return null;
                return (context & Context.Turn) != 0 ? "turn" : null;
            case Control.LeftGrip:
                return (tourScene || museum) && inspecting ? "inspect_move_model" : null;
            case Control.RightGrip:
                if ((tourScene || museum) && inspecting) return "inspect_move_model";
                if (aligning) return "align_place";
                return museum && inspecting ? "rotate_artifact" : null;
            case Control.RightA:
                if (aligning && (context & Context.AlignmentPlaced) != 0) return "align_confirm";
                if (mr) return !busy && (context & Context.Aligning) == 0 ? "toggle_model" : null;
                return (museum && inspecting) || (tourScene && !tourMenu && !busy) ? "zoom_out" : null;
            case Control.RightB:
                if (mr) return "toggle_site_info";
                return (museum && inspecting) || (tourScene && !tourMenu && !busy) ? "zoom_in" : null;
            default: return null;
        }
    }
}
