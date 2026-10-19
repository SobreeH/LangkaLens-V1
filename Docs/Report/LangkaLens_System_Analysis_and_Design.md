# 1.1 System Analysis and Design: LangkaLens Cultural Heritage Platform
**Academic Context:** Senior Project Technical Specification (Part 1.1)  
**Author:** Wansobree Hajihama (Student ID: 6620610023)  
**Project Advisor:** Assistant Professor Dr. Zamree Che-aron (ผศ.ดร.ซัมรี เจ๊ะอารน)  
**Academic Degree:** Bachelor of Science in Computer and Informatics for Management (CIM)  
**Institution:** Faculty of Communication Sciences, Prince of Songkla University, Pattani Campus  
**Target Hardware:** Meta Quest 3 & Meta Quest 3S (Horizon OS / Android 12L) | **Engine:** Unity 6000.3.16f1 URP  

---

# 1.1 System Analysis and Design

LangkaLens is a standalone spatial computing platform developed for the Meta Quest 3 and Meta Quest 3S head-mounted displays (HMDs), dedicated to preserving, interpreting, and communicating the archaeological heritage of Ancient Yarang City (encompassing Ban Jalae and Ban Wat archaeological complexes in Pattani, Southern Thailand). Archaeological remains in Yarang typically consist of surviving brick foundation contours, moats, and baselines without complete standing upper structures. LangkaLens addresses this visualization challenge by synthesizing three interconnected experiential modalities within a unified application:
1. **VR 360-Degree Panoramic Tour:** Covering Ban Jalae 2, Ban Jalae 3, Ban Jalae 8, and Ban Wat 9.
2. **Interactive VR Museum:** 6-degree-of-freedom (6-DOF) examination, manipulation, dynamic lighting dimming, and dual-mesh alternate LOD comparisons of digitised artifacts.
3. **On-Site Mixed Reality (MR) Reconstruction:** 1:1 scale interpreted architectural reconstructions overlaid directly onto physical ruins using high-fidelity color video passthrough and hardware-tracked spatial session anchors.

System Analysis and Design establishes the foundational requirements, structural paradigms, behavioral models, and data schemas that govern the LangkaLens spatial software architecture.

> **Architectural Decoupling Principle:** LangkaLens strictly decouples core presentation scenes from external cloud intelligence. All 3D models, panoramic spheres, localization assets, and spatial alignment parameters execute client-side on the Meta Quest headset. Cloud AI services (Gemini chat and Groq transcription) operate asynchronously with robust timeout, cancellation, and local fallback routines to ensure application stability under unpredictable field network conditions.

---

## 1.1.1 Requirements Engineering & Analysis

### Functional Requirements (FR)

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

### Non-Functional Requirements (NFR)

| ID | Category | Target Specification & Metric | Architectural Strategy |
|---|---|---|---|
| **NFR-01** | Performance & Frame Rate | Consistent 72 FPS / 90 FPS; frame time <= 13.8 ms | URP Forward+ rendering, ASTC 6x6 compression, single-pass instanced stereo rendering. |
| **NFR-02** | Memory Management | Peak heap footprint < 2.5 GB; zero VRAM leaks | Unity Addressables async loading and handle release; texture cache garbage collection. |
| **NFR-03** | Spatial Ergonomics | Comfort FOV (30-60 deg); 1.2m-2.0m UI canvas distance | World-space canvas anchoring; vergence-accommodation conflict mitigation. |
| **NFR-04** | Cybersickness Mitigation | Zero involuntary camera motion; snap turning support | Instantaneous or quadratic ease-in transitions; universal one-handed locomotion. |
| **NFR-05** | Fault Tolerance & Resilience | Offline recovery; automatic fallback API models | Single-attempt retry on HTTP 404/408/429/5xx; non-blocking coroutine timeouts. |
| **NFR-06** | Bilingual Parity | 100% lexical symmetry between Thai and English | Strict parity across StringTable collections for museum, tour, and UI labels. |

---

## 1.1.2 Conceptual & High-Level System Architecture

The high-level architecture of LangkaLens follows a 4-tier layered model:
1. **Tier 1 (Presentation & UX):** World-space Canvas hierarchies, TextMesh Pro distance-field rendering, Meta XR Passthrough, and audio cues.
2. **Tier 2 (Application & Domain Logic):** MonoBehaviour controllers orchestrating state machines: MainMenuManager, TourManager, InspectionManager, InspectionEnvironmentDimmer, MRMeshReferenceAlignment, and GeminiChatUI.
3. **Tier 3 (Hardware Abstraction & XR Engine):** Unity 6 Engine and Meta XR All-In-One SDK (v203.0.2), handling SLAM tracking, optical passthrough buffers, spatial anchor hardware, and bimanual Touch Plus controller events.
4. **Tier 4 (Data & External Cloud Services):** Serialized ScriptableObjects and Addressable AssetBundles paired with external RESTful endpoints for Google Gemini and Groq Whisper.

![Figure 1.1: High-Level System Architecture and External Services Topology](figures/diagrams/figure_3_1.png)

---

## 1.1.3 Actor Profiles and Functional Use Cases

System actors represent entities interacting with LangkaLens:
- **Cultural Heritage Visitor / Learner:** Primary human actor (general public, students, tourists) engaging in tour navigation, museum inspection, and conversational inquiry.
- **Field Researcher / Site Curator:** Secondary human actor responsible for calibrating on-site physical-to-virtual alignment.
- **External AI Microservices:** Automated cloud endpoints (Google Gemini and Groq Whisper).

![Figure 1.2: System Functions and Use Case Diagram for Visitors and Curators](figures/diagrams/figure_3_2.png)

---

## 1.1.4 System Workflow & Behavioral Design (Activity Flowcharts)

### Application Lifecycle & Scene Orchestration
The application follows a centralized hub-and-spoke execution pattern. Launching the application initializes `Main_Menu_Scene`. Visitors can navigate into any of the three experiential scenes. Return requests prompt confirmation before restoring the main menu and releasing mode-specific assets.

![Figure 1.3: Application Overview and Scene Navigation Flowchart](figures/diagrams/figure_3_3.png)

### Main Menu & Session Language Confirmation Workflow
The menu workflow enforces session-scoped language persistence. When launched, the system checks whether language has been confirmed during the current application session. If unconfirmed, the bilingual selection dialog is displayed. Subsequent returns to the main menu skip this prompt.

![Figure 1.4: Main Menu Language Selection and Scene Loading Flowchart](figures/diagrams/figure_3_4.png)

### VR Tour Viewpoint Traversal & Map Navigation Workflow
Tour navigation operates as an event-driven state machine. Selecting a linked arrow disables user locomotion, loads the destination equirectangular texture asynchronously, animates camera container movement, disposes of the prior texture handle, and activates new navigation indicators upon successful arrival.

![Figure 1.5: VR 360 Panoramic Tour Navigation Flowchart](figures/diagrams/figure_3_5.png)

### VR Tour Restored-Model Inspection Workflow
Contextual 3D reconstruction inspection is available at key archaeological viewpoints. Pressing left controller Y loads the reconstruction display prefab via Addressables. If the user moves away or cancels, asynchronous loads are aborted and discarded.

![Figure 1.6: VR Tour Reconstruction Inspection and Memory Cleanup Flowchart](figures/diagrams/figure_3_6.png)

### VR Museum Artifact Exploration & Dimmer Lifecycle Workflow
When an artifact is selected, player locomotion is locked, pre-inspection camera poses are cached, and `InspectionEnvironmentDimmer` initiates an exposure fade on the surrounding gallery while keeping the artifact illuminated. Closing inspection restores gallery lighting and returns the artifact to its pedestal.

![Figure 1.7: VR Museum Artifact Exploration and Environment Dimmer Flowchart](figures/diagrams/figure_3_7.png)

### Mixed Reality Entrance Alignment & Session Anchor Workflow
The curator aligns a virtual entrance outline to physical signage using the right controller grip and thumbstick. Confirming with button A initiates hardware spatial anchor creation with a 15-second timeout window. Upon localization, the relative transformation matrix is locked for the session.

![Figure 1.8: On-Site Mixed Reality Entrance Alignment and Anchor Flowchart](figures/diagrams/figure_3_8.png)

### MR Model Display Gating & Tracking Recovery Workflow
In the saved unrestricted configuration, model display requires alignment confirmation, active head tracking, and passthrough readiness. If tracking quality drops or passthrough is interrupted, the reconstructed building automatically conceals itself until SLAM tracking re-establishes stability.

![Figure 1.9: MR Model Display Gating and Tracking Recovery Flowchart](figures/diagrams/figure_3_9.png)

### Shared Multimodal AI Heritage Guide Workflow
Users may type questions or dictate speech. Audio recordings are transmitted to Groq Whisper for transcription and returned to an editable text field. This allows users to review and amend the transcription before submitting the prompt to Google Gemini.

![Figure 1.10: Shared Multimodal AI Heritage Guide Workflow](figures/diagrams/figure_3_10.png)

---

## 1.1.5 Dynamic Process & Interaction Design (Sequence Diagrams)

### Process 1: Tour Viewpoint Transition & Asynchronous Asset Loading
![Figure 1.11: Sequence Diagram: Asynchronous Tour Viewpoint Transition](figures/diagrams/figure_3_11.png)

### Process 2: Museum Artifact Inspection & Environment Dimming
![Figure 1.12: Sequence Diagram: Museum Artifact Inspection and Dimmer Lifecycle](figures/diagrams/figure_3_12.png)

### Process 3: MR Spatial Alignment & Session Anchoring
![Figure 1.13: Sequence Diagram: MR Entrance Alignment and Spatial Anchoring](figures/diagrams/figure_3_13.png)

### Process 4: Multimodal AI Heritage Guide (Speech & LLM Synthesis)
![Figure 1.14: Sequence Diagram: Multimodal AI Guide Speech-to-Text and LLM Query](figures/diagrams/figure_3_14.png)

---

## 1.1.6 User Interface & Spatial Interaction Design

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

---

## 1.1.7 Data Structure & Content Asset Architecture

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
