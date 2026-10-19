# LangkaLens: Project Objectives, System Scope, Master Spatial Storyboard, and Architectural Diagrams
**Academic Context:** Senior Project Technical Specification & Print Deliverable  
**Author:** Wansobree Hajihama (Student ID: 6620610023)  
**Project Advisor:** Assistant Professor Dr. Zamree Che-aron (ผศ.ดร.ซัมรี เจ๊ะอารน)  
**Course / Major:** Computer and Informatics for Management (CIM) / สาขาวิชาคอมพิวเตอร์และวิทยาการสารสนเทศเพื่อการจัดการ  
**Faculty / Institution:** Faculty of Communication Science, Prince of Songkla University, Pattani Campus  
**Target Hardware:** Meta Quest 3 & Meta Quest 3S (Horizon OS / Android 12L) | **Engine:** Unity 6000.3.16f1 URP 17.3.0  
**Reference Master Report:** `Docs/Report/LangkaLens_Senior_Project_Report.docx` (Approved Thesis Edition, Updated October 2026)  
**Visual Asset Repository:** 512 Multi-Format Assets Across 15 Domains (`Docs/IMAGE_CATALOG.md`)  

---

## Executive Summary & Seminar Progression Context
This specification document is compiled specifically for print and academic review. It consolidates the definitive Project Objectives, System Scope, Master Spatial Storyboard generated via Google Stitch, and the complete 14-diagram architectural suite (Figures 3.1 through 3.15) for LangkaLens.

These materials represent the latest, updated versions of the concepts originally presented during the preliminary seminar course last semester. Key updates include:
1. **Master Spatial Storyboard Integration:** Integrating the Google Stitch Master Spatial Storyboard into Section 3.1 as Figure 3.1, establishing an end-to-end curatorial user journey from onboarding to multimodal AI guidance.
2. **Synchronized Figure Numbering:** Shifting and renumbering the High-Level System Architecture and behavioral diagrams to Figures 3.2 through 3.15, aligning 1-to-1 with Chapter 3 of the thesis report.
3. **Dynamic Landscape Sections & Diagram Enlargement:** Adapting wide architecture and sequence diagrams to landscape page layouts, enlarging diagrams by up to 90% for clear, crisp legibility.
4. **Decoupled Cloud Architecture:** Transitioning from theoretical administrative portals to a focused, standalone mobile VR/MR architecture. Cloud AI (Gemini and Groq) is strictly decoupled from the core presentation loop.
5. **Session-Scoped Language Persistence:** Adopting session-scoped language persistence (confirming once per runtime session).
6. **Unrestricted Mixed Reality with Hardware Anchors:** Upgrading Mixed Reality to an unrestricted boundary matching open-air historical parks, backed by persistent hardware spatial session anchors (`OVRSpatialAnchor`).
7. **Unscaled Museum Dimmer:** Integrating an unscaled environment exposure dimmer (0.18 multiplier over 0.35s) during artifact inspection to maximize focus.
8. **Two-Stage Voice Transcription:** Implementing an asynchronous speech-to-text pipeline (Groq Whisper) with an editable input field before Google Gemini AI query dispatch.
9. **Enriched Visual Asset Integration:** Incorporating the complete 15-domain visual repository (512 assets, 1.72 GB) featuring 11 curated archaeological relics and 4 full-scale architectural reconstructions.

---

# Part 1: Project Objectives (วัตถุประสงค์ของโครงงาน)

LangkaLens addresses a critical preservation and pedagogical challenge: the surviving archaeological remains of Ancient Yarang City (Pattani Province, Southern Thailand) consist primarily of low brick foundation baselines, earthen ramparts, and moats without surviving upper superstructures.

Based on consultation with official Fine Arts Department excavation monographs and technical evaluation in the senior thesis, the six formal project objectives are:

1. **Standalone Mobile XR Application Development:** Develop a standalone Virtual Reality (VR) and Mixed Reality (MR) cultural heritage application targeting the Meta Quest 3 and Meta Quest 3S head-mounted displays, providing an immersive, accessible experience without tethered PC requirements.
2. **360-Degree Panoramic Tour with In-Situ Reconstruction:** Provide an interactive 360-degree photographic tour across four key Yarang archaeological complexes (Ban Jalae 2, Ban Jalae 3, Ban Jalae 8, and Ban Wat 9), supporting intuitive waypoint wayfinding and on-demand in-situ 3D inspection of interpreted ancient stupa superstructures.
3. **6-DOF Virtual Museum & Artifact Exploration:** Allow visitors to freely inspect, rotate, and resize digitised photogrammetric 3D artifacts using natural 6-degree-of-freedom (6-DOF) spatial grabbing, accompanied by an automatic environment exposure dimmer to focus visual attention, and alternate mesh comparisons between weathered and restored archaeological forms.
4. **On-Site Mixed Reality Alignment & Session Anchoring:** Provide an on-site Mixed Reality (MR) experience using high-definition stereoscopic color video passthrough, allowing curators and on-site visitors to align virtual entrance references to physical benchmarks and anchor full-scale 1:1 architectural reconstructions using hardware-tracked Meta spatial session anchors.
5. **Bilingual Parity & Multimodal AI Heritage Guide:** Implement fully symmetrical bilingual Thai (`th`) and English (`en`) interfaces across all scenes via Unity Localization, paired with an integrated multimodal AI heritage guide supporting both typed questions and Groq Whisper speech transcription prior to dispatching prompts to Google Gemini.
6. **Systematic Functional Verification & Simulator Testing:** Rigorously verify the application's stability, navigation transitions, memory deallocation, and interaction flows through structured functional checklists and Meta XR Simulator playtesting.

### Objectives & Engineering Deliverables Traceability Matrix

| ID | Project Objective | Primary Subsystem & Controller | Deliverable & Verification Standard |
|---|---|---|---|
| **OBJ-01** | Standalone VR/MR App | OVRCameraRig / URP Forward+ | Standalone Android APK (Horizon OS), sustaining 72/90 FPS with Single-Pass Stereo. |
| **OBJ-02** | VR Tour & Stupa Overlay | TourManager.cs / Addressables | 4-area tour graph, 8K panoramas, Left Y 3D stupa inspection with async release. |
| **OBJ-03** | VR Museum & Dimmer | InspectionManager.cs / Dimmer | Pedestal gallery, OneGrabFreeRotateTransformer, 0.18 exposure fade, dual-mesh toggle. |
| **OBJ-04** | On-Site MR Alignment | MRMeshReferenceAlignment.cs | Color passthrough, right-controller reference gizmo, OVRSpatialAnchor session lock. |
| **OBJ-05** | Bilingual AI Guide | GeminiChatUI.cs / Groq Whisper | 100% Thai/EN StringTable parity, 16kHz speech transcription, Gemini REST chat with retry. |
| **OBJ-06** | System Verification | Meta XR Simulator / ADB | 14-case functional checklist, zero memory leaks (<2.5GB heap), zero unhandled exceptions. |

---

# Part 2: System Scope (ขอบเขตของโครงงานและระบบ)

### 2.1 Application Lifecycle, Onboarding & Spatial Accessibility Scope
- **Centralized Hub-and-Spoke Lifecycle:** Boot initializes in `Main_Menu_Scene`. Visitors choose Thai or English and route to VR Tour, VR Museum, or MR Mode.
- **Session-Scoped Language Persistence:** Language is recorded for the active session. Returning to the main menu bypasses redundant prompts; restarting the app begins a clean session.
- **Accessibility & Ergonomics:** World-space UI canvases positioned at 1.2m–2.0m optical distance within 30°–60° FOV. Universal one-handed locomotion prevents cybersickness.

### 2.2 VR 360-Degree Panoramic Tour Scope & Historic Monuments
- **Archaeological Monument Coverage:** Ban Jalae 2 (Monument No. 2, stupa sanctuary), Ban Jalae 3 (Monument No. 3, tiered terracotta stupa base), Ban Jalae 8 (Monument No. 8, central mandapa complex), and Ban Wat 9 (Monument No. 9, moated shrine).
- **Wayfinding Navigation Graph:** Bidirectional node traversal driven by ScriptableObjects covering 91 equirectangular 8K photographic nodes.
- **Addressables Memory Lifecycle:** 8K equirectangular textures load asynchronously via Unity Addressables. Prior texture handles are released immediately upon arrival to conserve mobile VRAM.
- **In-Situ 3D Stupa Inspection:** Key viewpoints allow pressing Left Controller Button Y to instantiate a 3D interpreted reconstruction stupa with localized historical metadata.

### 2.3 VR Virtual Museum Scope & 11 Curated Archaeological Relics
- **Curated 11-Relic Roster:** Complete photogrammetric 3D scans: (1) Standing Buddha (ศิลปะทวารวดี), (2) Nandi Sacred Bull (โคนันทิ), (3) Carved Stone Pillar (เสาหินสลัก), (4) Ancient Deity Statue (เทวรูปโบราณ), (5-8) Stupa Model Types 1–4 (สถูปจำลอง แบบที่ 1-4), (9) Surya/Trinity Stele (แผ่นสลักสุริยเทพ/ตรีมูรติ), (10) Kurma Sacred Turtle (เต่ามงคล), and (11) Dharmachakra Wheel (ธรรมจักรศิลา).
- **6-DOF Manipulation:** Powered by Meta Interaction SDK (`OneGrabFreeRotateTransformer`). Grab, rotate 360°, and hold buttons A/B to scale.
- **Unscaled Environment Exposure Dimmer:** Selecting an exhibit dims ambient gallery lighting (0.18 multiplier over 0.35s), isolating the illuminated artifact.
- **Dual-Mesh Comparison:** Supported exhibits allow toggling between the weathered excavation state (`_ORG`) and an archaeologically restored model (`_RES`).

### 2.4 On-Site Mixed Reality Scope
- **Stereoscopic Color Passthrough:** Utilizes Meta Quest 3/3S RGB cameras to blend digital monuments directly into the physical archaeological park.
- **Curator Entrance Alignment:** Curators position a virtual bounding entrance box over physical signs using right grip and thumbstick controls.
- **Session Spatial Anchoring:** Confirming alignment creates a hardware `OVRSpatialAnchor`, locking the 1:1 scale reconstructed monument in physical space.
- **Unrestricted Footprint Mode:** Operates with an open movement volume matching open-air historical parks.
- **Tracking Loss Concealment:** Monument visibility is automatically gated by head tracking stability and passthrough status.

### 2.5 Multimodal AI Heritage Guide Scope
- **Omnipresent Access:** Accessible across all scenes via a wrist tablet / submenu.
- **Dual Input Modes:** Typed entry via virtual keyboard or voice recording via Unity Microphone.
- **Speech-to-Text Pipeline:** 16kHz PCM audio WAV is streamed to Groq Cloud (`whisper-large-v3`) and populates an editable input field.
- **Grounded Gemini Dialogue:** Sends questions to Google Gemini REST API (`gemini-1.5-flash` / `gemini-2.0-flash`) with prompt grounding, exponential backoff, and fallback routing.

### 2.6 Hardware, OS, Toolchain & Visual Asset Repository Scope
- **Workstation:** HP ZBook Fury 15.6 G8 (Intel i7-11800H, 64 GB RAM, NVIDIA RTX A4000 Laptop GPU, Windows 11 Pro 64-bit).
- **Target Headsets:** Meta Quest 3 & Meta Quest 3S running Meta Horizon OS (Android 12L / API 32, 64-bit ARM64).
- **Engine Stack:** Unity 6000.3.16f1 LTS, URP 17.3.0, Meta XR All-In-One SDK v203.0.2, Addressables 2.9.1, Unity Localization 1.5.12.
- **Visual Asset Inventory:** 512 multi-format assets across 15 domains (1.72 GB total volume), cataloged in `Docs/IMAGE_CATALOG.md`.

### Formal Requirements Specifications Matrix

| Req ID | Requirement Classification | Functional Scope & Implementation | Pass / Acceptance Standard |
|---|---|---|---|
| **FR-01** | Bilingual Locale Switching | MainMenuManager; Unity Localization 1.5.12 | Session persistence; dynamic label updates; zero untranslated text. |
| **FR-02** | 360° Viewpoint Navigation | TourManager; TourNode; Addressables 2.9.1 | Smooth quadratic translation; instant deallocation of prior textures. |
| **FR-03** | VR Reconstruction Inspection | TourReconstructionPresenter; InspectionManager | Left Y spawns 3D stupa; 6-DOF rotation and scaling; localized info panel. |
| **FR-04** | VR Museum Artifact 6-DOF | ArtifactData; OneGrabFreeRotate; Dimmer | Pedestal exhibit grab; 0.18 environment fade; dual-mesh toggle support. |
| **FR-05** | MR Entrance Alignment | MRMeshReferenceAlignment; OVRSpatialAnchor | Curator gizmo alignment; hardware anchor generation within 15 seconds. |
| **FR-06** | MR Display Gating | MRMeshPlayableArea; Passthrough Buffers | Automatic concealment upon tracking degradation; unrestricted park volume. |
| **FR-07** | Multimodal AI Heritage Guide | GeminiChatUI; Google Gemini REST API | Context-grounded prompt injection; automatic fallback on HTTP 429/5xx."],
| **FR-08** | Voice Speech-to-Text | SpeechToTextHandler; Groq Whisper Cloud | 16kHz WAV streaming; editable text field population before prompt dispatch. |
| **NFR-01** | Performance & Frame Rate | URP Forward+; Single-Pass Instanced Stereo | Rock-solid 72/90 FPS; frame time <= 13.8 ms; draw calls <= 120 per eye. |
| **NFR-02** | Memory Footprint | Addressables.Release(); ASTC 6x6 Textures | Peak heap allocation < 2.5 GB; zero VRAM leaks during scene traversal. |
| **NFR-03** | Spatial Ergonomics | World-Space Canvas; 1.2m-2.0m Distance | Eliminates vergence-accommodation conflict; comfort FOV between 30°-60°. |
| **NFR-04** | Cybersickness Mitigation | Universal One-Handed Locomotion System | Zero involuntary camera motion; instantaneous snap-turning options. |
| **NFR-05** | Network Fault Resilience | UnityWebRequest Coroutines; Retry Handlers | Graceful non-blocking error display on offline status or API timeouts. |
| **NFR-06** | Bilingual Symmetry | Parallel StringTable Collections (Thai / EN) | 100% lexical symmetry across UI buttons, descriptions, and guide prompts. |

---

# Part 3: Updated System Storyboards and Architectural Diagrams (สตอรี่บอร์ดและแผนภาพระบบฉบับปรับปรุงล่าสุด)

### 3.1 Master Spatial Storyboard (สตอรี่บอร์ดหลักจำลองการโต้ตอบเชิงพื้นที่)
Figure 3.1 (presented as a dedicated full-page spatial architecture plate) illustrates the comprehensive LangkaLens master spatial storyboard and end-to-end interaction topology across the seven primary user journey phases on Meta Quest 3 and Quest 3S, generated via Google Stitch spatial design specification.

#### Figure 3.1: Master Spatial Storyboard *(Full-Page Architectural Plate)*
![Figure 3.1: Master Spatial Storyboard (Full-Page Plate)](figures/diagrams/storyboard_01_master_journey.png)

*Figure 3.1: Complete 7-Scene Interactive User Journey and Architectural Topology across Meta Quest 3 and Quest 3S (Generated via Google Stitch Spatial Computing Design System, grounded in 7th–11th Century Dvaravati Archaeological Data). Visualizes end-to-end user flows, world-space UI cards, and Touch Plus controller field mapping.*

#### 3.1.1 Master Storyboard Interaction & Subsystem Specification Matrix

| # | Scene / Experience | Visual Environment | Spatial UI & Components | User Triggers & Controls | Audio & Haptic Feedback | C# Controller Link |
|---|---|---|---|---|---|---|
| **01** | Language Portal | Terracotta void with dust motes | Curved modal (800x520px) with Thai/EN cards | Raycast point & trigger click | Low hum; crisp tactile haptic pulse | MainMenuManager.cs |
| **02** | Main Menu Hub | 3D sunset panorama over ruins | 4 Glassmorphic Portal Cards | Raycast hover & trigger pull | Evening wind; gentle haptic buzz | MainMenuManager.cs |
| **03** | VR 360 Tour | 8K photosphere of Yarang ruins | 3D directional chevrons + Mini-Map | Aim ray at chevron & click trigger | Bird ambience; forward warp sound | TourManager.cs |
| **04** | 3D Stupa Overlay | Photosphere with 3D stupa | Reconstructed stupa + historical card | Press Left Controller Button Y | Sacred temple gong chime | TourReconstructionPresenter.cs |
| **05** | VR Museum | Gallery with spotlight pedestals | Pedestals with 3D relics; dimmer fade | Grip to grab; stick/AB to scale; Y alt | Soft footsteps; haptic snap on grab | InspectionManager.cs |
| **06** | On-Site MR | Real park via Color Passthrough | 1:1 Stupa + Curator reference gizmo | Right Grip+Stick align; Button A anchor | Spatial reverb; tactile anchor lock | MRMeshReferenceAlignment.cs |
| **07** | Multimodal AI Guide | Floating glassmorphic tablet | Tablet window with chat and mic bar | Dictate voice or type; click Send | Digital chime; typewriter audio stream | GeminiChatUI.cs |

### 3.2 High-Level System Architecture Diagram *(Landscape Orientation)*
![Figure 3.2: Updated High-Level System Architecture and External Services Topology](figures/diagrams/figure_3_1.png)

### 3.3 System Actor Profiles & Use Case Diagram
![Figure 3.3: Updated System Functions and Use Case Diagram for Visitors and Curators](figures/diagrams/figure_3_2.png)

### 3.4 System Workflow & Behavioral Activity Flowcharts
- **Figure 3.4:** Application Overview and Scene Navigation Flowchart  
  ![Figure 3.4](figures/diagrams/figure_3_3.png)
- **Figure 3.5:** Main Menu Language Selection and Scene Loading Flowchart  
  ![Figure 3.5](figures/diagrams/figure_3_4.png)
- **Figure 3.6 & 3.7:** VR 360 Panoramic Tour Navigation & Tour Reconstruction Inspection *(Landscape Side-by-Side)*  
  ![Figure 3.6](figures/diagrams/figure_3_5.png)  
  ![Figure 3.7](figures/diagrams/figure_3_6.png)
- **Figure 3.8:** VR Museum Artifact Exploration and Environment Dimmer Flowchart  
  ![Figure 3.8](figures/diagrams/figure_3_7.png)
- **Figure 3.9:** On-Site Mixed Reality Entrance Alignment and Session Anchor Flowchart  
  ![Figure 3.9](figures/diagrams/figure_3_8.png)
- **Figure 3.10:** MR Model Display Gating and Tracking Recovery Flowchart  
  ![Figure 3.10](figures/diagrams/figure_3_9.png)
- **Figure 3.11:** Shared Multimodal AI Heritage Guide Workflow Flowchart  
  ![Figure 3.11](figures/diagrams/figure_3_10.png)

### 3.5 Process Sequence Diagrams *(Landscape Orientation)*
- **Figure 3.12:** Sequence Diagram: Asynchronous Tour Viewpoint Transition and Memory Cleanup  
  ![Figure 3.12](figures/diagrams/figure_3_11.png)
- **Figure 3.13:** Sequence Diagram: Museum Artifact Inspection and Dimmer Lifecycle  
  ![Figure 3.13](figures/diagrams/figure_3_12.png)
- **Figure 3.14:** Sequence Diagram: MR Entrance Alignment and Spatial Anchoring  
  ![Figure 3.14](figures/diagrams/figure_3_13.png)
- **Figure 3.15:** Sequence Diagram: Multimodal AI Guide Speech Transcription and Gemini LLM Query  
  ![Figure 3.15](figures/diagrams/figure_3_14.png)

### 3.6 Step-by-Step Spatial Interface Walkthrough & Visual Plates

#### Storyboard Frame 1: Language Onboarding Portal
![Storyboard Frame 1](../User_Guide_Pictures/01_GettingStarted_PointAndSelect.jpg)

#### Storyboard Frame 2: Spatial Heritage Hub (Main Menu Navigation)
![Storyboard Frame 2](../User_Guide_Pictures/02_GettingStarted_ChooseExperience.jpg)

#### Storyboard Frame 3: VR 360-Degree Panoramic Tour & Wayfinding
![Storyboard Frame 3](../User_Guide_Pictures/12_VRTour_AimAtNavigationArrow.jpg)

#### 3D Architectural Reconstructions Plate (Four Primary Monuments)
![3D Interpreted Monument Reconstructions](figures/reconstructions_grid_4monuments.png)

#### Storyboard Frame 4: In-Situ 3D Stupa Architectural Reconstruction Overlay
![Storyboard Frame 4](../User_Guide_Pictures/15_VRTour_EnterReconstructionInspection.jpg)

#### Storyboard Frame 5: VR Museum 6-DOF Artifact Examination with Dimmer & Dual-Mesh Comparison Plate
![Storyboard Frame 5 Artifact Comparison Plate](../Images/02_Artifact_Comparisons/Buddha_Comparison.png)

#### Storyboard Frame 6: On-Site Mixed Reality Passthrough & Spatial Alignment
![Storyboard Frame 6](../User_Guide_Pictures/07_MR_ReleaseGripAndConfirm.jpg)

#### Storyboard Frame 7: Independent Multimodal AI Heritage Assistant
![Storyboard Frame 7](../User_Guide_Pictures/27_AIGuide_TypeAndSendQuestion.jpg)

### 3.7 Spatial Controller Input Mapping Reference Table & Curatorial Field Guide

| Mode | Hand | Input Element | Physical Action | Operational Function & Visual Feedback |
|---|---|---|---|---|
| **Universal / Menu** | Right / Left | Index Trigger | Raycast Point & Pull | Selects menu cards, language toggles, and UI buttons. Audio click + highlight. |
| **Universal / Submenu** | Left Hand | Menu Button | Single Click | Toggles the omnipresent wrist tablet containing the AI Heritage Guide. |
| **VR Tour** | Right Hand | Index Trigger | Point & Click Chevron | Triggers smooth quadratic camera translation to the next panoramic viewpoint. |
| **VR Tour** | Left Hand | Button Y | Single Click | Spawns 3D stupa reconstruction and localized historical information panel. |
| **VR Tour** | Right Hand | Thumbstick | Horizontal Deflection | Smoothly rotates the inspected 3D stupa model; thumbstick click resets rotation. |
| **VR Museum** | Right / Left | Grip Button | Hold Grip on Relic | Halts auto-spin; model follows 6-DOF hand motion; triggers gallery dimming. |
| **VR Museum** | Right Hand | Buttons A / B | Hold Button A or B | Button A reduces artifact scale; Button B enlarges artifact scale smoothly. |
| **VR Museum** | Left Hand | Button Y | Single Click | Swaps between weathered excavation scan and archaeologically restored model. |
| **VR Museum** | Left Hand | Button X / Close | Single Click | Exits inspection mode; restores original gallery lighting and pedestal pose. |
| **On-Site MR** | Right Hand | Grip + Stick | Hold Grip + Move Stick | Translates and rotates the virtual entrance outline to match physical park sign. |
| **On-Site MR** | Right Hand | Button A | Single Click | Confirms alignment; invokes hardware OVRSpatialAnchor; anchors 1:1 monument. |
| **On-Site MR** | Right Hand | Buttons A / B | Single Click (Post-Align) | Button A toggles monument visibility; Button B opens archaeological evidence panel. |
| **On-Site MR** | Left Hand | Button X | Hold for 2 Seconds | Clears active spatial session anchor; returns curator to alignment calibration mode. |
