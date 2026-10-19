# LangkaLens 3D Models Image Catalog

This directory contains high-definition studio renders of all 3D models and artifacts used across the **LangkaLens** Meta Quest VR/MR project.

All images are rendered directly from Unity with:
- **Format**: 32-bit ARGB PNG with clean transparent alpha background.
- **Lighting**: Studio 3-point illumination (Warm Key Light, Cool Fill Light, High-contrast Rim Light).
- **Anti-Aliasing**: 8x Multi-Sample Anti-Aliasing (MSAA) with offscreen isolated `PreviewScene` rendering.
- **Framing**: Bounding-box fitted framing at 3/4 isometric beauty angles.

---

## Directory Structure

```
Docs/Images/
├── 01_Museum_Artifacts/           # 22 Individual artifact renders (11 Original + 11 Restored, 1024x1024)
├── 02_Artifact_Comparisons/       # 11 Widescreen side-by-side comparison sheets (1920x1080)
├── 03_Historic_Reconstructions/   # 7 Ancient monument 3D architectural renders (1024x1024)
├── 04_Museum_Demo_Exhibits/       # 5 Museum gallery demo ceramic & relic exhibits (1024x1024)
├── 05_Interactive_Props_And_Icons/# 7 VR Tour navigation arrows, 3D UI icons & pedestals (1024x1024)
└── 06_Photogrammetry_Assets/      # 7 Photogrammetric scans & environmental materials (1024x1024)
```

---

## 1. Museum Archaeological Artifacts (`01_Museum_Artifacts/`)

High-fidelity renders of the 11 key archaeological artifacts excavated from the Yarang Ancient City complex. Each artifact is captured in both its weathered original excavated condition (`_ORG`) and its 3D digitally reconstructed museum state (`_RES`).

| Artifact | Original Render (`_ORG`) | Restored Render (`_RES`) | Original Asset Source |
| :--- | :--- | :--- | :--- |
| **Standing Buddha**<br>พระพุทธรูปยืนศิลปะทวารวดี | [Buddha_ORG.png](01_Museum_Artifacts/Buddha_ORG.png) | [Buddha_RES.png](01_Museum_Artifacts/Buddha_RES.png) | `Assets/_LangkaLens/Museum/3D Models/Artifacts/Original/Buddha_ORG.glb` |
| **Nandi Sacred Bull**<br>ประติมากรรมโคนันทิ | [Cow_ORG.png](01_Museum_Artifacts/Cow_ORG.png) | [Cow_RES.png](01_Museum_Artifacts/Cow_RES.png) | `Assets/_LangkaLens/Museum/3D Models/Artifacts/Original/Cow_ORG.glb` |
| **Carved Stone Pillar**<br>เสาหินสลักโบราณ | [Pillar_ORG.png](01_Museum_Artifacts/Pillar_ORG.png) | [Pillar_RES.png](01_Museum_Artifacts/Pillar_RES.png) | `Assets/_LangkaLens/Museum/3D Models/Artifacts/Original/Pillar_ORG.glb` |
| **Standing Bodhisattva / Deity**<br>เทวรูปโบราณ | [Statue_ORG.png](01_Museum_Artifacts/Statue_ORG.png) | [Statue_RES.png](01_Museum_Artifacts/Statue_RES.png) | `Assets/_LangkaLens/Museum/3D Models/Artifacts/Original/Statue_ORG.glb` |
| **Stupa Type 1**<br>สถูปจำลอง แบบที่ 1 | [Stupa1_ORG.png](01_Museum_Artifacts/Stupa1_ORG.png) | [Stupa1_RES.png](01_Museum_Artifacts/Stupa1_RES.png) | `Assets/_LangkaLens/Museum/3D Models/Artifacts/Original/Stupa1_ORG.glb` |
| **Stupa Type 2**<br>สถูปจำลอง แบบที่ 2 | [Stupa2_ORG.png](01_Museum_Artifacts/Stupa2_ORG.png) | [Stupa2_RES.png](01_Museum_Artifacts/Stupa2_RES.png) | `Assets/_LangkaLens/Museum/3D Models/Artifacts/Original/Stupa2_ORG.glb` |
| **Stupa Type 3**<br>สถูปจำลอง แบบที่ 3 | [Stupa3_ORG.png](01_Museum_Artifacts/Stupa3_ORG.png) | [Stupa3_RES.png](01_Museum_Artifacts/Stupa3_RES.png) | `Assets/_LangkaLens/Museum/3D Models/Artifacts/Original/Stupa3_ORG.glb` |
| **Stupa Type 4**<br>สถูปจำลอง แบบที่ 4 | [Stupa4_ORG.png](01_Museum_Artifacts/Stupa4_ORG.png) | [Stupa4_RES.png](01_Museum_Artifacts/Stupa4_RES.png) | `Assets/_LangkaLens/Museum/3D Models/Artifacts/Original/Stupa4_ORG.glb` |
| **Surya / Trinity Stele**<br>แผ่นสลักสุริยเทพ / ตรีมูรติ | [Tri_ORG.png](01_Museum_Artifacts/Tri_ORG.png) | [Tri_RES.png](01_Museum_Artifacts/Tri_RES.png) | `Assets/_LangkaLens/Museum/3D Models/Artifacts/Original/Tri_ORG.glb` |
| **Kurma Sacred Turtle**<br>ประติมากรรมเต่ามงคล | [Turtle_ORG.png](01_Museum_Artifacts/Turtle_ORG.png) | [Turtle_RES.png](01_Museum_Artifacts/Turtle_RES.png) | `Assets/_LangkaLens/Museum/3D Models/Artifacts/Original/Turtle_ORG.glb` |
| **Dharmachakra Wheel**<br>ธรรมจักรศิลา | [Wheel_ORG.png](01_Museum_Artifacts/Wheel_ORG.png) | [Wheel_RES.png](01_Museum_Artifacts/Wheel_RES.png) | `Assets/_LangkaLens/Museum/3D Models/Artifacts/Original/Wheel_ORG.glb` |

---

## 2. Artifact Original vs Restored Comparisons (`02_Artifact_Comparisons/`)

Widescreen 1920x1080 composite renders presenting the Original Fragment (Left) directly alongside the Digital Restoration (Right) under identical lighting and camera perspective. Ideal for senior project reports, thesis defense slides, academic papers, and promotional materials.

| Exhibit | Comparison Image | Details |
| :--- | :--- | :--- |
| **Buddha** | [Buddha_Comparison.png](02_Artifact_Comparisons/Buddha_Comparison.png) | Weathered sandstone posture vs. full abhaya mudra restored form |
| **Nandi Cow** | [Cow_Comparison.png](02_Artifact_Comparisons/Cow_Comparison.png) | Weathered terracotta scan vs. smooth restored sacred zebu bull |
| **Carved Pillar** | [Pillar_Comparison.png](02_Artifact_Comparisons/Pillar_Comparison.png) | Eroded base relief vs. crisp carved architectural column |
| **Statue** | [Statue_Comparison.png](02_Artifact_Comparisons/Statue_Comparison.png) | Armless excavated figurine vs. full headdress deity restoration |
| **Stupa 1** | [Stupa1_Comparison.png](02_Artifact_Comparisons/Stupa1_Comparison.png) | Segmented miniature stupa comparison |
| **Stupa 2** | [Stupa2_Comparison.png](02_Artifact_Comparisons/Stupa2_Comparison.png) | Tiered bell-shaped stupa comparison |
| **Stupa 3** | [Stupa3_Comparison.png](02_Artifact_Comparisons/Stupa3_Comparison.png) | Conical finial stupa comparison |
| **Stupa 4** | [Stupa4_Comparison.png](02_Artifact_Comparisons/Stupa4_Comparison.png) | Stepped square base stupa comparison |
| **Surya Stele** | [Tri_Comparison.png](02_Artifact_Comparisons/Tri_Comparison.png) | Weathered dark stone relief vs. ornate bronze aureole restoration with 7 horses |
| **Kurma Turtle** | [Turtle_Comparison.png](02_Artifact_Comparisons/Turtle_Comparison.png) | Cracked stone shell vs. fully articulated Kurma avatar sculpture |
| **Dharmachakra** | [Wheel_Comparison.png](02_Artifact_Comparisons/Wheel_Comparison.png) | Weathered stone wheel hub vs. restored eight-spoke law wheel |

---

## 3. Historic Monument 3D Reconstructions (`03_Historic_Reconstructions/`)

Full architectural reconstructions of archaeological sites in Yarang, used in the **360 VR Tour** and **Mixed Reality Spatial Alignment** modes.

| Monument / Structure | Image Link | Description | Source File |
| :--- | :--- | :--- | :--- |
| **Ban Ja-Lae 2 (BJ2)** | [OG_BJ2_Ban_Ja-Lae_2.png](03_Historic_Reconstructions/OG_BJ2_Ban_Ja-Lae_2.png) | Reconstructed brick stupa and stepped base at Site BJ2 | `Assets/_LangkaLens/Misc/3D Models/RestoredModels/OG/OG_BJ2.glb` |
| **Ban Ja-Lae 3 (BJ3)** | [OG_BJ3_Ban_Ja-Lae_3.png](03_Historic_Reconstructions/OG_BJ3_Ban_Ja-Lae_3.png) | Monumental multi-tiered sanctuary at Site BJ3 | `Assets/_LangkaLens/MR/3D Models/RestoredModels/OG/OG_BJ3.glb` |
| **Ban Ja-Lae 8 (BJ8)** | [OG_BJ8_Ban_Ja-Lae_8.png](03_Historic_Reconstructions/OG_BJ8_Ban_Ja-Lae_8.png) | Shrine compound with wooden pavilion and tiered stupa | `Assets/_LangkaLens/Misc/3D Models/RestoredModels/OG/OG_BJ8.glb` |
| **Bo-Wa 9 (BW9)** | [OG_BW9_Bo-Wa_9.png](03_Historic_Reconstructions/OG_BW9_Bo-Wa_9.png) | Reconstructed religious monument at Ban Bo-Wa Site 9 | `Assets/_LangkaLens/Misc/3D Models/RestoredModels/OG/OG_BW9.glb` |
| **Temple Complex** | [Temple_Complex.png](03_Historic_Reconstructions/Temple_Complex.png) | Comprehensive ancient brick sanctuary with guardian shrines | `Assets/_LangkaLens/Misc/3D Models/RestoredModels/OG/temple.glb` |
| **Temple Base** | [Temple_Base.png](03_Historic_Reconstructions/Temple_Base.png) | Archaeological monument foundation plinth | `Assets/_LangkaLens/Misc/3D Models/RestoredModels/OG/base.glb` |
| **Sample Structure** | [Sample_Structure.png](03_Historic_Reconstructions/Sample_Structure.png) | Architectural prototype reconstruction | `Assets/_LangkaLens/Misc/3D Models/RestoredModels/OG/sample.glb` |

---

## 4. Museum Demo Exhibits (`04_Museum_Demo_Exhibits/`)

Secondary and demo museum exhibits featured in the VR Museum gallery.

| Exhibit | Image Link | Type / Subject | Source File |
| :--- | :--- | :--- | :--- |
| **Demo 1** | [Demo1_Glazed_Pottery.png](04_Museum_Demo_Exhibits/Demo1_Glazed_Pottery.png) | Glazed two-tone ceramic pottery jar with handles | `Assets/_LangkaLens/Museum/3D Models/Demo/Demo1.glb` |
| **Demo 2** | [Demo2_Ancient_Vessel.png](04_Museum_Demo_Exhibits/Demo2_Ancient_Vessel.png) | Ancient clay earthenware vessel | `Assets/_LangkaLens/Museum/3D Models/Demo/Demo2.glb` |
| **Demo 3** | [Demo3_Terracotta_Pottery.png](04_Museum_Demo_Exhibits/Demo3_Terracotta_Pottery.png) | Textured terracotta storage pottery | `Assets/_LangkaLens/Museum/3D Models/Demo/Demo3.glb` |
| **Demo 4** | [Demo4_Sculptural_Artifact.png](04_Museum_Demo_Exhibits/Demo4_Sculptural_Artifact.png) | High-polygon sculptural exhibition asset | `Assets/_LangkaLens/Misc/3D Models/Demo/Demo4.glb` |
| **Demo 5** | [Demo5_Relic_Ornament.png](04_Museum_Demo_Exhibits/Demo5_Relic_Ornament.png) | Ancient relic ornament | `Assets/_LangkaLens/Museum/3D Models/Demo/Demo5.glb` |

---

## 5. Interactive 3D Props & Navigation Icons (`05_Interactive_Props_And_Icons/`)

Custom 3D user-interface elements, interaction controls, and museum furniture.

| Prop / Icon | Image Link | Usage in Experience | Source File |
| :--- | :--- | :--- | :--- |
| **Map Pointer** | [Map_Pointer_3D.png](05_Interactive_Props_And_Icons/Map_Pointer_3D.png) | 3D waypoint marker on the interactive site map | `Assets/_LangkaLens/Misc/3D Models/Icons/map_pointer_3d_icon.glb` |
| **Globe with Meridians** | [Globe_Meridians_3D.png](05_Interactive_Props_And_Icons/Globe_Meridians_3D.png) | Interactive VR Tour portal icon | `Assets/_LangkaLens/Misc/3D Models/Prefabs/globe_with_meridians_3d_icon.glb` |
| **Globe Sphere** | [Globe_Sphere_3D.png](05_Interactive_Props_And_Icons/Globe_Sphere_3D.png) | Spherical world projection model | `Assets/_LangkaLens/Tour/3D Models/Prefabs/globe.obj` |
| **Exclamation Mark** | [Exclamation_Mark_3D.png](05_Interactive_Props_And_Icons/Exclamation_Mark_3D.png) | Attention/hotspot prompt indicator | `Assets/_LangkaLens/Museum/3D Models/Icons/exclamation_mark_3d_icon.glb` |
| **Switch Lever** | [Switch_Lever_3D.png](05_Interactive_Props_And_Icons/Switch_Lever_3D.png) | VR interactive toggle lever | `Assets/_LangkaLens/Museum/3D Models/Icons/Switch.glb` |
| **Tour Arrow** | [Tour_Navigation_Arrow.png](05_Interactive_Props_And_Icons/Tour_Navigation_Arrow.png) | 360 panorama teleportation navigation arrow | `Assets/_LangkaLens/Tour/3D Models/Prefabs/Vtour/Arrow_prefab.prefab` |
| **Display Station** | [Artifact_Display_Station.png](05_Interactive_Props_And_Icons/Artifact_Display_Station.png) | Carved wooden museum artifact plinth/pedestal | `Assets/_LangkaLens/Museum/3D Models/Prefabs/Vmuseum/Artifact_Display_Station.prefab` |

---

## 6. Photogrammetry Architectural Assets (`06_Photogrammetry_Assets/`)

LOD photogrammetric scans configured with URP Lit materials.

| Asset ID | Image Link | Material Description | Source Prefab |
| :--- | :--- | :--- | :--- |
| **1050569** | [Parquet_Floor_1050569.png](06_Photogrammetry_Assets/Parquet_Floor_1050569.png) | Polished hardwood parquet flooring | `Assets/MetaAssets/Prefabs/1050569/1050569_M.prefab` |
| **1384373** | [Photogrammetry_1384373.png](06_Photogrammetry_Assets/Photogrammetry_1384373.png) | Environmental structural photogrammetry surface | `Assets/MetaAssets/Prefabs/1384373/1384373_M.prefab` |
| **1386201** | [Photogrammetry_1386201.png](06_Photogrammetry_Assets/Photogrammetry_1386201.png) | Textured architectural fragment scan | `Assets/MetaAssets/Prefabs/1386201/1386201_M.prefab` |
| **1431899** | [Photogrammetry_1431899.png](06_Photogrammetry_Assets/Photogrammetry_1431899.png) | Cultural prop scan | `Assets/MetaAssets/Prefabs/1431899/1431899_S.prefab` |
| **1573084** | [Photogrammetry_1573084.png](06_Photogrammetry_Assets/Photogrammetry_1573084.png) | Traditional woven bamboo mat flooring | `Assets/MetaAssets/Prefabs/1573084/1573084_M.prefab` |
| **1640840** | [Photogrammetry_1640840.png](06_Photogrammetry_Assets/Photogrammetry_1640840.png) | Cultural exhibition scan | `Assets/MetaAssets/Prefabs/1640840/1640840_M.prefab` |
| **791394** | [Photogrammetry_791394.png](06_Photogrammetry_Assets/Photogrammetry_791394.png) | Weathered architectural surface scan | `Assets/MetaAssets/Prefabs/791394/791394_M.prefab` |
