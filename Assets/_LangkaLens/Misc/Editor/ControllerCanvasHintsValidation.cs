using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using C = ControllerCanvasHints.Control;
using S = ControllerCanvasHints.Context;

public static class ControllerCanvasHintsValidation
{
    [MenuItem("LangkaLens/Validation/Controller Hints")]
    public static void ValidateAll()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Run outside Play Mode with saved scenes.");
        string original = SceneManager.GetActiveScene().path;
        int checks = 0;
        var table = LocalizationEditorSettings.GetStringTableCollection(ControllerCanvasHints.TableName);
        Require(table != null && table.SharedData.Entries.Count >= 18, "Missing hint table entries.");
        foreach (string language in new[] { "en", "th" })
        {
            var translated = table.StringTables.Single(t => t.LocaleIdentifier.Code == language);
            foreach (var key in table.SharedData.Entries)
            {
                Require(!string.IsNullOrWhiteSpace(translated.GetEntry(key.Id)?.Value), language + ": " + key.Key);
                checks++;
            }
        }
        try
        {
            foreach (var name in new[] { "Main_Menu_Scene", "MR_Scene", "VR_Museum_Scene", "VR_Tour_Scene" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_LangkaLens/_Scenes/" + name + ".unity", OpenSceneMode.Single);
                var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ControllerCanvasHints>(true)).ToArray();
                Require(components.Length == 1, name + ": expected one hint owner.");
                var hints = components[0];
                Require(hints.Labels.Length == 11 && hints.Labels.Select(l => l.control).Distinct().Count() == 11, name + ": incomplete/duplicate controls.");
                foreach (var label in hints.Labels)
                {
                    var e = label.localized;
                    Require(e != null && e.OnUpdateString.GetPersistentEventCount() == 1, name + ": missing or duplicate localization listener.");
                    Require(e.OnUpdateString.GetPersistentMethodName(0) == "set_text" &&
                            e.OnUpdateString.GetPersistentTarget(0) == e.GetComponent<TMPro.TMP_Text>() &&
                            e.OnUpdateString.GetPersistentListenerState(0) == UnityEventCallState.EditorAndRuntime,
                            name + ": wrong text target or inactive listener.");
                    checks++;
                }
                foreach (var state in Scenarios(name))
                {
                    hints.ApplyContext(name, state);
                    foreach (var label in hints.Labels.Where(l => l.localized.gameObject.activeSelf))
                    {
                        foreach (var translated in table.StringTables)
                        {
                            var e = label.localized;
                            var entry = translated.GetEntry(e.StringReference.TableEntryReference.Key);
                            Require(entry != null && !string.IsNullOrEmpty(entry.Value), name + ": invalid active key.");
                            e.OnUpdateString.Invoke(entry.Value);
                            var text = e.GetComponent<TMPro.TMP_Text>();
                            Require(text.text == entry.Value, name + ": string event did not update label.");
                            uint[] missing;
                            Require(text.font.HasCharacters(entry.Value, out missing, true, false), name + ": missing " + translated.LocaleIdentifier.Code + " glyphs.");
                            text.ForceMeshUpdate();
                            Require(!text.isTextOverflowing, name + ": overflowing " + label.control + " in " + translated.LocaleIdentifier.Code);
                            checks++;
                        }
                    }
                    // Reapplying the same state must leave the binding intact.
                    hints.ApplyContext(name, state);
                }
                if (name == "Main_Menu_Scene")
                {
                    Check(hints, name, S.Move | S.Turn, C.LeftX, null);
                    Check(hints, name, S.Move | S.Turn, C.LeftTrigger, "select");
                    Check(hints, name, S.Move | S.Turn, C.RightA, null);
                }
                if (name == "MR_Scene")
                {
                    Check(hints, name, S.AlignmentAvailable | S.Aligning, C.RightA, null);
                    Check(hints, name, S.AlignmentAvailable | S.Aligning | S.AlignmentPlaced, C.RightA, "align_confirm");
                    Check(hints, name, S.AlignmentAvailable | S.Aligning | S.Busy, C.RightGrip, null);
                    Check(hints, name, S.AlignmentAvailable, C.RightStick, null);
                    Check(hints, name, S.AlignmentAvailable | S.ConfirmExit, C.LeftX, "mr_confirm_home");
                }
                if (name == "VR_Museum_Scene")
                {
                    Check(hints, name, S.Move | S.Turn, C.LeftX, "home");
                    Check(hints, name, S.Move | S.Turn, C.RightA, null);
                    Check(hints, name, S.Inspecting, C.LeftX, "exit_inspection");
                    Check(hints, name, S.Inspecting, C.LeftY, null);
                    Check(hints, name, S.Inspecting | S.AlternateModel, C.LeftY, "switch_model");
                    Check(hints, name, S.Move | S.Turn, C.LeftGrip, null);
                    Check(hints, name, S.Inspecting, C.LeftGrip, "inspect_move_model");
                    Check(hints, name, S.Inspecting, C.RightGrip, "inspect_move_model");
                    Check(hints, name, S.Inspecting, C.RightStick, "inspect_turn_reset");
                }
                if (name == "VR_Tour_Scene")
                {
                    Check(hints, name, S.TourMenu, C.LeftX, "home");
                    Check(hints, name, S.None, C.LeftX, "exit_tour");
                    Check(hints, name, S.None, C.LeftStick, "teleport");
                    Check(hints, name, S.Busy, C.LeftStick, null);
                    Check(hints, name, S.TourMenu, C.RightA, null);
                    Check(hints, name, S.TourMenu, C.LeftY, null);
                    Check(hints, name, S.None, C.LeftY, null);
                    Check(hints, name, S.ReconstructionAvailable, C.LeftY, "inspect_reconstruction");
                    Check(hints, name, S.ReconstructionAvailable | S.Busy, C.LeftY, "cancel_reconstruction");
                    Check(hints, name, S.ReconstructionAvailable | S.Inspecting, C.LeftY, "exit_inspection");
                    Check(hints, name, S.TourMenu | S.ConfirmExit, C.LeftX, "confirm_home");
                    var tour = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TourManager>(true)).Single();
                    Require(tour.tourOnlyLabels.Length == 0, "TourManager still competes for label visibility.");
                }
                hints.ApplyContext("UnconfiguredScene", S.None);
                Require(hints.Labels.All(l => !l.localized.gameObject.activeSelf), "Unsupported scene leaked hints.");
                hints.RefreshHints();
                Debug.Log(name + ": controller hint bindings, state transitions, both locales and text fit passed.");
            }
        }
        finally { EditorSceneManager.OpenScene(original, OpenSceneMode.Single); }
        Debug.Log("Controller hints passed: " + checks + " binding/translation/layout checks plus 25 state assertions.");
    }

    private static void Check(ControllerCanvasHints hints, string scene, S context, C control, string expected)
    {
        hints.ApplyContext(scene, context);
        var label = hints.Labels.Single(l => l.control == control).localized;
        Require(label.gameObject.activeSelf == (expected != null), scene + ": incorrect visibility for " + control);
        if (expected != null) Require(label.StringReference.TableEntryReference.Key == expected, scene + ": incorrect action for " + control);
    }

    private static IEnumerable<S> Scenarios(string name)
    {
        yield return S.Tablet | S.Move | S.Turn | (name == "VR_Tour_Scene" ? S.TourMenu : S.None);
        yield return S.Tablet | S.ConfirmExit | (name == "VR_Tour_Scene" ? S.TourMenu : S.None);
        if (name == "MR_Scene")
        {
            yield return S.Tablet | S.AlignmentAvailable | S.Aligning;
            yield return S.Tablet | S.AlignmentAvailable | S.Aligning | S.AlignmentPlaced;
            yield return S.Tablet | S.AlignmentAvailable | S.Aligning | S.Busy;
            yield return S.Tablet | S.AlignmentAvailable | S.ConfirmExit;
        }
        if (name == "VR_Museum_Scene")
        {
            yield return S.Tablet | S.Inspecting;
            yield return S.Tablet | S.Inspecting | S.AlternateModel;
        }
        if (name == "VR_Tour_Scene")
        {
            yield return S.Tablet | S.Turn;
            yield return S.Tablet | S.Busy;
            yield return S.Tablet | S.ReconstructionAvailable;
            yield return S.Tablet | S.ReconstructionAvailable | S.Busy;
            yield return S.Tablet | S.ReconstructionAvailable | S.Inspecting;
        }
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
