# LangkaLens Senior Project Report — Extracted Diagrams & Draw.io Suite

This directory contains all **22 diagrams** extracted from [`Docs/Report/LangkaLens_Senior_Project_Report.docx`](../LangkaLens_Senior_Project_Report.docx) converted into a multi-page **Draw.io (`.drawio`)** workspace where every shape, connector, and text label is vector-based and editable.

---

## 🚀 Quick Start — How to Edit in Draw.io

### Option A: Web Browser (Fastest, Free, No Install)
1. Open [**app.diagrams.net**](https://app.diagrams.net/) (formerly draw.io).
2. Click **"Open Existing Diagram"** (or simply drag and drop the file into the browser window).
3. Select [`Docs/Report/LangkaLens_All_Diagrams.drawio`](../LangkaLens_All_Diagrams.drawio) (or `extracted_diagrams/LangkaLens_All_Diagrams.drawio`).
4. **All 22 diagrams will load in a single workspace with tabs at the bottom!**
   - Click any tab to switch between diagrams.
   - Double-click any box to edit text.
   - Click and drag boxes or arrow endpoints to adjust routing.
   - Change colors, line styles, or fonts from the right-hand panel.

### Option B: VS Code / Cursor IDE (Inline Editing)
1. Install the official extension **"Draw.io Integration"** (`hediet.vscode-drawio`).
2. Click directly on [`Docs/Report/LangkaLens_All_Diagrams.drawio`](../LangkaLens_All_Diagrams.drawio) or any file in [`drawio/`](drawio/).
3. The Draw.io graphical editor opens right inside your editor tabs.

### Option C: Draw.io Desktop App
1. Download and install [Draw.io Desktop](https://get.diagrams.net/).
2. Open [`Docs/Report/LangkaLens_All_Diagrams.drawio`](../LangkaLens_All_Diagrams.drawio).

---

## 📁 Directory Structure

```text
Docs/Report/
├── LangkaLens_All_Diagrams.drawio       <-- Master multi-page file (All 22 diagrams in tabs)
└── extracted_diagrams/
    ├── LangkaLens_All_Diagrams.drawio   <-- Master multi-page file (Identical backup)
    ├── README.md                        <-- This guide
    ├── drawio/                          <-- 22 Standalone .drawio files
    │   ├── Fig_3_01_System_Architecture.drawio
    │   ├── Fig_3_02_Visitor_Functions.drawio
    │   └── ...
    ├── from_docx/                       <-- Original bitmap images extracted from the .docx file
    │   ├── Fig_3_01_Main_Application_Components.png
    │   └── ...
    ├── png/                             <-- High-resolution PNG renderings
    │   ├── Fig_3_01_Main_Application_Components.png
    │   └── ...
    ├── svg/                             <-- Scalable Vector Graphics (SVG)
    │   ├── Fig_3_01_Main_Application_Components.svg
    │   └── ...
    └── mermaid/                         <-- Mermaid (.mmd) source definitions
        ├── Fig_3_01_Main_Application_Components.mmd
        └── ...
```

---

## 📊 Complete Catalog of Extracted Diagrams

### Chapter 3: System Architecture, Workflows & UML Sequence Diagrams

| Tab / Figure | Diagram Title & Description | Standalone Draw.io | SVG | Mermaid | Extracted DOCX Image |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **Tab 01 / Fig 3.1** | **Main Application Components & External Services**<br>High-level system topology, Unity client on Quest 3, Gemini & Groq APIs. | [drawio](drawio/Fig_3_01_System_Architecture.drawio) | [svg](svg/Fig_3_01_Main_Application_Components.svg) | [mmd](mermaid/Fig_3_01_Main_Application_Components.mmd) | [png](from_docx/Fig_3_01_Main_Application_Components.png) |
| **Tab 02 / Fig 3.2** | **Visitor Functions & Use Case Hierarchy**<br>System capabilities for language selection, tour, museum, MR, and AI guide. | [drawio](drawio/Fig_3_02_Visitor_Functions.drawio) | [svg](svg/Fig_3_02_Visitor_Functions.svg) | [mmd](mermaid/Fig_3_02_Visitor_Functions.mmd) | [png](from_docx/Fig_3_02_Visitor_Functions.png) |
| **Tab 03 / Fig 3.3** | **Application Overview & Scene Navigation**<br>Scene routing between Main Menu, VR Tour, VR Museum, and MR Scene. | [drawio](drawio/Fig_3_03_Enabled_Scenes.drawio) | [svg](svg/Fig_3_03_Overview_Of_Four_Scenes.svg) | [mmd](mermaid/Fig_3_03_Overview_Of_Four_Scenes.mmd) | [png](from_docx/Fig_3_03_Overview_Of_Four_Scenes.png) |
| **Tab 04 / Fig 3.4** | **Main Menu Workflow**<br>Language confirmation lifecycle and destination mode selection. | [drawio](drawio/Fig_3_04_Main_Menu_Workflow.drawio) | [svg](svg/Fig_3_04_Main_Menu_Workflow.svg) | [mmd](mermaid/Fig_3_04_Main_Menu_Workflow.mmd) | [png](from_docx/Fig_3_04_Main_Menu_Workflow.png) |
| **Tab 05 / Fig 3.5** | **VR Tour Navigation Flowchart**<br>Viewpoint transition, panoramic texture loading, and node arrival gating. | [drawio](drawio/Fig_3_05_Tour_Navigation.drawio) | [svg](svg/Fig_3_05_Tour_Navigation.svg) | [mmd](mermaid/Fig_3_05_Tour_Navigation.mmd) | [png](from_docx/Fig_3_05_Tour_Navigation.png) |
| **Tab 06 / Fig 3.6** | **VR Tour Reconstruction Inspection Flowchart**<br>Loading 3D site reconstruction via Left Y button, interaction, and cleanup. | [drawio](drawio/Fig_3_06_Tour_Inspection.drawio) | [svg](svg/Fig_3_06_Tour_Inspection.svg) | [mmd](mermaid/Fig_3_06_Tour_Inspection.mmd) | [png](png/Fig_3_06_Tour_Inspection.png) |
| **Tab 07 / Fig 3.7** | **VR Museum Exploration & Inspection Flowchart**<br>Artifact raycast selection, environment dimmer fade, and model switching. | [drawio](drawio/Fig_3_07_Museum_Exploration.drawio) | [svg](svg/Fig_3_07_Museum_Exploration.svg) | [mmd](mermaid/Fig_3_07_Museum_Exploration.mmd) | [png](png/Fig_3_07_Museum_Exploration.png) |
| **Tab 08 / Fig 3.8** | **MR Entrance Alignment & Anchor Flowchart**<br>Controller manual alignment, session spatial anchor creation, and verification. | [drawio](drawio/Fig_3_08_MR_Alignment.drawio) | [svg](svg/Fig_3_08_MR_Alignment.svg) | [mmd](mermaid/Fig_3_08_MR_Alignment.mmd) | [png](from_docx/Fig_3_08_MR_Alignment.png) |
| **Tab 09 / Fig 3.9** | **MR Display Gating & Tracking Recovery Flowchart**<br>Unrestricted footprint model spawner, tracking loss recovery, and UI panel sync. | [drawio](drawio/Fig_3_09_MR_Display_Recovery.drawio) | [svg](svg/Fig_3_09_MR_Display_Recovery.svg) | [mmd](mermaid/Fig_3_09_MR_Display_Recovery.mmd) | [png](from_docx/Fig_3_09_MR_Display_Recovery.png) |
| **Tab 10 / Fig 3.10** | **Shared Multimodal AI Heritage Guide Workflow**<br>Speech input, transcription verification, Gemini prompt delivery, and fallback. | [drawio](drawio/Fig_3_10_Shared_AI_Workflow.drawio) | [svg](svg/Fig_3_10_Shared_AI_Workflow.svg) | [mmd](mermaid/Fig_3_10_Shared_AI_Workflow.mmd) | [png](from_docx/Fig_3_10_Shared_AI_Workflow.png) |
| **Tab 11 / Fig 3.11** | **UML Sequence: Asynchronous Tour Viewpoint Transition**<br>Lifelines between Visitor, NavigationArrow, TourManager, Addressables, and Panorama. | [drawio](drawio/Fig_3_11_Tour_Transition_Sequence.drawio) | [svg](svg/Fig_3_11_Tour_Transition_Sequence.svg) | [mmd](mermaid/Fig_3_11_Tour_Transition_Sequence.mmd) | [png](from_docx/Fig_3_11_Tour_Transition_Sequence.png) |
| **Tab 12 / Fig 3.12** | **UML Sequence: Museum Inspection & Environment Dimmer**<br>Lifelines between Visitor, ArtifactData, InspectionManager, EnvironmentDimmer. | [drawio](drawio/Fig_3_12_Museum_Inspection_Sequence.drawio) | [svg](svg/Fig_3_12_Museum_Inspection_Dimmer.svg) | [mmd](mermaid/Fig_3_12_Museum_Inspection_Dimmer.mmd) | [png](from_docx/Fig_3_12a_Museum_Inspection_Dimmer.png) |
| **Tab 13 / Fig 3.13** | **UML Sequence: MR Entrance Alignment & Spatial Anchoring**<br>Lifelines between Visitor, MRMeshReferenceAlignment, SpatialAnchor, PlayableArea. | [drawio](drawio/Fig_3_13_MR_Alignment_Sequence.drawio) | [svg](svg/Fig_3_13_MR_Alignment_Sequence.svg) | [mmd](mermaid/Fig_3_13_MR_Alignment_Sequence.mmd) | [png](from_docx/Fig_3_13_Session_Alignment_Control.png) |
| **Tab 14 / Fig 3.14** | **UML Sequence: Speech Transcription & Gemini Query**<br>Lifelines between Visitor, EditableQuestionField, SpeechToTextHandler, Groq, Gemini. | [drawio](drawio/Fig_3_14_AI_Guide_Query_Sequence.drawio) | [svg](svg/Fig_3_14_Speech_Transcription_Chat.svg) | [mmd](mermaid/Fig_3_14_Speech_Transcription_Chat.mmd) | [png](from_docx/Fig_3_14_Speech_Transcription_Chat.png) |

---

### Chapter 4: Engineering Implementation & Build Verification Flowcharts

| Tab / Figure | Diagram Title & Description | Standalone Draw.io | PNG | Extracted DOCX Image |
| :--- | :--- | :---: | :---: | :---: |
| **Tab 15 / Fig 4.13** | **Development Environment & Project Restoration**<br>Numbered 6-step project setup sequence for Unity 6000.3.16f1 and Android SDK/NDK. | [drawio](drawio/Fig_4_13_Environment_Setup.drawio) | [png](png/Fig_4_13_Environment_Setup.png) | [png](from_docx/Fig_4_13_Environment_Setup.png) |
| **Tab 16 / Fig 4.14** | **Shared Scene Components & Inspector References**<br>Connection mapping across Meta rig, world-space canvas, localization tables, and data assets. | [drawio](drawio/Fig_4_14_Shared_Scene_Components.drawio) | [png](png/Fig_4_14_Shared_Scene_Components.png) | [png](from_docx/Fig_4_14_Shared_Scene_Components.png) |
| **Tab 17 / Fig 4.15** | **Main Menu Event Connections & Loading Destinations**<br>Button click routing from MainMenuManager methods to registered scene destinations. | [drawio](drawio/Fig_4_15_Main_Menu_Connections.drawio) | [png](png/Fig_4_15_Main_Menu_Connections.png) | [png](from_docx/Fig_4_15_Main_Menu_Connections.png) |
| **Tab 18 / Fig 4.16** | **VR Tour Asset Relationships & Loading Paths**<br>Structural separation between panorama tour assets and reconstruction prefab loading. | [drawio](drawio/Fig_4_16_VR_Tour_Asset_Relations.drawio) | [png](png/Fig_4_16_VR_Tour_Asset_Relations.png) | [png](from_docx/Fig_4_16_VR_Tour_Asset_Relations.png) |
| **Tab 19 / Fig 4.17** | **Museum Station Configuration & Inspection Lifecycle**<br>Artifact station components, raycast trigger, dimmer fade, and state restoration. | [drawio](drawio/Fig_4_17_Museum_Station_Config.drawio) | [png](png/Fig_4_17_Museum_Station_Config.png) | [png](from_docx/Fig_4_17_Museum_Station_Config.png) |
| **Tab 20 / Fig 4.18** | **MR Alignment, Session Anchoring & Display Dependencies**<br>Reference mesh alignment, Quest 3 spatial anchor tracking, and unrestricted footprint display. | [drawio](drawio/Fig_4_18_MR_Alignment_Dependencies.drawio) | [png](png/Fig_4_18_MR_Alignment_Dependencies.png) | [png](from_docx/Fig_4_18_MR_Alignment_Dependencies.png) |
| **Tab 21 / Fig 4.19** | **Shared AI Guide Data Flow for Typed & Spoken Questions**<br>Data pipeline separating microphone audio, Groq Whisper transcription, user review, and Gemini. | [drawio](drawio/Fig_4_19_Shared_AI_Guide_Flow.drawio) | [png](png/Fig_4_19_Shared_AI_Guide_Flow.png) | [png](from_docx/Fig_4_19_Shared_AI_Guide_Flow.png) |
| **Tab 22 / Fig 4.20** | **Build & Verification Sequence for Reproducible Project**<br>Complete 6-step lifecycle from scene check, Addressables build, APK compilation to Quest QA. | [drawio](drawio/Fig_4_20_Build_Verification.drawio) | [png](png/Fig_4_20_Build_Verification.png) | [png](from_docx/Fig_4_20_Build_Verification.png) |

---

## 🛠️ Editing Tips in Draw.io

1. **Re-routing Connectors:** Click any arrow to reveal its blue waypoint handles. Drag them to reroute orthogonal paths.
2. **Changing Styles & Colors:** Select any box or shape, then use the **Style** tab on the right sidebar to change fill colors, border thickness, or rounded corners.
3. **Exporting Back to Word/PDF:**
   - Go to `File -> Export as -> PNG...`
   - Set **DPI to 300** (or 260) and enable **"Transparent Background"** (or white background).
   - The exported image can be directly inserted into Word or Google Docs with zero blurriness.
