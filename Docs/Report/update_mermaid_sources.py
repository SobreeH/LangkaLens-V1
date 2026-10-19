from pathlib import Path

ROOT_DIR = Path(r"e:\Unity\Projects\LangkaLens")
MMD_DIR = ROOT_DIR / "Docs" / "Report" / "extracted_diagrams" / "mermaid"

mermaid_diagrams = {
    "Fig_3_04_Main_Menu_Workflow.mmd": """flowchart TD
    Start(["Launch Application"]) --> Init["Initialize LocalizationSettings & MainMenuManager"]
    Init --> CheckLang{"if (HasSelectedLanguage)?"}
    
    CheckLang -->|"False (First run)"| ShowLang["Show Language Selection Panel<br><i>languagePanel.SetActive(true)</i>"]
    ShowLang --> ClickLang["⚡ Visitor selects Thai or English<br><i>SelectLanguage(index)</i>"]
    ClickLang --> UpdateLocale["Update Active Locale<br><i>LocalizationSettings.SelectedLocale</i>"]
    UpdateLocale --> ClickConfirm["⚡ Visitor clicks 'Confirm & Continue'<br><i>ConfirmAndContinue()</i>"]
    ClickConfirm --> SaveConfirm["Remember Confirmation in Session<br><i>HasSelectedLanguage = true</i>"]
    SaveConfirm --> ShowModes
    
    CheckLang -->|"True (Confirmed)"| ShowModes["Display Mode Selection<br><i>Show Tour, Museum & MR buttons</i>"]
    
    ShowModes --> ClickMode["⚡ Visitor clicks Mode Button (Tour / Museum / MR)"]
    ClickMode --> CheckBusy{"if (SceneLoadingScreen.IsLoading)?"}
    
    CheckBusy -->|"True (Busy)"| IgnoreClick(["Ignore click / Return early"]):::error
    CheckBusy -->|"False (Idle)"| BeginLoad["Begin Content Loading<br><i>SceneLoadingScreen.BeginContentLoading()</i>"]
    
    BeginLoad --> LoadAsync["Call SceneManager.LoadSceneAsync(destScene)"]
    LoadAsync --> Done(["Enter Destination Experience Scene"]):::success

    classDef error fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#991b1b;
    classDef success fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#166534;
""",

    "Fig_3_05_Tour_Navigation.mmd": """flowchart TD
    EvClick["⚡ Visitor clicks NavigationArrow<br><i>LoadNode(targetNode, targetWorldPos)</i>"]
    
    EvClick --> CheckBlocked{"if (IsNavigationBlocked)?<br><i>IsTransitioning || IsReconstructionBusy || IsMapOpen</i>"}
    CheckBlocked -->|"True (Blocked)"| IgnoreClick(["Ignore click / Return early"]):::error
    
    CheckBlocked -->|"False (Idle)"| CheckKey{"if (photoReference.RuntimeKeyIsValid())?"}
    CheckKey -->|"False (Invalid)"| LogKeyErr(["Debug.LogError / Abort"]):::error
    
    CheckKey -->|"True (Valid)"| LockState["Lock Navigation & Clear UI<br><i>IsTransitioning = true</i><br><i>Hide active arrows & hover icons</i>"]
    LockState --> AsyncLoad["⚙️ Addressables Asset Pipeline<br><i>Addressables.LoadAssetAsync&lt;Texture2D&gt;(photoReference)</i>"]
    AsyncLoad --> StartWarp["Start Warp Transition Animation<br><i>Lunge rig forward 3m (Ease-in)</i><br><i>Stretch PhotoSphere 3x forward</i>"]
    
    StartWarp --> CheckLoaded{"if (handle.Status == Succeeded)?<br><i>Within 30s deadline</i>"}
    
    CheckLoaded -->|"False (Failed / Timeout)"| RecoverErr["Error Recovery:<br><i>Release pending handle</i><br><i>Restore previous camera position</i><br><i>IsTransitioning = false; RefreshNavigation()</i>"]
    RecoverErr --> KeepPlace(["Keep Current Viewpoint"]):::error
    
    CheckLoaded -->|"True (Succeeded)"| ApplyTex["Apply Destination Panorama<br><i>photoSphereRenderer.material.mainTexture = handle.Result</i><br><i>Release previous texture handle from VRAM</i>"]
    ApplyTex --> SettleRig["Settle Rig & Update Tour State<br><i>Ease-out rig to ideal center position</i><br><i>Reset PhotoSphere container scale to 1.0</i><br><i>currentNode = targetNode</i>"]
    SettleRig --> SpawnArrows["Spawn Navigation Arrows & Hints<br><i>Instantiate arrows for connectedPaths</i><br><i>Check currentNode reconstruction -> Enable Y hint</i>"]
    SpawnArrows --> UnlockNav["Unlock Navigation State<br><i>IsTransitioning = false</i>"]
    UnlockNav --> Ready(["Viewpoint Ready for Visitor Input"]):::success

    classDef error fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#991b1b;
    classDef success fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#166534;
""",

    "Fig_3_06_Tour_Inspection.mmd": """flowchart TD
    EvY["⚡ Visitor presses Left Controller Y Button<br><i>TourReconstructionPresenter.ToggleInspection()</i>"]
    
    EvY --> CheckCan{"if (CanToggleInspection)?<br><i>!isTransitioning && !isMapOpen && definition.isConfigured</i>"}
    CheckCan -->|"False (Blocked)"| IgnoreY(["Ignore button press / Return"]):::error
    
    CheckCan -->|"True (Allowed)"| CheckActive{"if (State == Inspecting || Loading)?"}
    
    %% Close Branch
    CheckActive -->|"True (Already active)"| TriggerClose["Trigger CloseInspection():<br><i>Fade panorama backdrop back to 1.0</i><br><i>Destroy reconstruction model instance</i><br><i>Release Addressables instantiate handle</i>"]
    TriggerClose --> RestoreTour["Restore Tour State:<br><i>Hide inspection panel tablet</i><br><i>Re-enable navigation arrows & hover icons</i><br><i>State = PresentationState.Ready</i>"]
    RestoreTour --> TourView(["Return to 360 Panorama View"]):::success
    
    %% Open Branch
    CheckActive -->|"False (Ready -> Open)"| StartLoad["OpenInspection():<br><i>State = PresentationState.Loading</i><br><i>Disable tour navigation arrows</i><br><i>Create inactive staging GameObject</i>"]
    StartLoad --> AsyncInst["⚙️ Addressables Asset Pipeline<br><i>Addressables.InstantiateAsync(definition.inspectionPrefab, staging)</i>"]
    
    AsyncInst --> CheckInstOK{"if (handle.Status == Succeeded && !cancelled)?"}
    CheckInstOK -->|"False (Failed / Cancelled)"| LoadErr["Destroy staging parent & release handle<br><i>State = PresentationState.Ready; RefreshNavigation()</i>"]
    LoadErr --> TourView
    
    CheckInstOK -->|"True (Loaded)"| SetupModel["Activate Model & Fade Backdrop:<br><i>Position model at inspectionViewingDistance (0.8m)</i><br><i>Fade panorama backdrop to 0.15 with #533A2D tint</i><br><i>Attach TourModelTransformer & OneGrabRotate</i>"]
    SetupModel --> ShowUI["Open Inspection Tablet UI<br><i>Position localized info panel at offset</i><br><i>State = PresentationState.Inspecting</i>"]
    
    ShowUI --> InspectLoop["Active Inspection Interaction Loop:<br><i>• OneGrabFreeRotateTransformer: Hand grab rotation</i><br><i>• Joystick Zoom: Scale model 0.5x - 3.0x</i><br><i>• Scrollable bilingual historical description</i>"]
    
    InspectLoop --> EvExit["⚡ Visitor presses Left Y, B button, or Close tablet"]
    EvExit --> TriggerClose

    classDef error fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#991b1b;
    classDef success fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#166534;
""",

    "Fig_3_07_Museum_Exploration.mmd": """flowchart TD
    EvRay["⚡ Visitor points ray & pulls Trigger / Pinch<br><i>Raycast hits Artifact pedestal collider</i>"]
    
    EvRay --> CheckBusy{"if (IsInspecting || IsMoving)?"}
    CheckBusy -->|"True (Busy)"| IgnoreRay(["Ignore Raycast / Return"]):::error
    
    CheckBusy -->|"False (Idle)"| CheckData{"if (artifact.GetComponent&lt;ArtifactData&gt;() != null)?"}
    CheckData -->|"False (Not an exhibit)"| IgnoreNotExhibit(["Return"]):::error
    
    CheckData -->|"True (Valid)"| StartInsp["Begin Inspection State:<br><i>IsInspecting = true; Pause locomotion</i><br><i>Capture original artifact & player rig transform</i><br><i>Hide pedestal & info icon GameObject</i>"]
    
    StartInsp --> DimEnv["Trigger EnvironmentDimmer:<br><i>EnvironmentDimmer.Begin(brightness = 0.18, duration = 0.35s)</i><br><i>Fade museum gallery materials to dark backdrop</i>"]
    
    DimEnv --> GlideModel["Bring Artifact to Inspection Distance:<br><i>Smoothly glide model to eye distance (0.6m)</i><br><i>Attach OneGrabFreeRotateTransformer & Zoom (0.5x - 3.0x)</i>"]
    
    GlideModel --> OpenUI["Open Inspection Canvas Tablet:<br><i>Bind LocalizedString title & description</i><br><i>Reset scroll rect position</i>"]
    
    OpenUI --> CheckAlt{"if (currentArtifactData.alternateModel != null)?"}
    
    CheckAlt -->|"True (Has alternate)"| ShowToggle["Show 'Switch Form' Toggle Button<br><i>toggleModelButton.SetActive(true)</i>"]
    ShowToggle --> ClickToggle["⚡ Visitor clicks 'Switch Form'<br><i>Swap between Original and Reconstructed mesh</i>"]
    ClickToggle --> ActiveLoop
    
    CheckAlt -->|"False (Single model)"| HideToggle["Hide 'Switch Form' Button<br><i>toggleModelButton.SetActive(false)</i>"]
    HideToggle --> ActiveLoop["Active Museum Inspection Loop:<br><i>Visitor manipulates 3D artifact with hand grab</i><br><i>Joystick zoom in/out, reads bilingual info panel</i>"]
    
    ActiveLoop --> EvClose["⚡ Visitor clicks 'Close' / presses B button"]
    
    EvClose --> Teardown["Teardown & Environment Restoration:<br><i>EnvironmentDimmer.Restore() -> Fade gallery back to 1.0</i><br><i>Glide artifact back to pedestal & restore rotation</i><br><i>Re-enable visitor locomotion; IsInspecting = false</i>"]
    
    Teardown --> Resume(["Resume Museum Gallery Exploration"]):::success

    classDef error fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#991b1b;
    classDef success fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#166534;
""",

    "Fig_3_08_MR_Alignment.mmd": """flowchart TD
    StartMR(["Enter On-Site MR Scene"]) --> CheckTrack{"if (HeadIsTracked() && ControllerTracked())?"}
    CheckTrack -->|"False (Untracked)"| WaitTrack["Wait for Quest Tracking Readiness<br><i>Display tracking prompt & yield</i>"]
    WaitTrack --> CheckTrack
    
    CheckTrack -->|"True (Tracked)"| CheckReset{"if (Left X held &gt;= 2.0s)?"}
    CheckReset -->|"True (Reset)"| DoReset["Reset Session Anchor:<br><i>Destroy active anchorObject</i><br><i>IsConfirmed = false; HasPlacedPreview = false</i><br><i>BeginAlignment()</i>"]
    
    CheckReset -->|"False (Normal)"| CheckConf{"if (IsConfirmed || IsBusy)?"}
    DoReset --> CheckConf
    CheckConf -->|"True (Confirmed)"| ActiveMR(["Site Aligned & Active"]):::success
    
    CheckConf -->|"False (Unaligned)"| ShowWire["Display Entrance Wireframe Outline<br><i>Show reference mesh attached to Right Controller</i>"]
    ShowWire --> CheckGrip{"if (Right Hand Trigger Held)?"}
    
    CheckGrip -->|"True (Grip held)"| AdjPose["Adjust Alignment Pose:<br><i>Thumbstick X: Rotate yaw (45 deg/s)</i><br><i>Thumbstick Y: Adjust reach distance (0.15m - 3m)</i><br><i>HasPlacedPreview = true</i>"]
    AdjPose --> CheckGrip
    
    CheckGrip -->|"False (Released)"| CheckA{"if (HasPlacedPreview && !Grip && Press A)?"}
    CheckA -->|"False (Still aligning)"| ShowWire
    
    CheckA -->|"True (Confirmed)"| StartAnchor["StartCoroutine: ConfirmAlignment()<br><i>IsBusy = true</i><br><i>Prompt UI: 'Creating spatial anchor...'</i>"]
    
    StartAnchor --> AsyncAnchor["⚙️ Meta XR Core SDK<br><i>OVRSpatialAnchor.CreateAsync() at aligned pose</i>"]
    
    AsyncAnchor --> CheckAnchorOK{"if (anchor.Created && anchor.Localized)?<br><i>Within 15s deadline</i>"}
    CheckAnchorOK -->|"False (Failed / Timeout)"| AnchorFail["Anchor Creation Failed:<br><i>Prompt UI: 'Localization failed. Retry.'</i><br><i>IsBusy = false</i>"]
    AnchorFail --> ShowWire
    
    CheckAnchorOK -->|"True (Localized)"| AnchorOK["Lock Reference Frame & Spawn Site:<br><i>IsConfirmed = true, IsBusy = false</i><br><i>Lock reference root pose to spatial anchor</i><br><i>Trigger MRModelSpawner.SpawnAlignedModel()</i>"]
    AnchorOK --> ActiveMR

    classDef error fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#991b1b;
    classDef success fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#166534;
""",

    "Fig_3_09_MR_Display_Recovery.mmd": """flowchart TD
    Tick["⚡ Unity Update() Frame Tick<br><i>MRMeshPlayableArea / MRModelSpawner</i>"]
    
    Tick --> CheckConf{"if (Alignment.IsConfirmed)?"}
    CheckConf -->|"False (Not aligned)"| HideUnaligned["Hide Site Models & Show Wireframe<br><i>siteModelPrefab.SetActive(false)</i>"]
    HideUnaligned --> WaitAlign(["Awaiting Alignment"]):::error
    
    CheckConf -->|"True (Aligned)"| CheckHead{"if (HeadIsTracked())?"}
    CheckHead -->|"False (Lost 6DOF)"| HideTrack["Tracking Lost: Suppress Virtual Content<br><i>Hide site models & UI panels</i><br><i>Show Quest system boundary warning</i>"]
    HideTrack --> WaitReloc(["Wait for Relocalization"]):::error
    
    CheckHead -->|"True (Head tracked)"| CheckAnchor{"if (anchor.IsTracked && Localized)?"}
    CheckAnchor -->|"False (Anchor lost)"| HideAnchor["Anchor Tracking Degraded<br><i>Hide site models to prevent drift</i>"]
    HideAnchor --> WaitReloc
    
    CheckAnchor -->|"True (Anchor tracked)"| CheckPass{"if (PassthroughReady && Active)?"}
    CheckPass -->|"False (Blackout)"| WaitPass["Wait for Passthrough Feed<br><i>Suppress virtual overlay</i>"]
    WaitPass --> WaitReloc
    
    CheckPass -->|"True (All guards pass)"| RenderModels["Render Aligned Site Models:<br><i>• Enable MeshRenderers on siteModelPrefab</i><br><i>• Apply unrestricted footprint (Free walking)</i><br><i>• Update MRSiteInformationPanel billboard to head</i>"]
    
    RenderModels --> CheckBtnA{"if (Button A Pressed)?"}
    CheckBtnA -->|"True"| ToggleModel["Toggle Reconstruction Model<br><i>siteModelPrefab.SetActive(!active)</i>"]
    
    CheckBtnA -->|"False"| CheckBtnB{"if (Button B Pressed)?"}
    ToggleModel --> CheckBtnB
    CheckBtnB -->|"True"| TogglePanel["Toggle Site Information Tablet<br><i>siteInfoPanel.SetActive(!active)</i>"]
    
    CheckBtnB -->|"False"| CheckReset{"if (Left X held &gt;= 2.0s)?"}
    TogglePanel --> CheckReset
    CheckReset -->|"True"| DoReset["Trigger BeginAlignment()<br><i>Reset anchor & restart alignment</i>"]
    
    CheckReset -->|"False"| FrameEnd(["End of Frame (Yield)"]):::success

    classDef error fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#991b1b;
    classDef success fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#166534;
""",

    "Fig_3_10_Shared_AI_Workflow.mmd": """flowchart TD
    OpenGuide["⚡ Visitor opens AI Guide Canvas<br><i>Tablet button or quick-action icon</i>"]
    
    OpenGuide --> CheckMethod{"Visitor selects input method?"}
    
    %% Voice Branch
    CheckMethod -->|"Voice (Speak Button)"| CheckRec{"if (IsRecording)?"}
    CheckRec -->|"True (Active)"| StopRec["Stop Recording & Encode WAV"]
    CheckRec -->|"False (Idle)"| CheckPerm{"if (RECORD_AUDIO Permission)?"}
    CheckPerm -->|"False (Denied)"| ReqPerm["Request Android RECORD_AUDIO Permission"]
    CheckPerm -->|"True (Granted)"| StartRec["Record Microphone Audio<br><i>Microphone.Start(16kHz mono, up to 15s)</i><br><i>Show pulsing waveform indicator</i>"]
    
    StopRec --> EncWav["Encode Audio Samples to 16kHz WAV"]
    StartRec --> EncWav
    
    EncWav --> AsyncGroq["⚙️ Groq Cloud Whisper API<br><i>POST /v1/audio/transcriptions (whisper-large-v3)</i>"]
    AsyncGroq --> CheckGroq{"if (HTTP Status == 200 OK)?"}
    CheckGroq -->|"False (Error)"| GroqErr(["Show Error Toast / Allow Retry"]):::error
    CheckGroq -->|"True (Success)"| PopInput["Populate Input Field<br><i>inputField.text = transcriptText</i><br><i>Enable visitor editing & correction</i>"]
    
    %% Typed Branch
    CheckMethod -->|"Typed (Keyboard)"| VirtualKbd["Bilingual Virtual Keyboard<br><i>Visitor types question in Thai or English</i>"]
    
    %% Merge at Review
    PopInput --> Review["Review & Edit Question in inputField"]
    VirtualKbd --> Review
    
    Review --> ClickSend["⚡ Visitor clicks 'Send' button<br><i>GeminiChatUI.OnSendClicked()</i>"]
    
    ClickSend --> CheckEmpty{"if (string.IsNullOrWhiteSpace(text))?"}
    CheckEmpty -->|"True (Empty)"| IgnoreEmpty(["Ignore click / Do nothing"]):::error
    
    CheckEmpty -->|"False (Valid)"| CheckInflight{"if (requestPending)?"}
    CheckInflight -->|"True (In-flight)"| CancelReq["Cancel Request:<br><i>currentWebRequest.Abort()</i><br><i>Reset Send button icon; requestPending = false</i>"]
    
    CheckInflight -->|"False (Idle -> Send)"| BuildPrompt["Construct Prompt & Payload:<br><i>Append question to chat log (Cyan badge)</i><br><i>Detect locale -> Select thaiSystemPrompt or English</i><br><i>Build JSON: { contents, maxOutputTokens: 150 }</i>"]
    
    BuildPrompt --> AsyncGemini["⚙️ Google Gemini API<br><i>POST gemini-3.5-flash-lite:generateContent</i>"]
    
    AsyncGemini --> CheckGemini{"if (request.result == Success 200)?"}
    
    %% Fallback Branch
    CheckGemini -->|"False (API Error)"| CheckFallback{"if (enableFallback && !usingFallback)?<br><i>HTTP 404, 408, 429, 5xx</i>"}
    CheckFallback -->|"True (Fallback on)"| AsyncFallback["⚙️ Retry Gemini Fallback Model<br><i>POST gemini-3.1-flash-lite</i>"]
    CheckFallback -->|"False (Fallback off)"| ShowErr["Display Localized Error in Chat Log<br><i>Show error badge + 'Check Connection'</i>"]
    
    %% Success
    CheckGemini -->|"True (200 OK)"| DisplayResp["Display AI Guide Response:<br><i>Parse response text</i><br><i>Format markdown & append to log (Emerald badge)</i><br><i>Auto-scroll ScrollRect to bottom</i>"]
    AsyncFallback --> DisplayResp
    
    DisplayResp --> Cleanup["Reset State & Clear Input:<br><i>requestPending = false; usingFallback = false</i><br><i>Restore Send button icon; inputField.text = ''</i>"]
    ShowErr --> Cleanup
    
    Cleanup --> ReadyNext(["Ready for Next Question"]):::success

    classDef error fill:#fef2f2,stroke:#dc2626,stroke-width:2px,color:#991b1b;
    classDef success fill:#f0fdf4,stroke:#16a34a,stroke-width:2px,color:#166534;
"""
}

for fn, content in mermaid_diagrams.items():
    with open(MMD_DIR / fn, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"Updated Mermaid definition: {fn}")

print("All Mermaid flowchart definitions updated successfully!")
