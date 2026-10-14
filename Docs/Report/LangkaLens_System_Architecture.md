# 1. System Architecture: LangkaLens Cultural Heritage Platform
## 1.1 System Analysis and Design & 1.2 System Development

**Academic Context:** Senior Project Technical Specification & Architectural Report  
**Author:** Wansobree Hajihama (Student ID: 6620610023)  
**Project Advisor:** Assistant Professor Dr. Zamree Che-aron (ผศ.ดร.ซัมรี เจ๊ะอารน)  
**Academic Program:** Bachelor of Science in Computer and Information Science for Management (CSM)  
**Institution:** Faculty of Communication Sciences, Prince of Songkla University, Pattani Campus  
**Target Hardware:** Meta Quest 3 & Meta Quest 3S (Horizon OS / Android 12L) | **Engine:** Unity 6000.3.16f1 URP  

---

# 1. System Architecture

LangkaLens is a high-performance standalone spatial computing platform developed for the Meta Quest 3 and Meta Quest 3S head-mounted displays (HMDs). The system preserves, interprets, and communicates the rich cultural and archaeological heritage of Ancient Yarang City (Ban Jalae and Ban Wat archaeological complexes in Pattani, Southern Thailand). Archaeological sites in Yarang frequently feature surviving brick foundations, moat contours, and weathered architectural baselines, but lack complete standing upper structures. To solve this visual and educational challenge, LangkaLens integrates three complementary experiential modalities within a unified application:

1. **VR 360-Degree Panoramic Photographic Tour:** High-resolution equirectangular photographic exploration covering Ban Jalae 2, Ban Jalae 3, Ban Jalae 8, and Ban Wat 9.
2. **Interactive VR Museum:** 6-degree-of-freedom (6-DOF) examination, manipulation, dynamic lighting dimming, and dual-mesh alternate LOD comparisons of digitised Yarang artifacts.
3. **On-Site Mixed Reality (MR) Reconstruction:** 1:1 scale interpreted architectural reconstructions overlaid directly onto physical archaeological ruins using color video passthrough and hardware-tracked spatial session anchors.

The overarching architectural design adheres to a decoupled, modular service-oriented structure implemented in Unity 6 (6000.3.16f1) utilizing the Universal Render Pipeline (URP). Standalone XR headsets operate under strict thermal envelopes and mobile hardware budgets (Snapdragon XR2 Gen 2 SoC). Consequently, the architecture prioritizes:
- **Scene Modularity:** Isolates rendering state across four core scenes (`Main_Menu_Scene`, `VR_Tour_Scene`, `VR_Museum_Scene`, `MR_Scene`).
- **Asynchronous Asset Lifecycle:** Utilizes Unity Addressables for on-demand texture loading and aggressive memory deallocation.
- **Decoupled Interaction Helpers:** Leverages Meta XR Interaction SDK for natural 6-DOF single-hand model manipulation (`OneGrabFreeRotateTransformer`).
- **External Multimodal AI Microservices:** Connects local speech recording (Groq Whisper) and contextual large language model generation (Google Gemini REST API) asynchronously without blocking the rendering thread.

> **Architectural Tenet:** LangkaLens strictly decouples core presentation scenes from external cloud intelligence. All 3D models, panoramic spheres, localization assets, and spatial alignment parameters execute client-side on the Meta Quest headset. Cloud AI services (Gemini chat and Groq transcription) operate asynchronously with robust timeout, cancellation, and local fallback routines to ensure application stability under unpredictable field network conditions.

---

## 1.1 System Analysis and Design

System Analysis and Design establishes the foundational requirements, structural paradigms, behavioral models, and data schemas that govern the LangkaLens ecosystem. This section analyzes user needs, formalizes functional and non-functional specifications, models dynamic workflows through UML activity diagrams and sequence charts, and delineates the data dictionary governing spatial ScriptableObjects.

### 1.1.1 Requirements Engineering & Analysis

The engineering requirements for LangkaLens were derived from field visits to Ancient Yarang City, consultations with archaeological literature from the Fine Arts Department of Thailand, and spatial ergonomics standards for mobile VR headsets.

#### Functional Requirements (FR)

| ID | Functional Requirement | Subsystem & Implementation Driver | Verification Standard |
|---|---|---|---|
| **FR-01** | Bilingual Locale & Mode Selection | MainMenuManager; Unity Localization 1.5.12 StringTables | Persistent language selection across session; loads selected mode asynchronously. |
| **FR-02** | 360-Degree Panoramic Navigation | TourManager; TourNode ScriptableObjects; Addressables | Smooth viewpoint traversal via directional indicators; releases previous texture handles. |
| **FR-03** | VR Reconstruction Inspection | TourReconstructionPresenter; InspectionManager | Left Y button toggles 3D model; provides 6-DOF rotation, scaling, and bilingual info. |
| **FR-04** | VR Museum Artifact Manipulation | ArtifactData; OneGrabFreeRotateTransformer; Dimmer | Displays artifacts on pedestals; dims environment during inspection; toggles alternate LODs. |
| **FR-05** | On-Site MR Entrance Alignment | MRMeshReferenceAlignment; OVRSpatialAnchor API | Curator aligns bounding reference with physical sign/table; creates session spatial anchor. |
| **FR-06** | MR Tracking & Display Gating | MRMeshPlayableArea; MRModelSpawner; Passthrough | Continuous validation of head tracking, anchor localization, and passthrough; unrestricted mode. |
| **FR-07** | Multimodal AI Heritage Guide | GeminiChatUI; Google Gemini REST API client | Contextual archaeological chat; localized system prompt injection; HTTP 429/5xx fallback. |
| **FR-08** | Custom Voice Speech-to-Text | SpeechToTextHandler; Groq Whisper Cloud API | 16kHz PCM WAV capture; transcribes speech into editable input field prior to query submission. |

#### Non-Functional Requirements (NFR)

| ID | Category | Target Specification & Metric | Architectural Strategy |
|---|---|---|---|
| **NFR-01** | Performance & Frame Rate | Consistent 72 FPS / 90 FPS; frame time <= 13.8 ms | URP Forward+ rendering, ASTC 6x6 compression, single-pass instanced stereo rendering. |
| **NFR-02** | Memory Management | Peak heap footprint < 2.5 GB; zero VRAM leaks | Unity Addressables async loading and handle release; texture cache garbage collection. |
| **NFR-03** | Spatial Ergonomics | Comfort FOV (30-60 deg); 1.2m-2.0m UI canvas distance | World-space canvas anchoring; vergence-accommodation conflict mitigation. |
| **NFR-04** | Cybersickness Mitigation | Zero involuntary camera motion; snap turning support | Instantaneous or quadratic ease-in transitions; universal one-handed locomotion. |
| **NFR-05** | Fault Tolerance & Resilience | Offline recovery; automatic fallback API models | Single-attempt retry on HTTP 404/408/429/5xx; non-blocking coroutine timeouts. |
| **NFR-06** | Bilingual Parity | 100% lexical symmetry between Thai and English | Strict parity across StringTable collections for museum, tour, and UI labels. |

### 1.1.2 Conceptual & High-Level System Architecture

The high-level architecture of LangkaLens follows a 4-tier layered model:
1. **Tier 1: Spatial Presentation & UX Layer:** Encompasses world-space Canvas hierarchies, TextMesh Pro bilingual typography, Meta XR Passthrough visual layers, spatial reticles, and audio feedback. All user interfaces are scaled to 0.001f world units to match physical-scale optics.
2. **Tier 2: Application & Domain Logic Layer:** Comprises dedicated MonoBehaviour state machines controlling scene orchestration: `MainMenuManager` for lifecycle routing, `TourManager` for graph navigation, `InspectionManager` and `InspectionEnvironmentDimmer` for 6-DOF exhibit interaction, `MRMeshReferenceAlignment` and `MRMeshPlayableArea` for physical-to-virtual anchoring, and `GeminiChatUI` for conversational guidance.
3. **Tier 3: Hardware Abstraction & XR Engine Layer:** Powered by Unity 6 Engine and the Meta XR All-In-One SDK (v203.0.2). This tier interfaces with the Snapdragon XR2 Gen 2 SoC, translating 6-DOF Inside-Out SLAM tracking, optical passthrough buffers, spatial anchor hardware, and bimanual Touch Plus controller events into standard OpenXR and Meta XR input streams.
4. **Tier 4: Data & External Cloud Services Layer:** Splits into offline serialized assets (ScriptableObjects for tour nodes, photogrammetric asset bundles managed by Addressables) and secure RESTful microservice gateways connecting to Google Gemini API (conversational synthesis) and Groq Cloud (Whisper ASR).

![Figure 1.1: High-Level System Architecture and External Services Topology](figures/diagrams/figure_3_1.png)

### 1.1.3 Actor Profiles and Functional Use Cases

System actors represent entities interacting with LangkaLens:
- **Cultural Heritage Visitor / Learner:** Primary human actor (general public, students, tourists) engaging in tour navigation, museum inspection, and conversational inquiry.
- **Field Researcher / Site Curator:** Secondary human actor responsible for calibrating on-site physical-to-virtual alignment.
- **External AI Microservices:** Automated cloud endpoints (Google Gemini and Groq Whisper).

![Figure 1.2: System Functions and Use Case Diagram for Visitors and Curators](figures/diagrams/figure_3_2.png)

### 1.1.4 System Workflow & Behavioral Design (Activity Flowcharts)

#### Application Lifecycle & Scene Orchestration
The application follows a centralized hub-and-spoke execution pattern. Launching the application initializes `Main_Menu_Scene`. Visitors can navigate into any of the three experiential scenes. Return requests prompt confirmation before restoring the main menu and releasing mode-specific assets.

![Figure 1.3: Application Overview and Scene Navigation Flowchart](figures/diagrams/figure_3_3.png)

#### Main Menu & Session Language Confirmation Workflow
The menu workflow enforces session-scoped language persistence. When launched, the system checks whether language has been confirmed during the current application session. If unconfirmed, the bilingual selection dialog is displayed. Subsequent returns to the main menu skip this prompt.

![Figure 1.4: Main Menu Language Selection and Scene Loading Flowchart](figures/diagrams/figure_3_4.png)

#### VR Tour Viewpoint Traversal & Map Navigation Workflow
Tour navigation operates as an event-driven state machine. Selecting a linked arrow disables user locomotion, loads the destination equirectangular texture asynchronously, animates camera container movement, disposes of the prior texture handle, and activates new navigation indicators upon successful arrival.

![Figure 1.5: VR 360 Panoramic Tour Navigation Flowchart](figures/diagrams/figure_3_5.png)

#### VR Tour Restored-Model Inspection Workflow
Contextual 3D reconstruction inspection is available at key archaeological viewpoints. Pressing left controller Y loads the reconstruction display prefab via Addressables. If the user moves away or cancels, asynchronous loads are aborted and discarded.

![Figure 1.6: VR Tour Reconstruction Inspection and Memory Cleanup Flowchart](figures/diagrams/figure_3_6.png)

#### VR Museum Artifact Exploration & Dimmer Lifecycle Workflow
When an artifact is selected, player locomotion is locked, pre-inspection camera poses are cached, and `InspectionEnvironmentDimmer` initiates an exposure fade on the surrounding gallery while keeping the artifact illuminated. Closing inspection restores gallery lighting and returns the artifact to its pedestal.

![Figure 1.7: VR Museum Artifact Exploration and Environment Dimmer Flowchart](figures/diagrams/figure_3_7.png)

#### Mixed Reality Entrance Alignment & Session Anchor Workflow
The curator aligns a virtual entrance outline to physical signage using the right controller grip and thumbstick. Confirming with button A initiates hardware spatial anchor creation with a 15-second timeout window. Upon localization, the relative transformation matrix is locked for the session.

![Figure 1.8: On-Site Mixed Reality Entrance Alignment and Anchor Flowchart](figures/diagrams/figure_3_8.png)

#### MR Model Display Gating & Tracking Recovery Workflow
In the saved unrestricted configuration, model display requires alignment confirmation, active head tracking, and passthrough readiness. If tracking quality drops or passthrough is interrupted, the reconstructed building automatically conceals itself until SLAM tracking re-establishes stability.

![Figure 1.9: MR Model Display Gating and Tracking Recovery Flowchart](figures/diagrams/figure_3_9.png)

#### Shared Multimodal AI Heritage Guide Workflow
Users may type questions or dictate speech. Audio recordings are transmitted to Groq Whisper for transcription and returned to an editable text field. This allows users to review and amend the transcription before submitting the prompt to Google Gemini.

![Figure 1.10: Shared Multimodal AI Heritage Guide Workflow](figures/diagrams/figure_3_10.png)

### 1.1.5 Dynamic Process & Interaction Design (Sequence Diagrams)

#### Process 1: Tour Viewpoint Transition & Asynchronous Asset Loading
`TourManager` locks navigation inputs, requests the destination `Texture2D` handle via Addressables, smoothly translates the player rig, releases the preceding texture from GPU memory, and instantiates destination directional indicators upon completion.

![Figure 1.11: Sequence Diagram: Asynchronous Tour Viewpoint Transition](figures/diagrams/figure_3_11.png)

#### Process 2: Museum Artifact Inspection & Environment Dimming
`InspectionManager` signals `InspectionEnvironmentDimmer` to execute a non-blocking shader lerp on gallery materials using unscaled delta time. Simultaneously, player locomotion is frozen, the 6-DOF transformer is enabled, and bilingual TextMesh Pro metadata panels are populated.

![Figure 1.12: Sequence Diagram: Museum Artifact Inspection and Dimmer Lifecycle](figures/diagrams/figure_3_12.png)

#### Process 3: MR Spatial Alignment & Session Anchoring
The hardware spatial anchor localization state is polled asynchronously. Once locked, `MRMeshPlayableArea` updates its `CanPlay` state, permitting `MRModelSpawner` to instantiate the Ban Jalae 3 reconstruction model at true scale.

![Figure 1.13: Sequence Diagram: MR Entrance Alignment and Spatial Anchoring](figures/diagrams/figure_3_13.png)

#### Process 4: Multimodal AI Heritage Guide (Speech & LLM Synthesis)
`SpeechToTextHandler` streams WAV audio to Groq Whisper, populates the input field, and allows the visitor to press Send. `GeminiChatUI` dispatches the prompt with localized Yarang historical system instructions, handling retry fallback on HTTP rate limits (429) or service outages (5xx).

![Figure 1.14: Sequence Diagram: Multimodal AI Guide Speech-to-Text and LLM Query](figures/diagrams/figure_3_14.png)

### 1.1.6 User Interface & Spatial Interaction Design

Spatial UI in VR/MR must eliminate vergence-accommodation conflict, maintain readable text sizing, and prevent neck strain. World-space canvases are positioned at an optimal focal distance of 1.2 to 2.0 meters, scaled to 0.001f, and tilted toward eye level. TextMesh Pro delivers distance-field bilingual rendering in TH Sarabun New (Thai) and sans-serif Latin fonts.

| Mode | Primary Hand | Input Element | Action / Trigger | Visual Feedback & UI State |
|---|---|---|---|---|
| **Main Menu** | Right / Left | Index Trigger | Raycast Point & Click | Button highlight, audio click, scene load overlay |
| **VR Tour** | Right Hand | Index Trigger | Select Navigation Arrow | Quadratic camera translation, arrow pulse |
| **VR Tour** | Left Hand | Button Y | Toggle Reconstruction | Spawns 3D model, displays historical description panel |
| **VR Tour** | Right Hand | Thumbstick | Rotate Reconstruction | Model turns smoothly on vertical axis; click resets |
| **VR Museum** | Right / Left | Grip Button | One-Handed 6-DOF Grab | Halts auto-spin; model follows hand translation/rotation |
| **VR Museum** | Right Hand | Buttons A / B | Scale Model (Hold) | Holding A reduces scale; holding B enlarges model |
| **VR Museum** | Left Hand | Button Y | Toggle Alternate Model | Swaps weathered photogrammetry model with restored form |
| **VR Museum** | Left Hand | Button X / Close | Exit Inspection | Restores original lighting, pedestal, and locomotion |
| **On-Site MR** | Right Hand | Grip + Stick | Position Entrance Outline | Moves outline in 3D space; stick adjusts reach & yaw |
| **On-Site MR** | Right Hand | Button A | Confirm Alignment | Generates session spatial anchor; locks site matrix |
| **On-Site MR** | Right Hand | Button A / B | Toggle Model / Site Info | A toggles monument visibility; B opens evidence panel |
| **On-Site MR** | Left Hand | Hold Button X (2s) | Realign Site | Clears spatial anchor; returns to alignment mode |
| **Shared Submenu**| Left Hand | Menu Button | Toggle Tablet / AI Guide | Opens world-space tablet for AI chat and guide |

### 1.1.7 Data Structure & Content Asset Architecture

LangkaLens employs an asset-driven, modular data architecture leveraging Unity ScriptableObjects and Addressable AssetBundles. Separating archaeological content definitions from runtime engine code ensures that additional viewpoints, reconstructed stupas, or newly excavated artifacts can be integrated into the application without altering compiled C# scripts.

| Data Entity | Field / Attribute | Type | Functional Role & Constraint |
|---|---|---|---|
| **TourArea** | areaName | string | Localized human-readable area title (e.g., 'Ban Jalae 3'). |
| **TourArea** | startingNode | TourNode | Root viewpoint entry node loaded upon area selection. |
| **TourArea** | reconstruction | TourReconstructionDef | Reference to the 3D reconstructed monument definition. |
| **TourNode** | areaNumber | int | Archaeological area index matching Yarang site registry. |
| **TourNode** | position | NodePosition (Enum) | Categorical placement: Front, Back, Left, Right, Center. |
| **TourNode** | photoReference | AssetReferenceTexture2D | Addressables key pointing to the 8K equirectangular panorama. |
| **TourNode** | previousPath / nextPath | NodeConnection[] | Bidirectional graph links defining navigable adjacent nodes. |
| **NodeConnection** | targetNode | TourNode | Destination node loaded upon indicator interaction. |
| **NodeConnection** | arrowPosition / Rotation | Vector3 / Quaternion | Spatial transform offset for rendering directional arrow in 3D. |
| **TourReconstructionDef** | inspectionPrefab | AssetReferenceGameObject| Addressables prefab holding 3D stupa mesh and collider. |
| **TourReconstructionDef** | viewingDistance / scale | float | Optimal offset and scale factor applied during inspection. |
| **ArtifactData** | localizedTitle / Desc | LocalizedString | Unity Localization Table keys for bilingual Thai/English text. |
| **ArtifactData** | primaryModel / altModel | GameObject | Primary weathered model and optional reconstructed variant. |
| **MRMeshPlayableArea** | footprintMode | FootprintMode (Enum) | Set to Unrestricted (mode 3) in current saved scene configuration. |
| **MRMeshPlayableArea** | CanPlay | computed bool | Runtime gate verifying alignment, tracking, and passthrough readiness. |

---

## 1.2 System Development

System Development documents the technical construction, toolchain infrastructure, codebase organization, subsystem engineering implementations, and mobile VR optimization pipelines employed to realize LangkaLens.

### 1.2.1 Development Environment & Toolchain Infrastructure

| Category | Hardware / Software Component | Specification / Version | Engineering Purpose |
|---|---|---|---|
| **Workstation** | HP ZBook Fury 15.6 G8 PC | Intel i7-11800H, 64 GB RAM | Primary engine compilation, shader cooking, asset packaging. |
| **GPU** | NVIDIA RTX A4000 Laptop GPU | 16 GB GDDR6 VRAM, Studio Driver | High-fidelity VR scene rendering and texture baking. |
| **Target HMD** | Meta Quest 3 & Quest 3S | Snapdragon XR2 Gen 2, Horizon OS | Primary deployment target for standalone 6-DOF VR and MR. |
| **Tethering** | High-Speed Link Cables | 2m USB4 (10 Gb/s) & 5m USB-C 3.0 | Rapid playtesting via Meta Quest Link and live ADB debugging. |
| **Engine** | Unity Editor | 6000.3.16f1 (Unity 6 LTS) | Core game engine, scene hierarchy, and build orchestrator. |
| **Pipeline** | Universal Render Pipeline | URP 17.3.0 (Forward+ renderer) | Mobile-optimized single-pass stereo rendering. |
| **XR SDK** | Meta XR All-In-One SDK | v203.0.2 (Core 203.0.0, ISDK 203.0.0) | Camera rig tracking, passthrough buffers, spatial anchors. |
| **Asset Loader**| Unity Addressables | v2.9.1 | Asynchronous texture loading and GPU memory lifecycle management. |
| **Localization**| Unity Localization | v1.5.12 | Dynamic runtime bilingual Thai (th) and English (en) switching. |
| **Compilation** | Android NDK & OpenJDK | r23c / OpenJDK 17 (IL2CPP, ARM64) | Compiles high-performance C++ binaries for Horizon OS (Android 12L). |
| **Desktop Sim** | Meta XR Simulator | v203.0 (Integrated into Editor) | Simulates 6-DOF head motion, passthrough, and controllers on PC. |
| **Device Hub** | Meta Quest Developer Hub (MQDH)| v6.5.2 | APK sideloading, real-time performance metrics, and ADB log streaming. |

### 1.2.2 Project Directory Structure & Software Organization

The project repository follows a strict modular structure under `Assets/_LangkaLens/`:
- `_Scenes/`: The four enabled runtime scenes (`Main_Menu_Scene.unity`, `VR_Tour_Scene.unity`, `VR_Museum_Scene.unity`, `MR_Scene.unity`).
- `Main_menu/`: `MainMenuManager.cs`, audio feedback clips, and `RigResetter.cs` for camera calibration.
- `Tour/`: `TourManager.cs`, `TourNode.cs`, `TourArea.cs`, `TourReconstructionDefinition.cs`, and the Addressables catalog for 360 photos.
- `Museum/`: `ArtifactData.cs`, `OneGrabFreeRotateTransformer.cs`, exhibit pedestal prefabs, and 3D photogrammetric meshes.
- `MR/`: `MRMeshReferenceAlignment.cs`, `MRMeshPlayableArea.cs`, `MRModelSpawner.cs`, and `MRSiteInformationPanel.cs`.
- `Mixed/`: Cross-scene shared modules (`InspectionManager.cs`, `InspectionEnvironmentDimmer.cs`, `GeminiChatUI.cs`, `SpeechToTextHandler.cs`, `BilingualVirtualKeyboard.cs`).
- `Misc/`: Unity Localization StringTable collections, Editor validation utilities, and `UniversalOneHandedLocomotion.cs`.

### 1.2.3 Subsystem Engineering & Implementation Details

#### Subsystem 1: Main Menu & Localization Engine
`MainMenuManager.cs` manages locale initialization, session language selection, and scene routing. Upon scene load, `RigResetter.cs` forces the OVRCameraRig tracking space to zero and recenters the camera to eye level. Unity Localization StringTable collections dynamically broadcast locale changes to all active TextMesh Pro labels.

![Figure 1.15: Unity Editor Authoring: Main Menu Scene Hierarchy](Screenshots/Development/unity-main-menu-editor.png)
![Figure 1.16: Meta XR Simulator Playtest: Main Menu Hub Interface](Screenshots/Playtesting/main-menu-simulator.png)

#### Subsystem 2: VR 360-Degree Panoramic Tour Subsystem
The VR Tour subsystem renders high-resolution equirectangular spherical panoramas captured at Ancient Yarang City. `TourManager.cs` parses `TourNode` ScriptableObjects to construct an in-memory navigation graph. When the user selects a directional arrow, `TourManager` initiates an asynchronous transition: locks input, loads the new 8K `Texture2D` asynchronously via Addressables, interpolates camera container position via quadratic ease-in, releases the prior texture handle, and populates new indicators. `TourReconstructionPresenter.cs` enables in-situ 3D inspection of ancient stupas upon pressing button Y.

![Figure 1.17: Unity Editor Authoring: VR Tour Viewpoint Graph](Screenshots/Development/unity-tour-editor.png)
![Figure 1.18: Meta XR Simulator Playtest: 360 Panoramic Viewpoint Traversal](Screenshots/Playtesting/tour-panorama-simulator.png)

#### Subsystem 3: VR Museum & 3D Artifact Inspection Subsystem
The Virtual Museum displays photogrammetric 3D scans of excavated Yarang artifacts. Each exhibit pedestal features an `ArtifactData` component linking localized titles, descriptions, and dual-mesh references. `InspectionManager.cs` triggers inspection mode, locking locomotion, while `InspectionEnvironmentDimmer.cs` executes an unscaled exposure fade on surrounding gallery renderers, darkening the room while spotlighting the exhibit. Users manipulate the model using `OneGrabFreeRotateTransformer.cs` and can toggle between weathered photogrammetry and restored forms via button Y.

![Figure 1.19: Unity Editor Authoring: VR Museum Gallery Hierarchy](Screenshots/Development/unity-museum-editor.png)
![Figure 1.20: Meta XR Simulator Playtest: 6-DOF Artifact Examination with Dimmer](Screenshots/Playtesting/museum-inspection-simulator.png)

#### Subsystem 4: On-Site Mixed Reality Alignment & Reconstruction Subsystem
The Mixed Reality mode uses full-color stereoscopic video passthrough for the real-world background. `MRMeshReferenceAlignment.cs` enables curators to match a virtual bounding reference box with an on-site physical benchmark using the right grip and thumbstick. Confirming with button A creates a persistent hardware `OVRSpatialAnchor`. `MRMeshPlayableArea.cs` evaluates tracking health and gates content visibility through its `CanPlay` property. `MRModelSpawner.cs` instantiates the 1:1 scale reconstructed monument, allowing visitors to walk around the ancient stupa while observing physical brick foundations through passthrough.

![Figure 1.21: Unity Editor Authoring: On-Site MR Entrance Alignment](Screenshots/Development/unity-mr-alignment-editor.png)

#### Subsystem 5: Multimodal AI Heritage Guide & Speech Subsystem
The intelligent guide is accessible across all scenes via a wrist tablet or submenu. `GeminiChatUI.cs` manages asynchronous HTTP requests to Google Gemini API (`gemini-1.5-flash` / `gemini-2.0-flash`). The request injects curated archaeological prompt instructions. The client implements exponential backoff and a single-attempt fallback model for HTTP 404/408/429/5xx errors. `SpeechToTextHandler.cs` captures audio via Unity's Microphone API, encodes 16-bit PCM WAV chunks, and streams them to Groq Whisper (`whisper-large-v3`). The returned transcript populates an editable input field. A virtual bilingual keyboard (`BilingualVirtualKeyboard.cs`) supports direct typed entry in English and Thai.

![Figure 1.22: Meta XR Simulator Playtest: Shared Multimodal AI Guide Interface](Screenshots/Playtesting/ai-guide-simulator.png)

### 1.2.4 Build Pipeline, Mobile XR Optimization & Asset Packaging

- **URP Forward+ Rendering & Single-Pass Stereo:** Forward+ rendering eliminates unnecessary multi-pass lighting overhead. Single-Pass Instanced Stereo rendering submits draw calls for both eyes in a single GPU pass, halving CPU driver overhead.
- **ASTC Texture Compression:** All 8K panoramic equirectangular textures and 4K photogrammetric PBR material maps are encoded using ASTC 6x6 block compression, significantly reducing GPU memory bandwidth and texture cache misses on the Adreno GPU.
- **Addressables Asset Bundling & Memory Management:** Panoramic textures and reconstruction prefabs are compiled into discrete Addressable AssetBundles. Unused bundles are deallocated immediately upon scene exit or viewpoint transition via `Addressables.Release()`, keeping the mobile heap footprint consistently below 2.5 GB.
- **Android XR Manifest & Permissions:** `AndroidManifest.xml` configures `android.hardware.vr.headtracking` (required), `RECORD_AUDIO` (for voice input), and `com.oculus.feature.PASSTHROUGH` (required for MR mode). Target SDK is Android 12L (API level 32) with 64-bit ARM64 architecture.
- **Automated Verification via Meta XR Simulator:** The desktop workflow leverages Meta XR Simulator v203.0 and MQDH v6.5.2 to validate 6-DOF tracking, hand controller gestures, language switching, and UI layouts on PC before pushing release APKs to physical headsets.

---
