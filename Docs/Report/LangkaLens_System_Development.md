# 1.2 System Development: LangkaLens Cultural Heritage Platform
**Academic Context:** Senior Project Technical Specification (Part 1.2)  
**Author:** Wansobree Hajihama (Student ID: 6620610023)  
**Project Advisor:** Assistant Professor Dr. Zamree Che-aron (ผศ.ดร.ซัมรี เจ๊ะอารน)  
**Academic Degree:** Bachelor of Science in Computer and Informatics for Management (CIM)  
**Institution:** Faculty of Communication Sciences, Prince of Songkla University, Pattani Campus  
**Target Hardware:** Meta Quest 3 & Meta Quest 3S (Horizon OS / Android 12L) | **Engine:** Unity 6000.3.16f1 URP  

---

# 1.2 System Development

System Development documents the technical construction, toolchain infrastructure, codebase organization, subsystem engineering implementations, and mobile VR optimization pipelines employed to realize LangkaLens. Developing for standalone XR headsets (Meta Quest 3 and Meta Quest 3S) introduces stringent hardware constraints: rendering must sustain 72 to 90 frames per second without frame drops to prevent cybersickness, while operating within the mobile thermal envelope and unified memory bandwidth of the Qualcomm Snapdragon XR2 Gen 2 platform.

> **Implementation Paradigm:** LangkaLens follows a decoupled component-based architecture in Unity 6000.3.16f1 URP. Core scene controllers manage rendering and user interaction, while Addressables handles memory lifecycles and external REST clients interface with Google Gemini and Groq Cloud asynchronously without blocking the main rendering loop.

---

## 1.2.1 Development Environment & Toolchain Infrastructure

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

---

## 1.2.2 Project Directory Structure & Software Organization

The project repository follows a strict modular structure under `Assets/_LangkaLens/`:
- `_Scenes/`: The four enabled runtime scenes (`Main_Menu_Scene.unity`, `VR_Tour_Scene.unity`, `VR_Museum_Scene.unity`, `MR_Scene.unity`).
- `Main_menu/`: `MainMenuManager.cs`, audio feedback clips, and `RigResetter.cs` for camera calibration.
- `Tour/`: `TourManager.cs`, `TourNode.cs`, `TourArea.cs`, `TourReconstructionDefinition.cs`, and the Addressables catalog for 360 photos.
- `Museum/`: `ArtifactData.cs`, `OneGrabFreeRotateTransformer.cs`, exhibit pedestal prefabs, and 3D photogrammetric meshes.
- `MR/`: `MRMeshReferenceAlignment.cs`, `MRMeshPlayableArea.cs`, `MRModelSpawner.cs`, and `MRSiteInformationPanel.cs`.
- `Mixed/`: Cross-scene shared modules (`InspectionManager.cs`, `InspectionEnvironmentDimmer.cs`, `GeminiChatUI.cs`, `SpeechToTextHandler.cs`, `BilingualVirtualKeyboard.cs`).
- `Misc/`: Unity Localization StringTable collections, Editor validation utilities, and `UniversalOneHandedLocomotion.cs`.

---

## 1.2.3 Subsystem Engineering & Implementation Details

### Subsystem 1: Main Menu & Localization Engine
`MainMenuManager.cs` manages locale initialization, session language selection, and scene routing. Upon scene load, `RigResetter.cs` forces the OVRCameraRig tracking space to zero and recenters the camera to eye level. Unity Localization StringTable collections dynamically broadcast locale changes to all active TextMesh Pro labels.

![Figure 2.1: Unity Editor Authoring: Main Menu Scene Hierarchy](Screenshots/Development/unity-main-menu-editor.png)
![Figure 2.2: Meta XR Simulator Playtest: Main Menu Hub Interface](Screenshots/Playtesting/main-menu-simulator.png)

### Subsystem 2: VR 360-Degree Panoramic Tour Subsystem
The VR Tour subsystem renders high-resolution equirectangular spherical panoramas captured at Ancient Yarang City. `TourManager.cs` parses `TourNode` ScriptableObjects to construct an in-memory navigation graph. When the user selects a directional arrow, `TourManager` initiates an asynchronous transition: locks input, loads the new 8K `Texture2D` asynchronously via Addressables, interpolates camera container position via quadratic ease-in, releases the prior texture handle, and populates new indicators. `TourReconstructionPresenter.cs` enables in-situ 3D inspection of ancient stupas upon pressing button Y.

![Figure 2.3: Unity Editor Authoring: VR Tour Viewpoint Graph](Screenshots/Development/unity-tour-editor.png)
![Figure 2.4: Meta XR Simulator Playtest: 360 Panoramic Viewpoint Traversal](Screenshots/Playtesting/tour-panorama-simulator.png)

### Subsystem 3: VR Museum & 3D Artifact Inspection Subsystem
The Virtual Museum displays photogrammetric 3D scans of excavated Yarang artifacts. Each exhibit pedestal features an `ArtifactData` component linking localized titles, descriptions, and dual-mesh references. `InspectionManager.cs` triggers inspection mode, locking locomotion, while `InspectionEnvironmentDimmer.cs` executes an unscaled exposure fade on surrounding gallery renderers, darkening the room while spotlighting the exhibit. Users manipulate the model using `OneGrabFreeRotateTransformer.cs` and can toggle between weathered photogrammetry and restored forms via button Y.

![Figure 2.5: Unity Editor Authoring: VR Museum Gallery Hierarchy](Screenshots/Development/unity-museum-editor.png)
![Figure 2.6: Meta XR Simulator Playtest: 6-DOF Artifact Examination with Dimmer](Screenshots/Playtesting/museum-inspection-simulator.png)

### Subsystem 4: On-Site Mixed Reality Alignment & Reconstruction Subsystem
The Mixed Reality mode uses full-color stereoscopic video passthrough for the real-world background. `MRMeshReferenceAlignment.cs` enables curators to match a virtual bounding reference box with an on-site physical benchmark using the right grip and thumbstick. Confirming with button A creates a persistent hardware `OVRSpatialAnchor`. `MRMeshPlayableArea.cs` evaluates tracking health and gates content visibility through its `CanPlay` property. `MRModelSpawner.cs` instantiates the 1:1 scale reconstructed monument, allowing visitors to walk around the ancient stupa while observing physical brick foundations through passthrough.

![Figure 2.7: Unity Editor Authoring: On-Site MR Entrance Alignment](Screenshots/Development/unity-mr-alignment-editor.png)

### Subsystem 5: Multimodal AI Heritage Guide & Speech Subsystem
The intelligent guide is accessible across all scenes via a wrist tablet or submenu. `GeminiChatUI.cs` manages asynchronous HTTP requests to Google Gemini API (`gemini-1.5-flash` / `gemini-2.0-flash`). The request injects curated archaeological prompt instructions. The client implements exponential backoff and a single-attempt fallback model for HTTP 404/408/429/5xx errors. `SpeechToTextHandler.cs` captures audio via Unity's Microphone API, encodes 16-bit PCM WAV chunks, and streams them to Groq Whisper (`whisper-large-v3`). The returned transcript populates an editable input field. A virtual bilingual keyboard (`BilingualVirtualKeyboard.cs`) supports direct typed entry in English and Thai.

![Figure 2.8: Meta XR Simulator Playtest: Shared Multimodal AI Guide Interface](Screenshots/Playtesting/ai-guide-simulator.png)

### Summary of Subsystem Architectures

| Subsystem Name | Primary C# Controller | Key Auxiliary Components | XR SDK & Engine Drivers |
|---|---|---|---|
| **Main Menu & Localization** | MainMenuManager.cs | RigResetter.cs, SceneLoadingScreen.cs | Unity Localization 1.5.12, OVRCameraRig |
| **VR Panoramic Tour** | TourManager.cs | TourNode.cs, TourReconstructionPresenter.cs | Unity Addressables 2.9.1, URP Unlit Shaders |
| **VR Virtual Museum** | InspectionManager.cs | ArtifactData.cs, InspectionEnvironmentDimmer.cs | Meta Interaction SDK 203.0.0 (OneGrabFreeRotate) |
| **On-Site Mixed Reality** | MRMeshReferenceAlignment.cs | MRMeshPlayableArea.cs, MRModelSpawner.cs | Meta XR Core 203.0.0 (Passthrough, OVRSpatialAnchor) |
| **Multimodal AI Guide** | GeminiChatUI.cs | SpeechToTextHandler.cs, BilingualVirtualKeyboard.cs | UnityWebRequest, Groq Whisper REST, Gemini API |

---

## 1.2.4 Build Pipeline, Mobile XR Optimization & Asset Packaging

- **URP Forward+ Rendering & Single-Pass Stereo:** Forward+ rendering eliminates unnecessary multi-pass lighting overhead. Single-Pass Instanced Stereo rendering submits draw calls for both eyes in a single GPU pass, halving CPU driver overhead.
- **ASTC Texture Compression:** All 8K panoramic equirectangular textures and 4K photogrammetric PBR material maps are encoded using ASTC 6x6 block compression, significantly reducing GPU memory bandwidth and texture cache misses on the Adreno GPU.
- **Addressables Asset Bundling & Memory Management:** Panoramic textures and reconstruction prefabs are compiled into discrete Addressable AssetBundles. Unused bundles are deallocated immediately upon scene exit or viewpoint transition via `Addressables.Release()`, keeping the mobile heap footprint consistently below 2.5 GB.
- **Android XR Manifest & Permissions:** `AndroidManifest.xml` configures `android.hardware.vr.headtracking` (required), `RECORD_AUDIO` (for voice input), and `com.oculus.feature.PASSTHROUGH` (required for MR mode). Target SDK is Android 12L (API level 32) with 64-bit ARM64 architecture.
- **Automated Verification via Meta XR Simulator:** The desktop workflow leverages Meta XR Simulator v203.0 and MQDH v6.5.2 to validate 6-DOF tracking, hand controller gestures, language switching, and UI layouts on PC before pushing release APKs to physical headsets.

| Optimization Dimension | Technical Configuration | Mobile VR Impact | Target Threshold |
|---|---|---|---|
| **Stereo Rendering** | Single-Pass Instanced | Halves draw call submission overhead | Draw calls <= 120 per eye |
| **Texture Encoding** | ASTC 6x6 Block Compression | Reduces VRAM usage and bandwidth | Memory bandwidth <= 1.2 GB/s |
| **Dynamic Lighting** | Forward+ Clustered Lighting | Culls unneeded per-object lights | Real-time lights <= 4 active |
| **Memory Deallocation**| Addressables.Release() | Disposes unused 8K textures instantly | Heap memory < 2.5 GB |
| **Script Compilation** | IL2CPP with ARM64 Bitcode | Executes high-performance C++ code | CPU frame time <= 10.0 ms |
