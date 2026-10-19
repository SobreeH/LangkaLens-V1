import os
from pathlib import Path
from flowchart_builder import FlowchartBuilder

def build_fig_3_4():
    fb = FlowchartBuilder("fig-3-4", "Fig 3.4 - Main Menu Workflow", width=1600, height=1300)
    fb.add_title("Figure 3.4. Programmatic Flowchart: Main Menu Language Confirmation & Scene Loading")
    
    start = fb.add_start("Application Launch", x=690, y=70, w=220, h=55)
    init = fb.add_action("Initialize Main Menu", "LocalizationSettings.InitializationOperation\nMainMenuManager.Awake()", x=660, y=160, w=280, h=70)
    fb.add_edge(start, init)
    
    dec_lang = fb.add_decision("Session HasSelectedLanguage?", "Checks if visitor already confirmed", x=670, y=270, w=260, h=90)
    fb.add_edge(init, dec_lang)
    
    # Left Branch: First time -> Select Language
    show_lang = fb.add_action("Show Language Modal Panel", "languagePanel.SetActive(true)\nHide mode buttons", x=260, y=280, w=260, h=70)
    fb.add_edge(dec_lang, show_lang, label="[FALSE] First time", label_color="#b45309")
    
    click_lang = fb.add_event("Visitor clicks 'Thai' or 'English'", "SelectLanguage(index)", x=260, y=390, w=260, h=70)
    fb.add_edge(show_lang, click_lang)
    
    set_locale = fb.add_action("Update Active Locale", "LocalizationSettings.SelectedLocale = locale\nUpdate UI labels via StringTables", x=260, y=500, w=260, h=75)
    fb.add_edge(click_lang, set_locale)
    
    click_confirm = fb.add_event("Visitor clicks 'Confirm & Continue'", "ConfirmAndContinue()", x=260, y=615, w=260, h=70)
    fb.add_edge(set_locale, click_confirm)
    
    set_flag = fb.add_action("Remember Session Confirmation", "HasSelectedLanguage = true\nlanguagePanel.SetActive(false)", x=260, y=725, w=260, h=70)
    fb.add_edge(click_confirm, set_flag)
    
    # Right/Center: Mode Selection
    show_modes = fb.add_action("Show Experience Mode Selection", "Display Tour, Museum & MR buttons\nShow reusable user guide icon", x=660, y=725, w=280, h=70, accent=True)
    fb.add_edge(dec_lang, show_modes, label="[TRUE] Confirmed", label_color="#16a34a", waypoints=[(800, 420), (800, 710)])
    fb.add_edge(set_flag, show_modes)
    
    click_mode = fb.add_event("Visitor clicks Mode Button", "Tour / Museum / MR", x=670, y=835, w=260, h=70)
    fb.add_edge(show_modes, click_mode)
    
    dec_busy = fb.add_decision("SceneLoadingScreen.IsLoading?", "Guard against double-clicks", x=670, y=945, w=260, h=90)
    fb.add_edge(click_mode, dec_busy)
    
    ignore_click = fb.add_terminal("Ignore Click (Return)", is_error=True, x=1050, y=965, w=180, h=50)
    fb.add_edge(dec_busy, ignore_click, label="[TRUE] Busy loading", label_color="#dc2626")
    
    start_load = fb.add_action("Begin Asynchronous Scene Load", "SceneLoadingScreen.BeginContentLoading()\nFade screen & show spinner", x=650, y=1080, w=300, h=75)
    fb.add_edge(dec_busy, start_load, label="[FALSE] Idle", label_color="#16a34a")
    
    call_scene = fb.add_async_call("Unity SceneManager", "SceneManager.LoadSceneAsync(destSceneName)", x=650, y=1195, w=300, h=75)
    fb.add_edge(start_load, call_scene)
    
    done = fb.add_start("Enter Destination Mode Scene", x=690, y=1310, w=220, h=55)
    fb.add_edge(call_scene, done)
    
    return fb.diagram

def build_fig_3_5():
    fb = FlowchartBuilder("fig-3-5", "Fig 3.5 - Tour Navigation", width=1700, height=1400)
    fb.add_title("Figure 3.5. Programmatic Flowchart: 360-Degree Panoramic Tour Navigation & Viewpoint Transitions")
    
    ev_arrow = fb.add_event("Visitor clicks NavigationArrow", "LoadNode(targetNode, targetWorldPos)", x=700, y=70, w=300, h=70)
    
    dec_state = fb.add_decision("IsNavigationBlocked?", "IsTransitioning || IsReconstructionBusy || IsMapOpen", x=680, y=180, w=340, h=100)
    fb.add_edge(ev_arrow, dec_state)
    
    term_ignore = fb.add_terminal("Ignore Click (No-Op)", is_error=True, x=1180, y=205, w=190, h=50)
    fb.add_edge(dec_state, term_ignore, label="[TRUE] Blocked", label_color="#dc2626")
    
    dec_ref = fb.add_decision("photoReference.RuntimeKeyIsValid()?", "Check Addressable key exists", x=690, y=320, w=320, h=95)
    fb.add_edge(dec_state, dec_ref, label="[FALSE] Idle", label_color="#16a34a")
    
    term_missing = fb.add_terminal("Debug.LogError / Abort", is_error=True, x=1180, y=342, w=190, h=50)
    fb.add_edge(dec_ref, term_missing, label="[FALSE] Invalid", label_color="#dc2626")
    
    prep = fb.add_action("Lock State & Hide Navigation UI", "IsTransitioning = true\nHide active arrows & clear hover icons", x=690, y=455, w=320, h=75)
    fb.add_edge(dec_ref, prep, label="[TRUE] Valid key", label_color="#16a34a")
    
    async_load = fb.add_async_call("Addressables Asset Pipeline", "Addressables.LoadAssetAsync<Texture2D>(photoReference)", x=680, y=565, w=340, h=75)
    fb.add_edge(prep, async_load)
    
    anim_warp = fb.add_action("Start Warp Transition Animation", "Lunge rig forward by 3m (Ease-in)\nStretch PhotoSphere container forward 3x", x=680, y=675, w=340, h=80)
    fb.add_edge(async_load, anim_warp)
    
    dec_load = fb.add_decision("handle.Status == Succeeded?", "Within 30s load deadline", x=690, y=795, w=320, h=95)
    fb.add_edge(anim_warp, dec_load)
    
    # Error Branch
    err_recover = fb.add_action("Error Recovery: Rollback State", "Release pending handle\nReset rig position & restore previous view\nIsTransitioning = false; RefreshNavigation()", x=240, y=795, w=320, h=95, accent=False)
    fb.add_edge(dec_load, err_recover, label="[FALSE] Failed / Timeout", label_color="#dc2626")
    term_err = fb.add_terminal("Keep Current Viewpoint", is_error=True, x=290, y=940, w=220, h=50)
    fb.add_edge(err_recover, term_err)
    
    # Success Branch
    apply_tex = fb.add_action("Apply Destination Panorama", "photoSphereRenderer.material.mainTexture = handle.Result\nRelease previous texture handle from GPU VRAM", x=680, y=930, w=340, h=80, accent=True)
    fb.add_edge(dec_load, apply_tex, label="[TRUE] Succeeded", label_color="#16a34a")
    
    settle = fb.add_action("Settle Rig & Update Tour State", "Ease-out rig to ideal center position\nReset PhotoSphere container scale to 1.0\ncurrentNode = targetNode", x=680, y=1045, w=340, h=80)
    fb.add_edge(apply_tex, settle)
    
    spawn_arrows = fb.add_action("Spawn Navigation Arrows & Hints", "Instantiate ArrowPlacementHelper for connectedPaths\nCheck currentNode reconstruction -> Enable Y hint", x=680, y=1160, w=340, h=80)
    fb.add_edge(settle, spawn_arrows)
    
    unlock = fb.add_action("Unlock Navigation State", "IsTransitioning = false\nNavigation input resumes", x=720, y=1275, w=260, h=65)
    fb.add_edge(spawn_arrows, unlock)
    
    ready = fb.add_start("Viewpoint Ready", x=740, y=1370, w=220, h=55)
    fb.add_edge(unlock, ready)
    
    return fb.diagram

def build_fig_3_6():
    fb = FlowchartBuilder("fig-3-6", "Fig 3.6 - Tour Inspection", width=1700, height=1400)
    fb.add_title("Figure 3.6. Programmatic Flowchart: VR Tour Reconstruction Inspection & Cleanup Lifecycle")
    
    ev_y = fb.add_event("Visitor presses Left Controller Y Button", "TourReconstructionPresenter.ToggleInspection()", x=680, y=70, w=340, h=70)
    
    dec_can = fb.add_decision("CanToggleInspection == true?", "Checks !isTransitioning && !isMapOpen && definition.isConfigured", x=670, y=180, w=360, h=100)
    fb.add_edge(ev_y, dec_can)
    
    term_no_can = fb.add_terminal("Ignore Input (Return)", is_error=True, x=1190, y=205, w=190, h=50)
    fb.add_edge(dec_can, term_no_can, label="[FALSE] Blocked", label_color="#dc2626")
    
    dec_state = fb.add_decision("State == Inspecting || Loading?", "Is reconstruction already open?", x=680, y=320, w=340, h=95)
    fb.add_edge(dec_can, dec_state, label="[TRUE] Allowed", label_color="#16a34a")
    
    # Left Branch: Close Inspection
    close_start = fb.add_action("CloseInspection() Triggered", "Fade panorama backdrop back to 1.0 (Normal)\nDestroy spawned reconstruction model instance\nRelease Addressables instantiate handle", x=220, y=320, w=340, h=95)
    fb.add_edge(dec_state, close_start, label="[TRUE] Active -> Close", label_color="#b45309")
    
    close_clean = fb.add_action("Restore Tour Navigation", "Hide inspection panel tablet\nRe-enable navigation arrows & hover icons\nState = PresentationState.Ready", x=220, y=460, w=340, h=80)
    fb.add_edge(close_start, close_clean)
    
    close_end = fb.add_start("Return to 360 Tour View", x=270, y=580, w=240, h=55)
    fb.add_edge(close_clean, close_end)
    
    # Right/Center Branch: Open Inspection
    open_prep = fb.add_action("OpenInspection() Triggered", "State = PresentationState.Loading\nDisable tour navigation arrows\nCreate inactive staging GameObject parent", x=680, y=460, w=340, h=80)
    fb.add_edge(dec_state, open_prep, label="[FALSE] State == Ready -> Open", label_color="#16a34a")
    
    async_inst = fb.add_async_call("Addressables Asset Pipeline", "Addressables.InstantiateAsync(definition.inspectionPrefab, staging)", x=670, y=575, w=360, h=75)
    fb.add_edge(open_prep, async_inst)
    
    dec_loaded = fb.add_decision("Load Succeeded && !Cancelled?", "Check handle.Status == Succeeded && generation == request", x=670, y=690, w=360, h=100)
    fb.add_edge(async_inst, dec_loaded)
    
    err_load = fb.add_action("Handle Load Failure / Cancellation", "Destroy staging parent & release handle\nState = PresentationState.Ready\nRefresh tour arrows", x=1180, y=690, w=320, h=100, accent=False)
    fb.add_edge(dec_loaded, err_load, label="[FALSE] Failed", label_color="#dc2626")
    term_retry = fb.add_terminal("Return to Ready State", is_error=True, x=1230, y=835, w=220, h=50)
    fb.add_edge(err_load, term_retry)
    
    # Success: Position & Display
    setup_model = fb.add_action("Place Model & Fade Backdrop", "Activate model at viewing distance (0.8m)\nFade panorama backdrop to 0.15 with #533A2D tint\nAttach TourModelTransformer & OneGrabRotate", x=670, y=835, w=360, h=85, accent=True)
    fb.add_edge(dec_loaded, setup_model, label="[TRUE] Loaded", label_color="#16a34a")
    
    setup_ui = fb.add_action("Open Inspection Tablet UI", "Position localized information panel at offset\nSet State = PresentationState.Inspecting", x=670, y=955, w=360, h=75)
    fb.add_edge(setup_model, setup_ui)
    
    loop = fb.add_action("Interactive Inspection Loop", "OneGrabFreeRotateTransformer: Hand rotation\nJoystick Zoom: Scale model 0.5x - 3.0x\nScrollable localized historical description", x=670, y=1065, w=360, h=85)
    fb.add_edge(setup_ui, loop)
    
    ev_exit = fb.add_event("Visitor presses Left Y, B button, or Close", "Exit inspection command", x=700, y=1185, w=300, h=70)
    fb.add_edge(loop, ev_exit)
    
    fb.add_edge(ev_exit, close_start, label="Trigger Close", waypoints=[(500, 1220), (120, 1220), (120, 365), (220, 365)])
    
    return fb.diagram

def build_fig_3_7():
    fb = FlowchartBuilder("fig-3-7", "Fig 3.7 - Museum Exploration", width=1700, height=1400)
    fb.add_title("Figure 3.7. Programmatic Flowchart: VR Museum Exploration, Dimmer Fade & Artifact Inspection")
    
    ev_ray = fb.add_event("Visitor points ray & pulls Trigger / Pinch", "Raycast hits Artifact pedestal collider", x=680, y=70, w=340, h=70)
    
    dec_busy = fb.add_decision("IsInspecting || IsMoving?", "Checks if museum inspection is active", x=690, y=180, w=320, h=95)
    fb.add_edge(ev_ray, dec_busy)
    
    term_busy = fb.add_terminal("Ignore Raycast (Return)", is_error=True, x=1180, y=202, w=190, h=50)
    fb.add_edge(dec_busy, term_busy, label="[TRUE] Busy", label_color="#dc2626")
    
    dec_data = fb.add_decision("artifact.GetComponent<ArtifactData>() != null?", "Check valid museum artifact station", x=670, y=315, w=360, h=95)
    fb.add_edge(dec_busy, dec_data, label="[FALSE] Idle", label_color="#16a34a")
    
    term_no_data = fb.add_terminal("Ignore (Not an artifact)", is_error=True, x=1180, y=337, w=190, h=50)
    fb.add_edge(dec_data, term_no_data, label="[FALSE] Missing", label_color="#dc2626")
    
    start_insp = fb.add_action("Begin Inspection State", "IsInspecting = true; Pause locomotion\nCapture original artifact & player rig transform\nHide pedestal & info icon GameObject", x=670, y=450, w=360, h=85)
    fb.add_edge(dec_data, start_insp, label="[TRUE] Valid", label_color="#16a34a")
    
    dim_env = fb.add_action("Trigger EnvironmentDimmer", "EnvironmentDimmer.Begin(brightness = 0.18, duration = 0.35s)\nFade museum gallery materials to dark backdrop", x=670, y=570, w=360, h=80, accent=True)
    fb.add_edge(start_insp, dim_env)
    
    bring_closer = fb.add_action("Bring Artifact to Inspection Distance", "Smoothly glide model to eye distance (0.6m)\nAttach OneGrabFreeRotateTransformer & Zoom (0.5x - 3.0x)", x=670, y=685, w=360, h=80)
    fb.add_edge(dim_env, bring_closer)
    
    open_canvas = fb.add_action("Open Inspection Canvas Tablet", "Bind LocalizedString title & description\nReset scroll rect position", x=680, y=800, w=340, h=75)
    fb.add_edge(bring_closer, open_canvas)
    
    dec_alt = fb.add_decision("currentArtifactData.alternateModel != null?", "Has alternate / reconstructed form?", x=670, y=915, w=360, h=95)
    fb.add_edge(open_canvas, dec_alt)
    
    # Branch: Show Model Switch
    show_toggle = fb.add_action("Show 'Switch Form' Button", "toggleModelButton.SetActive(true)\nBind modelStateLocalizeEvent", x=240, y=920, w=300, h=80)
    fb.add_edge(dec_alt, show_toggle, label="[TRUE] Has alternate", label_color="#16a34a")
    
    ev_toggle = fb.add_event("Visitor clicks 'Switch Form'", "Swap between Original and Reconstructed mesh", x=240, y=1035, w=300, h=70)
    fb.add_edge(show_toggle, ev_toggle)
    
    # Hide Toggle Branch
    hide_toggle = fb.add_action("Hide 'Switch Form' Button", "toggleModelButton.SetActive(false)", x=700, y=1040, w=300, h=65)
    fb.add_edge(dec_alt, hide_toggle, label="[FALSE] Single model", label_color="#475569")
    
    active_insp = fb.add_action("Active Museum Inspection Loop", "Visitor manipulates 3D artifact with hand grab\nJoystick zoom in/out, reads bilingual info panel", x=470, y=1140, w=360, h=80)
    fb.add_edge(ev_toggle, active_insp)
    fb.add_edge(hide_toggle, active_insp)
    
    ev_close = fb.add_event("Visitor clicks 'Close' / presses B button", "Exit inspection command", x=500, y=1255, w=300, h=65)
    fb.add_edge(active_insp, ev_close)
    
    teardown = fb.add_action("Teardown & Environment Restoration", "EnvironmentDimmer.Restore() -> Fade gallery back to 1.0\nGlide artifact back to pedestal & restore rotation\nRe-enable visitor locomotion; IsInspecting = false", x=460, y=1355, w=380, h=90, accent=True)
    fb.add_edge(ev_close, teardown)
    
    resume = fb.add_start("Resume Museum Exploration", x=540, y=1480, w=220, h=55)
    fb.add_edge(teardown, resume)
    
    return fb.diagram

def build_fig_3_8():
    fb = FlowchartBuilder("fig-3-8", "Fig 3.8 - MR Alignment", width=1700, height=1400)
    fb.add_title("Figure 3.8. Programmatic Flowchart: On-Site Mixed Reality Entrance Alignment & Spatial Anchoring")
    
    start_mr = fb.add_start("Enter On-Site MR Scene", x=700, y=60, w=220, h=55)
    
    dec_track = fb.add_decision("HeadIsTracked() && ControllerTracked()?", "Verify Meta Quest 6DOF tracking", x=660, y=155, w=300, h=95)
    fb.add_edge(start_mr, dec_track)
    
    wait_track = fb.add_action("Wait for Quest Tracking Readiness", "Display tracking & passthrough prompt\nYield until 6DOF poses become valid", x=1160, y=165, w=300, h=75)
    fb.add_edge(dec_track, wait_track, label="[FALSE] Untracked", label_color="#dc2626")
    fb.add_edge(wait_track, dec_track, waypoints=[(1310, 115), (810, 115)])
    
    dec_reset = fb.add_decision("OVRInput: Left X held >= 2.0s?", "Check for manual alignment reset", x=670, y=285, w=280, h=90)
    fb.add_edge(dec_track, dec_reset, label="[TRUE] Tracked", label_color="#16a34a")
    
    do_reset = fb.add_action("Reset Session Anchor", "Destroy active anchorObject\nIsConfirmed = false; HasPlacedPreview = false\nBeginAlignment()", x=240, y=285, w=320, h=90, accent=False)
    fb.add_edge(dec_reset, do_reset, label="[TRUE] Reset held", label_color="#dc2626")
    
    dec_conf = fb.add_decision("IsConfirmed || IsBusy?", "Is entrance alignment already locked?", x=670, y=415, w=280, h=90)
    fb.add_edge(dec_reset, dec_conf, label="[FALSE] Normal", label_color="#16a34a")
    fb.add_edge(do_reset, dec_conf, waypoints=[(400, 460)])
    
    term_locked = fb.add_terminal("Site Aligned (Proceed to Display)", is_error=False, x=1160, y=435, w=240, h=50)
    fb.add_edge(dec_conf, term_locked, label="[TRUE] Confirmed", label_color="#16a34a")
    
    show_wireframe = fb.add_action("Display Entrance Wireframe Outline", "Show reference mesh attached to Right Controller\nPlacementReach = 0.6m", x=660, y=545, w=300, h=75)
    fb.add_edge(dec_conf, show_wireframe, label="[FALSE] Unaligned", label_color="#b45309")
    
    dec_grip = fb.add_decision("Right Hand Trigger Held?", "Is visitor adjusting pose with grip?", x=670, y=660, w=280, h=90)
    fb.add_edge(show_wireframe, dec_grip)
    
    adj_pose = fb.add_action("Adjust Alignment Pose", "Thumbstick X: Rotate yaw (45 deg/s)\nThumbstick Y: Adjust reach distance (0.15m - 3m)\nHasPlacedPreview = true", x=240, y=660, w=320, h=90, accent=True)
    fb.add_edge(dec_grip, adj_pose, label="[TRUE] Grip held", label_color="#16a34a")
    fb.add_edge(adj_pose, dec_grip, waypoints=[(400, 780), (810, 780)])
    
    dec_btn_a = fb.add_decision("HasPlacedPreview && !Grip && Press A?", "Visitor confirms entrance sign alignment", x=650, y=795, w=320, h=95)
    fb.add_edge(dec_grip, dec_btn_a, label="[FALSE] Released", label_color="#475569")
    
    fb.add_edge(dec_btn_a, show_wireframe, label="[FALSE] Still aligning", waypoints=[(1040, 842), (1040, 582)])
    
    start_anchor = fb.add_action("StartCoroutine: ConfirmAlignment()", "IsBusy = true\nPrompt UI: 'Creating spatial anchor...'", x=660, y=930, w=300, h=75)
    fb.add_edge(dec_btn_a, start_anchor, label="[TRUE] A pressed", label_color="#16a34a")
    
    async_anchor = fb.add_async_call("Meta XR Core SDK", "OVRSpatialAnchor.CreateAsync() at aligned pose", x=660, y=1045, w=300, h=75)
    fb.add_edge(start_anchor, async_anchor)
    
    dec_success = fb.add_decision("anchor.Created && Localized?", "Check within 15s deadline", x=670, y=1160, w=280, h=90)
    fb.add_edge(async_anchor, dec_success)
    
    anchor_fail = fb.add_action("Anchor Creation Failed", "Prompt UI: 'Anchor localization failed. Retry.'\nIsBusy = false", x=1160, y=1160, w=300, h=80, accent=False)
    fb.add_edge(dec_success, anchor_fail, label="[FALSE] Failed / Timeout", label_color="#dc2626")
    fb.add_edge(anchor_fail, show_wireframe, waypoints=[(1520, 1200), (1520, 582), (960, 582)])
    
    anchor_ok = fb.add_action("Lock Reference Frame & Spawn Site", "IsConfirmed = true, IsBusy = false\nLock reference root pose to anchor\nTrigger MRModelSpawner.SpawnAlignedModel()", x=650, y=1290, w=320, h=85, accent=True)
    fb.add_edge(dec_success, anchor_ok, label="[TRUE] Localized", label_color="#16a34a")
    
    mr_active = fb.add_start("Active MR Site Reconstruction", x=700, y=1415, w=220, h=55)
    fb.add_edge(anchor_ok, mr_active)
    
    return fb.diagram

def build_fig_3_9():
    fb = FlowchartBuilder("fig-3-9", "Fig 3.9 - MR Display Recovery", width=1700, height=1400)
    fb.add_title("Figure 3.9. Programmatic Flowchart: MR Reconstruction Display Gating & Tracking Recovery Loop")
    
    tick = fb.add_event("Unity Update() Frame Tick", "MRMeshPlayableArea / MRModelSpawner", x=680, y=70, w=340, h=65)
    
    dec_conf = fb.add_decision("Alignment.IsConfirmed == true?", "Has entrance alignment completed?", x=690, y=175, w=320, h=90)
    fb.add_edge(tick, dec_conf)
    
    hide_unaligned = fb.add_action("Hide Models & Show Wireframe", "siteModelPrefab.SetActive(false)\nKeep alignment prompt active", x=240, y=175, w=320, h=80)
    fb.add_edge(dec_conf, hide_unaligned, label="[FALSE] Not aligned", label_color="#b45309")
    term_align = fb.add_terminal("Awaiting Alignment", is_error=True, x=290, y=295, w=220, h=50)
    fb.add_edge(hide_unaligned, term_align)
    
    dec_head = fb.add_decision("HeadIsTracked() == true?", "Check Meta 6DOF headset tracking", x=690, y=305, w=320, h=90)
    fb.add_edge(dec_conf, dec_head, label="[TRUE] Aligned", label_color="#16a34a")
    
    hide_lost_track = fb.add_action("Tracking Lost: Suppress Virtual Content", "Hide site models & UI panels\nShow Quest system boundary warning", x=1180, y=305, w=320, h=80, accent=False)
    fb.add_edge(dec_head, hide_lost_track, label="[FALSE] Lost 6DOF", label_color="#dc2626")
    term_track = fb.add_terminal("Wait for Tracking Relocalization", is_error=True, x=1220, y=425, w=240, h=50)
    fb.add_edge(hide_lost_track, term_track)
    
    dec_anchor = fb.add_decision("anchor.IsTracked && Localized?", "Check spatial anchor tracking state", x=690, y=435, w=320, h=90)
    fb.add_edge(dec_head, dec_anchor, label="[TRUE] Head tracked", label_color="#16a34a")
    
    anchor_lost = fb.add_action("Anchor Tracking Degraded", "Hide site models to prevent visual drifting\nWait for anchor re-localization", x=1180, y=435, w=320, h=80, accent=False)
    fb.add_edge(dec_anchor, anchor_lost, label="[FALSE] Anchor lost", label_color="#dc2626")
    
    dec_pass = fb.add_decision("PassthroughReady && Active?", "Ensure real world passthrough is rendered", x=690, y=565, w=320, h=90)
    fb.add_edge(dec_anchor, dec_pass, label="[TRUE] Anchor tracked", label_color="#16a34a")
    
    pass_wait = fb.add_action("Wait for Passthrough Feed", "Suppress virtual overlay until camera ready", x=1180, y=565, w=320, h=75)
    fb.add_edge(dec_pass, pass_wait, label="[FALSE] Blackout", label_color="#dc2626")
    
    # All Conditions Met -> Render
    render_all = fb.add_action("Render Aligned Site Models", "Enable MeshRenderers on siteModelPrefab\nApply unrestricted footprint (Free walking)\nUpdate MRSiteInformationPanel billboard to head", x=670, y=695, w=360, h=90, accent=True)
    fb.add_edge(dec_pass, render_all, label="[TRUE] All guards pass", label_color="#16a34a")
    
    check_btn_a = fb.add_decision("OVRInput: Button A Pressed?", "Toggle reconstruction model visibility", x=690, y=825, w=320, h=90)
    fb.add_edge(render_all, check_btn_a)
    
    toggle_model = fb.add_action("Toggle Model Visibility", "siteModelPrefab.SetActive(!activeSelf)", x=240, y=830, w=300, h=75)
    fb.add_edge(check_btn_a, toggle_model, label="[TRUE] Pressed", label_color="#16a34a")
    
    check_btn_b = fb.add_decision("OVRInput: Button B Pressed?", "Toggle architectural information tablet", x=690, y=955, w=320, h=90)
    fb.add_edge(check_btn_a, check_btn_b, label="[FALSE]")
    fb.add_edge(toggle_model, check_btn_b, waypoints=[(390, 930), (850, 930)])
    
    toggle_panel = fb.add_action("Toggle Info Panel Visibility", "siteInfoPanel.SetActive(!activeSelf)", x=240, y=960, w=300, h=75)
    fb.add_edge(check_btn_b, toggle_panel, label="[TRUE] Pressed", label_color="#16a34a")
    
    check_reset = fb.add_decision("OVRInput: Left X held >= 2.0s?", "Trigger manual alignment reset", x=690, y=1085, w=320, h=90)
    fb.add_edge(check_btn_b, check_reset, label="[FALSE]")
    fb.add_edge(toggle_panel, check_reset, waypoints=[(390, 1060), (850, 1060)])
    
    do_reset = fb.add_action("Trigger BeginAlignment()", "Reset anchor & restart alignment", x=1180, y=1090, w=300, h=75, accent=False)
    fb.add_edge(check_reset, do_reset, label="[TRUE] Reset held", label_color="#dc2626")
    
    frame_end = fb.add_start("End of Frame (Yield)", x=740, y=1215, w=220, h=55)
    fb.add_edge(check_reset, frame_end, label="[FALSE] Normal")
    
    return fb.diagram

def build_fig_3_10():
    fb = FlowchartBuilder("fig-3-10", "Fig 3.10 - Shared AI Workflow", width=1700, height=1500)
    fb.add_title("Figure 3.10. Programmatic Flowchart: Multimodal Shared AI Heritage Guide Workflow")
    
    open_guide = fb.add_event("Visitor opens AI Guide Canvas", "Tablet button or quick-action icon", x=700, y=60, w=300, h=65)
    
    dec_input = fb.add_decision("Visitor selects input method?", "Speak button (Voice) vs Virtual Keyboard (Text)", x=680, y=165, w=340, h=95)
    fb.add_edge(open_guide, dec_input)
    
    # Left Branch: Voice STT
    dec_rec = fb.add_decision("IsRecording == true?", "Toggle speech recording", x=250, y=295, w=280, h=90)
    fb.add_edge(dec_input, dec_rec, label="[VOICE] Speak button", label_color="#1d4ed8")
    
    stop_rec = fb.add_action("Stop Recording & Process", "Encode microphone samples to WAV", x=50, y=420, w=260, h=75)
    fb.add_edge(dec_rec, stop_rec, label="[TRUE] Active -> Stop", label_color="#b45309")
    
    dec_perm = fb.add_decision("RECORD_AUDIO Permission Granted?", "Android microphone runtime permission", x=350, y=420, w=280, h=90)
    fb.add_edge(dec_rec, dec_perm, label="[FALSE] Start new", label_color="#16a34a")
    
    req_perm = fb.add_action("Request Microphone Permission", "Permission.RequestUserPermission()", x=680, y=425, w=260, h=75)
    fb.add_edge(dec_perm, req_perm, label="[FALSE] Denied", label_color="#dc2626")
    
    start_rec = fb.add_action("Start Microphone Recording", "Microphone.Start(deviceName, loop=false, 15s, 16000)\nShow pulsing recording waveform indicator", x=340, y=550, w=300, h=80)
    fb.add_edge(dec_perm, start_rec, label="[TRUE] Granted", label_color="#16a34a")
    
    wav_enc = fb.add_action("Encode Audio to 16kHz WAV", "Convert float audio samples to 16-bit PCM WAV", x=240, y=665, w=300, h=75)
    fb.add_edge(stop_rec, wav_enc)
    fb.add_edge(start_rec, wav_enc)
    
    async_groq = fb.add_async_call("Groq Cloud Whisper API", "POST /v1/audio/transcriptions (whisper-large-v3)", x=240, y=775, w=300, h=75)
    fb.add_edge(wav_enc, async_groq)
    
    dec_groq = fb.add_decision("Groq HTTP Status == 200 OK?", "Check transcription API response", x=250, y=890, w=280, h=90)
    fb.add_edge(async_groq, dec_groq)
    
    groq_err = fb.add_terminal("Show Error Toast (Retry)", is_error=True, x=40, y=910, w=180, h=50)
    fb.add_edge(dec_groq, groq_err, label="[FALSE] Error", label_color="#dc2626")
    
    pop_input = fb.add_action("Populate Input Field", "inputField.text = transcriptionResult\nEnable visitor editing and correction", x=240, y=1020, w=300, h=80, accent=True)
    fb.add_edge(dec_groq, pop_input, label="[TRUE] Success", label_color="#16a34a")
    
    # Right Branch: Typed input
    type_kbd = fb.add_action("Bilingual Virtual Keyboard", "Click inputField -> open virtual keyboard\nVisitor types question in Thai or English", x=1150, y=295, w=300, h=80)
    fb.add_edge(dec_input, type_kbd, label="[TYPED] Keyboard", label_color="#475569")
    
    # Merge at Review
    review = fb.add_action("Review & Edit Question", "Visitor reviews text in inputField before submitting", x=700, y=1025, w=300, h=70)
    fb.add_edge(pop_input, review)
    fb.add_edge(type_kbd, review, waypoints=[(1300, 1060)])
    
    ev_send = fb.add_event("Visitor clicks 'Send' button", "GeminiChatUI.OnSendClicked()", x=700, y=1130, w=300, h=65)
    fb.add_edge(review, ev_send)
    
    dec_empty = fb.add_decision("string.IsNullOrWhiteSpace(text)?", "Check question is not blank", x=700, y=1230, w=300, h=85)
    fb.add_edge(ev_send, dec_empty)
    
    term_blank = fb.add_terminal("Ignore Click (Blank)", is_error=True, x=1180, y=1247, w=190, h=50)
    fb.add_edge(dec_empty, term_blank, label="[TRUE] Empty", label_color="#dc2626")
    
    dec_inflight = fb.add_decision("requestPending == true?", "Is another query currently running?", x=700, y=1350, w=300, h=85)
    fb.add_edge(dec_empty, dec_inflight, label="[FALSE] Valid text", label_color="#16a34a")
    
    cancel_req = fb.add_action("Cancel In-Flight Request", "currentWebRequest.Abort()\nReset Send button icon; requestPending = false", x=1180, y=1350, w=320, h=80, accent=False)
    fb.add_edge(dec_inflight, cancel_req, label="[TRUE] Stop requested", label_color="#b45309")
    
    build_req = fb.add_action("Construct Prompt & Payload", "Append question to chat log (Cyan badge)\nDetect locale -> Select thaiSystemPrompt or English\nBuild JSON: { contents, generationConfig: { maxOutputTokens } }", x=670, y=1475, w=360, h=90)
    fb.add_edge(dec_inflight, build_req, label="[FALSE] Idle -> Send", label_color="#16a34a")
    
    async_gemini = fb.add_async_call("Google Gemini API", "POST gemini-3.5-flash-lite:generateContent", x=680, y=1605, w=340, h=75)
    fb.add_edge(build_req, async_gemini)
    
    dec_gemini_ok = fb.add_decision("request.result == Success (200)?", "Check Gemini HTTP response", x=680, y=1720, w=340, h=90)
    fb.add_edge(async_gemini, dec_gemini_ok)
    
    # Fallback branch
    dec_fallback = fb.add_decision("enableFallback && !usingFallback?", "HTTP 404, 408, 429, or 5xx error?", x=240, y=1720, w=320, h=90)
    fb.add_edge(dec_gemini_ok, dec_fallback, label="[FALSE] API Error", label_color="#dc2626")
    
    do_fallback = fb.add_async_call("Gemini Fallback Model", "Retry POST gemini-3.1-flash-lite", x=240, y=1850, w=320, h=75)
    fb.add_edge(dec_fallback, do_fallback, label="[TRUE] Fallback enabled", label_color="#1d4ed8")
    
    show_err = fb.add_action("Display Localized Error", "Show error badge in chat log + 'Check Connection'", x=20, y=1850, w=200, h=75, accent=False)
    fb.add_edge(dec_fallback, show_err, label="[FALSE] Fallback off", label_color="#dc2626")
    
    show_resp = fb.add_action("Display AI Guide Response", "Parse candidates[0].content.parts[0].text\nAppend response to chat log (Emerald badge)\nAuto-scroll ScrollRect to bottom", x=670, y=1850, w=360, h=85, accent=True)
    fb.add_edge(dec_gemini_ok, show_resp, label="[TRUE] HTTP 200 OK", label_color="#16a34a")
    fb.add_edge(do_fallback, show_resp, label="Fallback OK", label_color="#16a34a")
    
    cleanup = fb.add_action("Reset State & Clear Input", "requestPending = false; usingFallback = false\nRestore Send button icon; inputField.text = ''", x=700, y=1975, w=300, h=75)
    fb.add_edge(show_resp, cleanup)
    fb.add_edge(show_err, cleanup, waypoints=[(120, 2012)])
    
    ready_end = fb.add_start("Ready for Next Question", x=740, y=2090, w=220, h=55)
    fb.add_edge(cleanup, ready_end)
    
    return fb.diagram

print("Flowchart builders defined!")
