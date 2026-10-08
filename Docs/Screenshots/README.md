# LangkaLens screenshot library

Captured on **5 October 2026** for the [senior project report](../Report/LangkaLens_Senior_Project_Report.md). Keep these original PNGs for future reports, presentations, and project documentation.

- `Development/`: four actual Unity Editor screenshots, including Scene View, Hierarchy, and Inspector.
- `Playtesting/`: nine actual Meta XR Simulator window captures of LangkaLens in Unity Play Mode.
- [capture-manifest.json](capture-manifest.json): captions, scene paths, capture context, image dimensions, and SHA-256 hashes.
- The eight rendered C# figures are stored separately in [the report's code figures folder](../Report/figures/code/); their exact source excerpts and hashes are in [source-manifest.json](../Report/figures/code/source-manifest.json).

## Capture context

Unity **6000.3.16f1**; Meta XR Simulator **207.0** (executable build **207.0.0.18.123**); simulated **Meta Quest 3**; **Left eye** viewport; selected **72 fps** refresh-rate setting. The visible value identifies the selected simulator refresh-rate setting. Runtime screenshots include the simulator title and active-runtime indicator.

The application callbacks and inspection methods opened the shown screens. Inspection viewpoints were adjusted temporarily to include the models and their panels. Scene changes were not saved. No AI request or speech recording was made. The simulator was already activated and its activation was preserved. After capture, Unity returned to the museum scene with Play Mode stopped.

The screenshots show development and simulated runtime states and document interface presentation. Physical passthrough and on-site MR tracking are outside these simulator captures. Preserve the simulator context when reusing the images.

The museum captures (Figures 4.11, 4.19, and 4.20) predate the inspection environment fade documented on 6 October 2026. They retain the 5 October development/runtime presentation.

## Figure index

| Report figure | Original PNG | Scene | Caption |
|---|---|---|---|
| Figure 4.9 | [unity-main-menu-editor.png](Development/unity-main-menu-editor.png) | `Main_Menu_Scene` | Main menu setup in Unity, showing language-panel references and the three destination scenes. |
| Figure 4.10 | [unity-tour-editor.png](Development/unity-tour-editor.png) | `VR_Tour_Scene` | Tour setup in Unity, showing the four area assets, panorama renderer, and navigation references. |
| Figure 4.11 | [unity-museum-editor.png](Development/unity-museum-editor.png) | `VR_Museum_Scene` | Museum exhibit layout in Unity and an ArtifactData component with localized descriptions and original/alternate models. |
| Figure 4.12 | [unity-mr-alignment-editor.png](Development/unity-mr-alignment-editor.png) | `MR_Scene` | MR development setup in Unity, showing the entrance reference mesh, tracked-object references, and unrestricted footprint configuration. |
| Figure 4.13 | [language-selection-simulator.png](Playtesting/language-selection-simulator.png) | `Main_Menu_Scene` | Thai and English language selection at the start of a session in Meta XR Simulator. |
| Figure 4.14 | [main-menu-simulator.png](Playtesting/main-menu-simulator.png) | `Main_Menu_Scene` | The confirmed-language main menu with tour, museum, and mixed-reality choices in Meta XR Simulator. |
| Figure 4.15 | [user-guide-simulator.png](Playtesting/user-guide-simulator.png) | `Main_Menu_Scene` | The reusable user guide with mode tabs and control instructions in Meta XR Simulator. |
| Figure 4.16 | [tour-area-menu-simulator.png](Playtesting/tour-area-menu-simulator.png) | `VR_Tour_Scene` | Selection of Ban Ja-Lae 3, Ban Ja-Lae 2, Ban Ja-Lae 8, and Ban Wat 9 in Meta XR Simulator. |
| Figure 4.17 | [tour-panorama-simulator.png](Playtesting/tour-panorama-simulator.png) | `VR_Tour_Scene` | The 360-degree panorama at Ban Ja-Lae 3 viewpoint 3F1 in Meta XR Simulator. |
| Figure 4.18 | [tour-reconstruction-simulator.png](Playtesting/tour-reconstruction-simulator.png) | `VR_Tour_Scene` | Ban Ja-Lae 3 reconstruction inspection, with the model, site information, and faded panorama in Meta XR Simulator. |
| Figure 4.19 | [museum-gallery-simulator.png](Playtesting/museum-gallery-simulator.png) | `VR_Museum_Scene` | The virtual museum gallery and artifact stations in Meta XR Simulator. |
| Figure 4.20 | [museum-inspection-simulator.png](Playtesting/museum-inspection-simulator.png) | `VR_Museum_Scene` | Standing Stone Buddha in Vitarka Mudra inspection, including the description and model-switch control, in Meta XR Simulator. |
| Figure 4.21 | [ai-guide-simulator.png](Playtesting/ai-guide-simulator.png) | `VR_Tour_Scene` | The shared AI guide interface with editable input, Speak, Send, Clear, and Close controls in Meta XR Simulator. |

## Reuse

Use the filenames and captions above to identify each screen. Keep the originals unchanged; store any presentation crops or resized copies separately and label them as derivatives. The current report uses absolute workspace image paths so both report copies can display the same saved files. Update those links if the project is moved to another computer.
