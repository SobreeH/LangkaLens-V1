# AI Prompt Specification: LangkaLens Project Flowchart

This document provides a **master AI prompt** engineered to generate an exhaustive, publication-grade **Flowchart** (Architecture, State Transitions, and User Journey) for the **LangkaLens** Unity VR/MR application.

---

## 🛠️ How to Use This Prompt
1. **For Text-to-Diagram AIs (Claude 3.7 / ChatGPT Plus / Gemini 2.0 Pro):**
   Copy the Master Prompt below and paste it into your AI assistant. It is instructed to generate both **Mermaid.js** code (for instant rendering in GitHub/Notion/Mermaid Live Editor) and **PlantUML/Graphviz** syntax.
2. **For Visual Flowchart Tools (Whimsical / Miro / FigJam AI):**
   Paste the Section 2 & 3 summary into the AI Diagram generator prompt box.

---

## 📋 Master Prompt (Copy & Paste Below)

```text
You are a Principal XR Software Architect and Systems Engineer specializing in Unity 6, Meta Quest XR SDK (All-In-One), and Interactive VR/MR Heritage Applications.

Your task is to generate a comprehensive, highly detailed multi-level FLOWCHART for the project named "LangkaLens".

### Project Overview:
- Name: LangkaLens (Virtual Reality & Mixed Reality Cultural Heritage Experience)
- Subject: Archaeological ruins and artifacts of the Ancient Yarang City (Pattani, Thailand)
- Target Platform: Meta Quest 2/3/Pro (Android Standalone)
- Engine & SDKs: Unity 6 (6000.3.16f1), URP, Meta XR All-In-One SDK (v203.0.2), Unity Addressables, Unity Localization, Google Gemini Generative Language API.

### Core Systems & Architecture to Include in the Flowchart:
1. Application Lifecycle & Entry:
   - App Launch & OpenXR Initialization
   - PlayerPrefs Check for Language ('HasSelectedLanguage')
   - First-time Language Setup (Thai / English) via Unity LocalizationSettings vs. Direct Main Menu Bypass
   - Scene Loader Asynchronous Dispatcher (MainMenuManager)
   - Headset Recenter & Spatial Pose Alignment (RigResetter)

2. VR Tour Mode (360 Photosphere Street-View Engine):
   - Loading Area Data (BN2, BN3, BN8, BW9 ScriptableObjects)
   - Street View Transition Sequence (TourManager):
     * User Raycast on NavigationArrow
     * 3D Directional Lunge Calculation (Ease-In)
     * Photosphere Mesh Warp & Pinch Transformation (narrowMultiplier 0.5x, stretchMultiplier 3.0x)
     * Active Arrow / Overlay Model Cleanup
     * Addressables Asynchronous Texture Download & Material Update
     * Spawn Navigation Arrows (previousPath, nextPath)
     * Addressables 3D Reconstructed Model Instantiation
     * Camera View Orientation Reset & Ease-Out Settle
   - Interactive 3D Model Ghost Overlay (HoverIcon hover enter/exit triggers 3D reconstruction overlay)
   - VR Mini-Map Menu Toggle via Left Controller 'Y' Button

3. VR Museum Mode (Artifact Exhibition & Inspection):
   - Universal One-Handed Locomotion (Single joystick auto-detect, Gaze-directed CharacterController translation + Yaw rotation)
   - Pedestal Proximity & Artifact Interaction (ArtifactData):
     * User Raycast / Touch on Artifact
     * Locomotion Lock
     * Smooth Rig Translation (0.4s glide directly facing pedestal forward vector at 0.6m distance)
     * Spatial UI Anchor alignment (Title, Historical Description, Audio)
     * Meta Interaction SDK Grabbable Hand/Controller Rotation (OneGrabFreeRotateTransformer: idle auto-spin pauses when held, resumes on release)
     * Exit Inspection State -> Locomotion Unlock -> Rig Return

4. Mixed Reality (MR) Mode (Independent Spatial Experience):
   - Meta Passthrough Layer Underlay (OVRPassthroughLayer)
   - Controller & Hand Tracking building blocks
   - Real-World Room spatial integration (Blending physical environment with ancient Yarang digital heritage)
   - Gesture interaction & spatial raycasting

5. Gemini AI Heritage Guide (Independent Intelligence & Dialogue Flow):
   - Standalone AI Assistant Service (GeminiChatUI / GeminiController)
   - User Query Submission (Voice Dictation / Spatial Virtual Keyboard)
   - System Persona Prompt ("Expert Yarang AI Guide, strictly <= 2 sentences")
   - Parameter Config (maxOutputTokens: 150)
   - Asynchronous UnityWebRequest POST to Google Generative Language API
   - Response Handling: Deserialization of candidates[0].content.parts, Error Catching
   - Rich-Text Color Formatting & Dynamic ScrollRect Auto-Snapping to newest message

6. Global Navigation & Sub-Menus:
   - Left Controller Menu Button ('Start') -> SubMenuToggle / Tablet UI
   - Return to Main Menu / Scene Changer
   - Exit Application Handler

### Requirements for the Flowchart Output:
1. Provide a comprehensive Mermaid.js diagram (`flowchart TD`) with distinct subgraphs clearly separating:
   - App Lifecycle & Main Menu Hub
   - VR 360 Tour System
   - VR Museum Exhibition System
   - Mixed Reality (MR) Experience
   - Independent Gemini AI Guide Pipeline
2. Clearly show that the Gemini AI Guide is an independent system/service with its own dedicated request/response flow.
3. Use distinct node styling for processes, decisions, user inputs, data storage, and AI processing.
```

---

## 📊 Pre-Generated Reference Mermaid Flowchart
Below is the ready-to-render baseline architecture diagram produced by this prompt:

```mermaid
flowchart TD
    %% Global Styling
    classDef startEnd fill:#1e293b,stroke:#38bdf8,stroke-width:2px,color:#fff;
    classDef decision fill:#854d0e,stroke:#facc15,stroke-width:2px,color:#fff;
    classDef process fill:#1e3a8a,stroke:#60a5fa,stroke-width:2px,color:#fff;
    classDef xrAction fill:#065f46,stroke:#34d399,stroke-width:2px,color:#fff;
    classDef dataStore fill:#4c1d95,stroke:#a78bfa,stroke-width:2px,color:#fff;
    classDef aiSystem fill:#831843,stroke:#f472b6,stroke-width:2px,color:#fff;

    Start([Headset Donning / App Boot]):::startEnd --> InitXR[Initialize OpenXR & Meta SDK]:::process
    InitXR --> LoadMainMenu[Load Main_Menu_Scene]:::process
    LoadMainMenu --> RigReset[RigResetter: Recenter Tracking Pose]:::xrAction
    RigReset --> CheckLang{PlayerPrefs:<br/>HasSelectedLanguage == 1?}:::decision

    %% Language Setup Flow
    CheckLang -- No (First Run) --> ShowLangPanel[Display Language Selection Panel]:::process
    ShowLangPanel --> UserPicksLang[/User Selects Thai or English/]:::xrAction
    UserPicksLang --> SetLocale[Set LocalizationSettings.SelectedLocale]:::process
    SetLocale --> EnableOK[Enable OK Button]:::process
    EnableOK --> ConfirmLang[Confirm & Save PlayerPrefs = 1]:::dataStore
    ConfirmLang --> ShowMainMenu[Display Spatial Main Menu Hub]:::process
    CheckLang -- Yes --> ShowMainMenu

    %% Main Menu Hub Selection
    ShowMainMenu --> MenuChoice{User Selection}:::decision
    MenuChoice -- "VR Tour Mode" --> LoadVRTour[SceneManager.LoadSceneAsync: VR_Tour_Scene]:::process
    MenuChoice -- "VR Museum Mode" --> LoadVRMuseum[SceneManager.LoadSceneAsync: VR_Museum_Scene]:::process
    MenuChoice -- "MR Mode" --> LoadMR[SceneManager.LoadSceneAsync: MR_Scene]:::process
    MenuChoice -- "AI Heritage Guide" --> LaunchAI[Open Dedicated AI Guide System]:::process
    MenuChoice -- "Language Settings" --> ShowLangPanel
    MenuChoice -- "Quit App" --> QuitApp([Application.Quit]):::startEnd

    %% ==========================================
    %% VR TOUR SYSTEM FLOW
    %% ==========================================
    subgraph VR_TOUR ["🏛️ VR 360 Tour Navigation System"]
        LoadVRTour --> InitTour[TourManager Initialized]:::process
        InitTour --> LoadTitlePhoto[(Addressables: Load Title 360 Texture)]:::dataStore
        LoadTitlePhoto --> ShowMapMenu[Display Tour Map Menu Canvas]:::process
        ShowMapMenu --> SelectArea[/User Selects Area: BN2 / BN3 / BN8 / BW9/]:::xrAction
        SelectArea --> LoadStartNode[Fetch Starting TourNode Asset]:::dataStore
        
        LoadStartNode --> TransitionStart[Begin Street-View Warp Transition]:::process
        TransitionStart --> CalcLunge[Calculate Forward Lunge & Pinch/Stretch Mesh]:::process
        CalcLunge --> ClearOld[Destroy Previous Arrows & Overlay Instances]:::process
        ClearOld --> StreamPhoto[(Addressables: Load Next 360 Photo Texture)]:::dataStore
        StreamPhoto --> ApplyTex[Apply to PhotoSphere Material]:::process
        ApplyTex --> SpawnArrows[Spawn NavigationArrow Prefabs in 3D Space]:::process
        
        SpawnArrows --> CheckOverlay{TourNode has<br/>overlayModel?}:::decision
        CheckOverlay -- Yes --> Load3DOverlay[(Addressables: Instantiate 3D Reconstructed Model)]:::dataStore
        Load3DOverlay --> SpawnHover[Spawn HoverIcon at Coordinates]:::process
        CheckOverlay -- No --> SettleView[Reset Camera Offset & Settle Ease-Out]:::process
        SpawnHover --> SettleView

        SettleView --> TourInteraction{User In-Tour Interaction}:::decision
        TourInteraction -- "Click Navigation Arrow" --> CalcTargetNode[Get Target TourNode]:::process --> TransitionStart
        TourInteraction -- "Hover on 3D Icon" --> ShowOverlay[TourManager.ShowOverlay: Display 3D Reconstructed Ancient Temple]:::xrAction
        TourInteraction -- "Unhover 3D Icon" --> HideOverlay[TourManager.HideOverlay: Hide 3D Model]:::xrAction
        TourInteraction -- "Press Controller Y Button" --> ToggleMap[Toggle In-Tour Map Menu]:::xrAction
        TourInteraction -- "Press Menu / Return" --> ReturnMain1[ReturnToMainMenu]:::process --> LoadMainMenu
    end

    %% ==========================================
    %% VR MUSEUM SYSTEM FLOW
    %% ==========================================
    subgraph VR_MUSEUM ["🏺 VR Museum Exhibition & Inspection System"]
        LoadVRMuseum --> InitMuseum[InspectionManager & Universal Locomotion Ready]:::process
        InitMuseum --> LocomotionLoop[/One-Handed Locomotion: Left/Right Thumbstick/]:::xrAction
        LocomotionLoop --> MoveRig[CharacterController.SimpleMove along Headset Forward + Yaw Turn]:::process
        MoveRig --> ArtifactIdle[Artifacts Rotate with OneGrabFreeRotateTransformer: idleRotationSpeed]:::process

        MoveRig --> SelectArtifact[/User Clicks / Points at Artifact Pedestal/]:::xrAction
        SelectArtifact --> EnterInspect[InspectionManager.EnterInspectionState]:::process
        EnterInspect --> LockMovement[Disable Universal Locomotion]:::process
        LockMovement --> GlideCamera[Smooth Camera Glide: 0.4s to 0.6m in front of Pedestal]:::process
        GlideCamera --> AlignUI[Position & Rotate InspectCanvas at Artifact uiAnchor]:::process
        AlignUI --> DisplayData[Populate Localized Title & Historical Description Text]:::process
        
        DisplayData --> InspectInteraction{User Inspection Actions}:::decision
        InspectInteraction -- "Grab Artifact (Hand/Controller)" --> PauseIdle[Pause Idle Spin -> Rotate Delta Free Transform]:::xrAction
        InspectInteraction -- "Release Artifact" --> ResumeIdle[Resume Auto Idle Spin]:::process
        InspectInteraction -- "Click Back / Exit Button" --> ExitInspect[InspectionManager.ExitInspectionState]:::process
        ExitInspect --> RestoreRig[Restore Rig Position & Unlock Locomotion]:::process --> LocomotionLoop
        InspectInteraction -- "Press Left Menu Button" --> ReturnMain2[Load Main_Menu_Scene]:::process --> LoadMainMenu
    end

    %% ==========================================
    %% MIXED REALITY (MR) INDEPENDENT FLOW
    %% ==========================================
    subgraph MR_MODE ["🥽 Mixed Reality (MR) Spatial Mode"]
        LoadMR --> InitMR[Enable Meta Passthrough Layer & Hand Tracking]:::process
        InitMR --> RenderPassthrough[Display Real-World Video Feed Underlay]:::process
        RenderPassthrough --> SpawnMRAssets[Spawn Interactive Spatial Portals & Ancient Artifacts in Real Room]:::xrAction
        SpawnMRAssets --> MRInteraction{User Spatial Interactions}:::decision
        MRInteraction -- "Hand Pinch / Poke" --> ManipulateMR[Direct Spatial Manipulation of 3D Models]:::xrAction
        MRInteraction -- "Raycast Selection" --> ExploreMR[Inspect Room-Scale Heritage Reconstruction]:::xrAction
        MRInteraction -- "Return to Hub" --> ReturnMain3[Load Main_Menu_Scene]:::process --> LoadMainMenu
    end

    %% ==========================================
    %% INDEPENDENT GEMINI AI GUIDE PIPELINE
    %% ==========================================
    subgraph AI_GUIDE ["🤖 Independent Gemini AI Heritage Assistant System"]
        LaunchAI --> InitAIChat[Initialize GeminiChatUI & Load System Persona]:::process
        InitAIChat --> DisplayChatUI[Render Floating Spatial Chat Tablet UI]:::process
        
        DisplayChatUI --> UserQueryInput[/User Inputs Question: Text or Speech-to-Text/]:::xrAction
        UserQueryInput --> LogUserMsg[Append User Query to ChatLog in Cyan Text]:::process
        LogUserMsg --> BuildPayload[Construct GeminiRequest JSON with System Prompt & maxTokens=150]:::process
        
        BuildPayload --> SendAPIReq[(Asynchronous UnityWebRequest POST to Gemini API Endpoint)]:::aiSystem
        SendAPIReq --> CheckAPIResponse{API Response Status}:::decision
        
        CheckAPIResponse -- "200 Success" --> ExtractReply[Deserialize JSON: Parse candidates[0].content.parts[0].text]:::process
        ExtractReply --> AppendReply[Append AI Reply to ChatLog with Green Guide Tag]:::process
        AppendReply --> ScrollBottom[Canvas.ForceUpdateCanvases & snap ScrollRect to bottom]:::process
        
        CheckAPIResponse -- "Network / Server Error" --> HandleErr[Format Error Message in Red Text to ChatLog]:::process
        HandleErr --> ScrollBottom
        
        ScrollBottom --> AIOptions{User AI Actions}:::decision
        AIOptions -- "Ask Follow-up Question" --> UserQueryInput
        AIOptions -- "Close / Return" --> ReturnMain4[Close AI Assistant / Return to Hub]:::process --> LoadMainMenu
    end
```
