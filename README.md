# LangkaLens (ลังกาเลนส์)

[![Unity Version](https://img.shields.io/badge/Unity-6000.3.16f1-blue.svg?logo=unity)](https://unity.com/)
[![Target Platform](https://img.shields.io/badge/Platform-Meta%20Quest%202%20%7C%203%20%7C%203S%20%7C%20Pro-blueviolet.svg?logo=oculus)](https://www.meta.com/quest/)
[![SDK](https://img.shields.io/badge/Meta%20XR%20SDK-v203.0.2-green.svg)](https://developer.oculus.com/)
[![Render Pipeline](https://img.shields.io/badge/URP-17.3.0-orange.svg)](https://unity.com/srp/Universal-Render-Pipeline)
[![License](https://img.shields.io/badge/License-Academic%20%2F%20Research-lightgrey.svg)](#license)

**LangkaLens** is an immersive Virtual Reality (VR) and Mixed Reality (MR) cultural heritage application designed for Meta Quest headsets. It brings the rich archaeological history of the **Ancient Yarang City** (*Ban Jalae, Pattani Province, Southern Thailand*) into the spatial computing era through interactive 360° tours, virtual museum artifact inspection, on-site passthrough mixed reality reconstruction, and an intelligent AI heritage guide.

---

## 🏛️ Project Highlights & Features

### 1. 🌐 VR 360° Photosphere Tour Mode
- **Spatial Waypoint Navigation**: Explore historical archaeological ruins (BN2, BN3, BN8, BW9) across Yarang Ancient City.
- **Dynamic Street-View Transitions**: Custom mesh warp, pinch, and directional lunge interpolation between 360° photospheres.
- **3D Ghost Reconstruction Overlay**: Interactive hover points revealing digital photogrammetry reconstructions over real-world ruins.
- **On-Demand Texture Streaming**: Built on Unity Addressables for smooth memory management on standalone Android hardware.
- **In-VR Mini-Map**: Spatial map menu toggleable via controller (`Y` button) for rapid site jumps.

### 2. 🏺 VR Museum Mode (Artifact Gallery & Inspection)
- **Universal One-Handed Locomotion**: Dynamic joystick auto-detection with gaze-directed `CharacterController` translation and yaw rotation.
- **Pedestal Proximity & Inspection Glide**: Smooth camera rig glide directly facing artifacts upon interaction.
- **Meta Interaction SDK Rotation**: Free-rotate 3D archaeological artifacts with controllers or hands (`OneGrabFreeRotateTransformer`), with auto-spin idle states.
- **Contextual Spatial UI**: Trilingual/bilingual artifact metadata, historical descriptions, and localized narration audio.

### 3. 👓 Mixed Reality (MR) On-Site Experience
- **Passthrough Heritage Blending**: High-fidelity passthrough underlay via `OVRPassthroughLayer` blending physical site ruins with reconstructed ancient architecture.
- **Physical Entrance Alignment**: Manual entrance alignment tool (`Mesh_ref`) mapping physical real-world signs/tables to virtual coordinates.
- **Session Spatial Anchors**: Spatial persistence locking virtual reconstructions accurately to real-world positions.
- **Playable Boundary & Safety Gating**: Outer/inner hallway boundary tracking (`PlayableBoundary`) that automatically pauses and hides content if visitors step outside the designated safe path.

### 4. 🤖 Gemini AI Cultural Heritage Guide
- **Interactive Spatial Dialogue**: Integrated Google Gemini Generative Language API assistant (`GeminiController`).
- **Archaeological Persona**: Configured with strict historical expertise guidelines tailored to Southern Thai and Dvaravati/Srivijaya maritime archaeology.
- **Multi-Modal Input**: Accessible via virtual spatial keyboard and voice dictation.

### 5. 🌍 Localization & Accessibility
- Integrated **Unity Localization** supporting Thai and English languages.
- First-time setup prompt with seamless locale switching across all spatial UI panels and artifact translations.

---

## 🏗️ Technical Architecture & Stack

| Component | Technology / Version |
|---|---|
| **Game Engine** | Unity 6 (`6000.3.16f1`) |
| **Render Pipeline** | Universal Render Pipeline (URP `17.3.0`) |
| **XR Runtime** | OpenXR (`com.unity.xr.openxr` `1.16.1`) |
| **Target Hardware** | Meta Quest 2, Quest 3, Quest 3S, Quest Pro |
| **Meta XR SDK** | Meta XR All-In-One SDK (`v203.0.2`) |
| **Interaction Framework** | Meta XR Interaction SDK (ISDK) |
| **Asset Streaming** | Unity Addressables (`2.9.1`) |
| **Localization** | Unity Localization (`1.5.12`) |
| **AI Integration** | Google Gemini REST API via `UnityWebRequest` |

---

## 📂 Project Structure

```text
LangkaLens/
├── Assets/
│   └── _LangkaLens/
│       ├── Main_menu/         # Main menu hub, language selection, scene dispatcher
│       ├── Tour/              # 360 photosphere street-view tour, waypoints, addressables
│       ├── Museum/            # Virtual museum scene, artifact pedestals, inspection logic
│       ├── MR/                # Mixed Reality scene, boundary tracking, entrance alignment
│       └── Mixed/             # Shared scripts, Gemini AI guide, UI prefabs, audio
├── Packages/                  # Unity Package Manager manifest & lockfiles
├── ProjectSettings/           # Unity project settings (XR, URP, Input, TagManager)
├── Docs/                      # Architecture notes, setup guides, and flowcharts
├── Artifacts_UI_Translations.csv # UI & artifact localization dictionary
├── PROJECT_PROGRESS.md        # Technical dev log & MR validation milestones
└── AI_PROMPT_FLOWCHART.md     # System architecture & state transition flowchart prompt
```

---

## 🚀 Getting Started

### Prerequisites
1. **Unity Hub** with **Unity 6 (6000.3.16f1)** installed.
2. **Android Build Support** modules:
   - Android SDK & NDK Tools
   - OpenJDK
3. **Meta Quest Link / Air Link** or **SideQuest** for deployment.
4. **Git LFS** enabled (required for large 3D models, textures, and photosphere assets):
   ```bash
   git lfs install
   ```

### Clone & Open
```bash
git clone https://github.com/SobreeH/LangkaLens-V1.git
cd LangkaLens-V1
git lfs pull
```

1. Open **Unity Hub** -> Click **Add** -> Select the project root folder.
2. Ensure the active build target is set to **Android** (`File > Build Profiles` or `Build Settings`).
3. Set Texture Compression to **ASTC**.

### Key Scenes
- **Main Menu**: `Assets/_LangkaLens/Main_menu/Scenes/Main_Menu_Scene.unity` (Default entry point)
- **VR Tour**: `Assets/_LangkaLens/Tour/Scenes/Tour_Scene.unity`
- **VR Museum**: `Assets/_LangkaLens/Museum/Scenes/Museum_Scene.unity`
- **Mixed Reality**: `Assets/_LangkaLens/MR/Scenes/MR_Scene.unity`

---

## 🎮 MR Alignment Controls (On-Site Setup)

When deploying `MR_Scene` on-site at Ban Jalae 3:
1. **Hold Right Grip**: Display and position the `Mesh_ref` alignment outline over the physical entrance sign/table.
2. **Right Thumbstick (Horizontal)**: Rotate the virtual site orientation.
3. **Right Thumbstick (Vertical)**: Adjust placement distance/reach.
4. **Release Grip + Press 'A'**: Confirm alignment and lock the session spatial anchor.
5. **Hold 'X' (2 seconds)**: Reset and re-align.

---

## 📄 License & Attribution

- **Cultural Heritage Assets & 3D Scans**: Archaeological data and photogrammetry models derived from the Yarang Ancient City excavations (Fine Arts Department of Thailand & research partners).
- **Codebase**: Developed for research and educational mixed-reality cultural heritage preservation.

---

*Developed with ❤️ for Cultural Heritage Preservation in Southern Thailand.*
