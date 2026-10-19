"""LangkaLens Dedicated Screenshots Lookbook & In-Game Visual Experience Gallery.

Compiles an illustrated Microsoft Word (.docx) document and high-fidelity PDF
dedicated exclusively to the project's in-game VR screenshots, Meta Quest simulator captures,
Unity development environment inspections, localized user guide walkthroughs, and code architecture figures.
"""

import os
import sys
import io
from datetime import datetime
from PIL import Image

import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import parse_xml
from docx.oxml.ns import nsdecls

sys.stdout.reconfigure(encoding='utf-8')

# Color Palette
HEX_PRIMARY_NAVY = "1F497D"
HEX_SECONDARY_NAVY = "2B579A"
HEX_LIGHT_ROW = "F8F9FA"
HEX_CARD_BG = "FFFFFF"
HEX_BORDER = "E1E4E8"
HEX_ACCENT_GOLD = "C59B27"

COLOR_PRIMARY = RGBColor(0x1F, 0x49, 0x7D)
COLOR_SECONDARY = RGBColor(0x2B, 0x57, 0x9A)
COLOR_TEXT_DARK = RGBColor(0x24, 0x29, 0x2F)
COLOR_MUTED = RGBColor(0x58, 0x60, 0x69)
COLOR_WHITE = RGBColor(0xFF, 0xFF, 0xFF)


def set_cell_background(cell, fill_hex: str):
    tcPr = cell._element.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex.strip("#")}"/>')
    tcPr.append(shd)


def set_cell_margins(cell, top=80, bottom=80, left=100, right=100):
    tcPr = cell._element.get_or_add_tcPr()
    tcMar = parse_xml(
        f'<w:tcMar {nsdecls("w")}>'
        f'<w:top w:w="{top}" w:type="dxa"/>'
        f'<w:bottom w:w="{bottom}" w:type="dxa"/>'
        f'<w:left w:w="{left}" w:type="dxa"/>'
        f'<w:right w:w="{right}" w:type="dxa"/>'
        f'</w:tcMar>'
    )
    tcPr.append(tcMar)


def set_cell_borders(cell, top="E1E4E8", bottom="E1E4E8", left="E1E4E8", right="E1E4E8", sz="4"):
    tcPr = cell._element.get_or_add_tcPr()
    top_attr = f'w:val="single" w:sz="{sz}" w:space="0" w:color="{top}"' if top != "none" else 'w:val="none"'
    bottom_attr = f'w:val="single" w:sz="{sz}" w:space="0" w:color="{bottom}"' if bottom != "none" else 'w:val="none"'
    left_attr = f'w:val="single" w:sz="{sz}" w:space="0" w:color="{left}"' if left != "none" else 'w:val="none"'
    right_attr = f'w:val="single" w:sz="{sz}" w:space="0" w:color="{right}"' if right != "none" else 'w:val="none"'

    borders = parse_xml(
        f'<w:tcBorders {nsdecls("w")}>'
        f'<w:top {top_attr}/>'
        f'<w:left {left_attr}/>'
        f'<w:bottom {bottom_attr}/>'
        f'<w:right {right_attr}/>'
        f'</w:tcBorders>'
    )
    tcPr.append(borders)


def make_clean_thumbnail_buffer(image_path: str, max_dim=(1920, 1080)):
    """Generate in-memory thumbnail buffer, stripping corrupt EXIF and handling PNG/JPEG."""
    try:
        with Image.open(image_path) as im:
            im_copy = im.copy()
            im_copy.thumbnail(max_dim, Image.Resampling.LANCZOS)
            buf = io.BytesIO()

            if im_copy.mode in ('RGBA', 'LA') or (im_copy.mode == 'P' and 'transparency' in im_copy.info):
                im_copy.convert('RGBA').save(buf, format='PNG', optimize=True)
            else:
                im_copy.convert('RGB').save(buf, format='JPEG', quality=88, optimize=True)

            buf.seek(0)
            return buf
    except Exception as e:
        print(f"Warning: Failed to thumbnail {image_path}: {e}")
        return None


def add_screenshot_fullwidth_plate(doc, abs_path, title, subsystem, interaction_modality, description, width=Inches(6.8)):
    """Render a showcase full-width screenshot card."""
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl.autofit = False
    tbl.columns[0].width = Inches(7.0)

    cell = tbl.cell(0, 0)
    set_cell_background(cell, HEX_CARD_BG)
    set_cell_margins(cell, top=100, bottom=100, left=120, right=120)
    set_cell_borders(cell, top=HEX_BORDER, bottom=HEX_BORDER, left=HEX_PRIMARY_NAVY, right=HEX_BORDER, sz="6")

    # Title header
    p_title = cell.paragraphs[0]
    p_title.paragraph_format.space_before = Pt(2)
    p_title.paragraph_format.space_after = Pt(4)
    r_t = p_title.add_run(title)
    r_t.font.name = "Segoe UI"
    r_t.font.size = Pt(11)
    r_t.bold = True
    r_t.font.color.rgb = COLOR_PRIMARY

    # Image
    p_img = cell.add_paragraph()
    p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_img.paragraph_format.space_before = Pt(4)
    p_img.paragraph_format.space_after = Pt(6)

    buf = make_clean_thumbnail_buffer(abs_path, max_dim=(1920, 1080))
    if buf:
        p_img.add_run().add_picture(buf, width=width)
    else:
        p_img.add_run("[Screenshot Image Unavailable]").font.italic = True

    # Metadata & Description
    size_bytes = os.path.getsize(abs_path)
    size_str = f"{size_bytes / (1024 * 1024):.2f} MB" if size_bytes >= 1024 * 1024 else f"{size_bytes / 1024:.1f} KB"
    w, h = 0, 0
    try:
        with Image.open(abs_path) as im:
            w, h = im.size
    except Exception:
        pass

    p_meta = cell.add_paragraph()
    p_meta.paragraph_format.space_before = Pt(2)
    p_meta.paragraph_format.space_after = Pt(2)
    p_meta.paragraph_format.line_spacing = 1.15

    r_sub = p_meta.add_run(f"Subsystem: {subsystem}  |  Modality: {interaction_modality}\n")
    r_sub.font.size = Pt(8.5)
    r_sub.bold = True
    r_sub.font.color.rgb = COLOR_SECONDARY

    r_desc = p_meta.add_run(description + "\n")
    r_desc.font.size = Pt(8.5)
    r_desc.font.color.rgb = COLOR_TEXT_DARK

    rel_p = os.path.relpath(abs_path, '.').replace(os.sep, '/')
    r_spec = p_meta.add_run(f"Native Resolution: {w}x{h} | File Size: {size_str} | Source: {rel_p}")
    r_spec.font.size = Pt(7.5)
    r_spec.font.color.rgb = COLOR_MUTED

    p_div = doc.add_paragraph()
    p_div.paragraph_format.space_before = Pt(2)
    p_div.paragraph_format.space_after = Pt(6)


def add_screenshot_pair_card(cell, abs_path, title, label_badge="", badge_color="BLUE", width=Inches(3.2)):
    """Render a single screenshot card inside a 2-column table cell."""
    set_cell_background(cell, HEX_CARD_BG)
    set_cell_margins(cell, top=60, bottom=60, left=70, right=70)
    set_cell_borders(cell, top=HEX_BORDER, bottom=HEX_BORDER, left=HEX_BORDER, right=HEX_BORDER, sz="4")

    cp = cell.paragraphs[0]
    cp.alignment = WD_ALIGN_PARAGRAPH.CENTER
    cp.paragraph_format.space_before = Pt(2)
    cp.paragraph_format.space_after = Pt(4)

    buf = make_clean_thumbnail_buffer(abs_path, max_dim=(1200, 800))
    if buf:
        cp.add_run().add_picture(buf, width=width)
    else:
        cp.add_run("[Screenshot Unavailable]").font.italic = True

    p_meta = cell.add_paragraph()
    p_meta.paragraph_format.space_before = Pt(2)
    p_meta.paragraph_format.space_after = Pt(2)
    p_meta.paragraph_format.line_spacing = 1.1

    if label_badge:
        r_b = p_meta.add_run(f"[{label_badge}] ")
        r_b.font.size = Pt(8)
        r_b.bold = True
        if badge_color == "RED":
            r_b.font.color.rgb = RGBColor(0xA8, 0x28, 0x28)
        elif badge_color == "GREEN":
            r_b.font.color.rgb = RGBColor(0x1E, 0x7E, 0x34)
        else:
            r_b.font.color.rgb = COLOR_PRIMARY

    r_t = p_meta.add_run(title + "\n")
    r_t.font.size = Pt(8.5)
    r_t.bold = True
    r_t.font.color.rgb = COLOR_PRIMARY

    fname = os.path.basename(abs_path)
    r_fn = p_meta.add_run(fname)
    r_fn.font.name = "Consolas"
    r_fn.font.size = Pt(7.5)
    r_fn.font.color.rgb = COLOR_MUTED


def build_screenshots_lookbook(output_path="Docs/LangkaLens_Screenshots_Lookbook.docx"):
    """Assemble complete high-standard dedicated Screenshots Lookbook."""
    doc = docx.Document()
    sec = doc.sections[0]
    sec.top_margin = Inches(0.75)
    sec.bottom_margin = Inches(0.75)
    sec.left_margin = Inches(0.75)
    sec.right_margin = Inches(0.75)
    sec.different_first_page_header_footer = True

    # Header / Footer
    header = sec.header
    hp = header.paragraphs[0]
    hp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    hr = hp.add_run("LangkaLens | In-Game VR Experience & Screenshots Lookbook")
    hr.font.name = "Segoe UI"
    hr.font.size = Pt(8.5)
    hr.font.color.rgb = RGBColor(0x88, 0x88, 0x88)

    footer = sec.footer
    fp = footer.paragraphs[0]
    fp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    fr1 = fp.add_run("LangkaLens VR/MR Experience — In-Game Screenshots Gallery  |  Page ")
    fr1.font.name = "Segoe UI"
    fr1.font.size = Pt(8.5)
    fr1.font.color.rgb = RGBColor(0x88, 0x88, 0x88)

    # Styles
    normal_style = doc.styles['Normal']
    normal_style.font.name = 'Segoe UI'
    normal_style.font.size = Pt(9.5)
    normal_style.font.color.rgb = COLOR_TEXT_DARK
    normal_style.paragraph_format.line_spacing = 1.15
    normal_style.paragraph_format.space_after = Pt(4)

    # ==========================================
    # COVER / TITLE BLOCK
    # ==========================================
    p_inst = doc.add_paragraph()
    p_inst.paragraph_format.space_before = Pt(8)
    p_inst.paragraph_format.space_after = Pt(2)
    r_inst = p_inst.add_run("PRINCE OF SONGKLA UNIVERSITY  |  FACULTY OF COMMUNICATION SCIENCES  |  TSRI")
    r_inst.font.name = "Segoe UI"
    r_inst.font.size = Pt(9.5)
    r_inst.bold = True
    r_inst.font.color.rgb = COLOR_SECONDARY

    p_title = doc.add_paragraph()
    p_title.paragraph_format.space_before = Pt(2)
    p_title.paragraph_format.space_after = Pt(2)
    r_title = p_title.add_run("LangkaLens (ลังกาเลนส์)")
    r_title.font.name = "Segoe UI"
    r_title.font.size = Pt(28)
    r_title.bold = True
    r_title.font.color.rgb = COLOR_PRIMARY

    p_sub = doc.add_paragraph()
    p_sub.paragraph_format.space_before = Pt(0)
    p_sub.paragraph_format.space_after = Pt(10)
    r_sub = p_sub.add_run("In-Game Screenshots & XR Experience Showcase Lookbook")
    r_sub.font.name = "Segoe UI"
    r_sub.font.size = Pt(15)
    r_sub.font.color.rgb = COLOR_MUTED

    meta_tbl = doc.add_table(rows=1, cols=4)
    meta_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    meta_tbl.autofit = False
    col_widths = [Inches(1.75), Inches(1.75), Inches(1.75), Inches(1.75)]
    for row in meta_tbl.rows:
        for idx, width in enumerate(col_widths):
            row.cells[idx].width = width

    meta_items = [
        ("IN-GAME SCREENSHOTS", "105 Curated Plates"),
        ("EXPERIENCE SUBSYSTEMS", "4 Core VR/MR Modes"),
        ("HARDWARE TARGET", "Meta Quest 3 & 3S"),
        ("PUBLICATION DATE", datetime.now().strftime("%B %Y")),
    ]
    for idx, (label, val) in enumerate(meta_items):
        cell = meta_tbl.cell(0, idx)
        set_cell_background(cell, HEX_LIGHT_ROW)
        set_cell_margins(cell, top=80, bottom=80, left=80, right=80)
        set_cell_borders(cell, top=HEX_PRIMARY_NAVY, bottom=HEX_PRIMARY_NAVY, sz="12")
        cp = cell.paragraphs[0]
        cp.paragraph_format.space_after = Pt(1)
        r_lbl = cp.add_run(label + "\n")
        r_lbl.font.size = Pt(7.5)
        r_lbl.bold = True
        r_lbl.font.color.rgb = COLOR_SECONDARY
        r_val = cp.add_run(val)
        r_val.font.size = Pt(9.5)
        r_val.bold = True
        r_val.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph().paragraph_format.space_after = Pt(12)

    # ==========================================
    # SECTION 1: IN-GAME SIMULATOR & PLAYTESTING SCREENSHOTS
    # ==========================================
    h1 = doc.add_heading("1. In-Game VR Experience & Simulator Playtesting Screenshots", level=1)
    h1.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph(
        "High-resolution 1888x1136 widescreen captures taken directly during runtime playtesting sessions "
        "and Quest 3 headset simulation. These screenshots demonstrate the live player viewpoint across "
        "the main menu, virtual museum gallery, 360-degree tour viewer, and Gemini AI cultural assistant."
    )

    playtesting_dir = "Docs/Screenshots/Playtesting"
    playtesting_meta = [
        (
            "main-menu-simulator.png",
            "Figure 1.1: VR Main Menu & Experience Selector Portal",
            "Main Menu Core",
            "Raycast Pointer & Poke Selection",
            "The player is positioned in the tranquil 3D environment facing the interactive curved menu. "
            "Large mode selector cards provide one-touch entry into the Virtual Museum, 360 VR Tour, and Mixed Reality Alignment."
        ),
        (
            "language-selection-simulator.png",
            "Figure 1.2: Multilingual Language Selection Dialog",
            "Localization Subsystem",
            "Direct Touch / Ray Interaction",
            "The localized language selector dialog allows users to toggle seamlessly between Thai (ภาษาไทย) and English "
            "across all 3D UI canvases, audio narration, and text descriptions via the Unity Localization package."
        ),
        (
            "museum-gallery-simulator.png",
            "Figure 1.3: Virtual Museum Exhibition Gallery",
            "Virtual Museum",
            "Smooth Locomotion & Snap Turn",
            "Overview of the museum interior featuring polished hardwood parquet flooring, studio-lit pedestals, "
            "and glass display stations housing the 11 excavated and restored Yarang archaeological artifacts."
        ),
        (
            "museum-inspection-simulator.png",
            "Figure 1.4: Interactive 3D Artifact Two-Handed Inspection",
            "Virtual Museum / Inspection",
            "Meta Interaction SDK Two-Handed Grab & Free Rotate",
            "The player has picked up the carved stone pillar exhibit. Using natural controller or hand-tracking gestures, "
            "the user can rotate the artifact freely in 6-DOF, inspect detailed carvings, scale the model, or swap between weathered and restored states."
        ),
        (
            "tour-area-menu-simulator.png",
            "Figure 1.5: 360 VR Tour Destination & Sector Selection Tablet",
            "Virtual Tour",
            "Interactive World-Space UI Poke",
            "The interactive site map tablet displays archaeological monuments across Yarang, enabling direct teleportation "
            "to Ban Ja-Lae Site 8, Ban Wat Site 9, Ban Ja-Lae Site 3, or Ban Ja-Lae Site 2."
        ),
        (
            "tour-panorama-simulator.png",
            "Figure 1.6: 360° Equirectangular Panoramic Navigation with Teleport Arrows",
            "Virtual Tour",
            "Raycast Navigation & Gaze Targeting",
            "Player view inside an ultra-high-resolution 360-degree panorama at Ban Ja-Lae Site 8. "
            "Dynamic 3D directional arrows indicate connected viewpoints, enabling intuitive ground-level traversal."
        ),
        (
            "tour-reconstruction-simulator.png",
            "Figure 1.7: Architectural Monument Reconstruction Overlay",
            "Virtual Tour & Mixed Reality",
            "Spatial Alignment & Inspection Controls",
            "Monumental reconstructed brick stupa rendered directly atop the ancient archaeological ruins, "
            "allowing visitors to visualize how the Dvaravati sanctuary appeared centuries ago."
        ),
        (
            "ai-guide-simulator.png",
            "Figure 1.8: AI Cultural Assistant & Voice Interaction Panel",
            "AI Cultural Guide",
            "Microphone Speech-to-Text & Gemini LLM Chat",
            "The player interacts with the Gemini AI guide, asking questions about the Dvaravati civilization. "
            "The assistant returns historical context, excavation facts, and archaeological analysis in real time."
        ),
        (
            "user-guide-simulator.png",
            "Figure 1.9: Interactive Wrist Tablet & User Onboarding Manual",
            "Onboarding & HUD",
            "Wrist-Mounted Tablet UI",
            "A convenient wrist-mounted tablet interface provides step-by-step controller button legends, "
            "interaction diagrams, and instant access to system settings."
        ),
    ]

    for fname, title, subsys, modality, desc in playtesting_meta:
        fp = os.path.join(playtesting_dir, fname)
        if os.path.exists(fp):
            add_screenshot_fullwidth_plate(doc, fp, title, subsys, modality, desc, width=Inches(6.8))

    # ==========================================
    # SECTION 2: UNITY EDITOR & XR DEVELOPMENT SCREENSHOTS
    # ==========================================
    doc.add_page_break()
    h2 = doc.add_heading("2. Unity Editor & XR Development Environment Screenshots", level=1)
    h2.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph(
        "Technical screenshots capturing the Unity 6 / Universal Render Pipeline (URP) authoring environment, "
        "scene hierarchy, Meta XR Core SDK configurations, and lighting setups configured for Meta Quest standalone deployment."
    )

    dev_dir = "Docs/Screenshots/Development"
    dev_meta = [
        (
            "unity-main-menu-editor.png",
            "Figure 2.1: Main Menu Scene Architecture in Unity Editor",
            "Unity Editor Scene Setup",
            "URP Lighting & Canvas Configuration",
            "Shows the Main_Menu scene open in Unity Editor. The Scene view reveals the curved world-space UI canvas, "
            "the ambient environment skybox, camera rig positioning, and the Addressable asset bindings."
        ),
        (
            "unity-museum-editor.png",
            "Figure 2.2: VR Museum Gallery Scene & Pedestal Placement",
            "Unity Editor Scene Setup",
            "Occlusion Culling & Interaction Rigs",
            "Captures the Museum scene layout with 11 individual artifact pedestals, lighting probes, "
            "and Meta Interaction SDK grab transformers (OneGrabFreeRotateTransformer) attached to each exhibit prefab."
        ),
        (
            "unity-tour-editor.png",
            "Figure 2.3: 360 Tour Graph Node & Directional Arrow Tooling",
            "Unity Editor Scene Setup",
            "Addressables & TourNode ScriptableObjects",
            "Shows the 360 Tour scene authoring workflow with TourNode ScriptableObject data, reciprocal navigation arrow placement, "
            "and Addressables photo asset links."
        ),
        (
            "unity-mr-alignment-editor.png",
            "Figure 2.4: Mixed Reality Spatial Passthrough & Alignment Setup",
            "Unity Editor Scene Setup",
            "Meta XR Passthrough & Spatial Anchors",
            "Displays the MR alignment test scene with passthrough camera background, bounding box positioning gizmos, "
            "and real-world monument foundation plinth alignment rigs."
        ),
    ]

    for fname, title, subsys, modality, desc in dev_meta:
        fp = os.path.join(dev_dir, fname)
        if os.path.exists(fp):
            add_screenshot_fullwidth_plate(doc, fp, title, subsys, modality, desc, width=Inches(6.8))

    # ==========================================
    # SECTION 3: BILINGUAL USER GUIDE WALKTHROUGH SCREENSHOTS
    # ==========================================
    doc.add_page_break()
    h3 = doc.add_heading("3. Localized In-Game User Guide Screens (Side-by-Side EN / TH)", level=1)
    h3.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph(
        "High-resolution 2600x1800 localized tutorial sheets embedded in the in-game tablet. "
        "Each screen is presented side-by-side: English version (Left) and Thai version (Right) "
        "covering all five experience modules."
    )

    guide_dir = "Docs/Screenshots/UserGuides"
    guide_pairs = [
        # Getting Started
        ("GettingStarted_EN_01.png", "GettingStarted_TH_01.png", "Getting Started 1: Point and Select Controls"),
        ("GettingStarted_EN_02.png", "GettingStarted_TH_02.png", "Getting Started 2: Experience Selection"),
        ("GettingStarted_EN_03.png", "GettingStarted_TH_03.png", "Getting Started 3: Show/Hide Wrist Tablet"),
        ("GettingStarted_EN_04.png", "GettingStarted_TH_04.png", "Getting Started 4: Language Switcher"),
        # VR Museum
        ("VRMuseum_EN_01.png", "VRMuseum_TH_01.png", "VR Museum 1: Move Around Gallery"),
        ("VRMuseum_EN_02.png", "VRMuseum_TH_02.png", "VR Museum 2: Select Exhibit"),
        ("VRMuseum_EN_03.png", "VRMuseum_TH_03.png", "VR Museum 3: Grip & Rotate Artifact"),
        ("VRMuseum_EN_04.png", "VRMuseum_TH_04.png", "VR Museum 4: Rotate & Reset Controls"),
        ("VRMuseum_EN_05.png", "VRMuseum_TH_05.png", "VR Museum 5: Enlarge or Reduce Artifact"),
        ("VRMuseum_EN_06.png", "VRMuseum_TH_06.png", "VR Museum 6: Compare Weathered vs Restored"),
        ("VRMuseum_EN_07.png", "VRMuseum_TH_07.png", "VR Museum 7: Exit Inspection Mode"),
        # VR Tour
        ("VRTour_EN_01.png", "VRTour_TH_01.png", "VR Tour 1: Select Tour Location"),
        ("VRTour_EN_02.png", "VRTour_TH_02.png", "VR Tour 2: Aim at Navigation Arrow"),
        ("VRTour_EN_03.png", "VRTour_TH_03.png", "VR Tour 3: Release Trigger to Travel"),
        ("VRTour_EN_04.png", "VRTour_TH_04.png", "VR Tour 4: Zoom In/Out on Panorama"),
        ("VRTour_EN_05.png", "VRTour_TH_05.png", "VR Tour 5: Inspect Reconstructed Monument"),
        ("VRTour_EN_06.png", "VRTour_TH_06.png", "VR Tour 6: Move or Turn Monument"),
        ("VRTour_EN_07.png", "VRTour_TH_07.png", "VR Tour 7: Rotate with Thumbstick"),
        ("VRTour_EN_08.png", "VRTour_TH_08.png", "VR Tour 8: Reset Monument Transform"),
        ("VRTour_EN_09.png", "VRTour_TH_09.png", "VR Tour 9: Scale Architectural Model"),
        # Mixed Reality
        ("MixedReality_EN_01.png", "MixedReality_TH_01.png", "Mixed Reality 1: Position Outline at Entrance"),
        ("MixedReality_EN_02.png", "MixedReality_TH_02.png", "Mixed Reality 2: Fine-Tune Spatial Alignment"),
        ("MixedReality_EN_03.png", "MixedReality_TH_03.png", "Mixed Reality 3: Release Grip & Lock Model"),
        ("MixedReality_EN_04.png", "MixedReality_TH_04.png", "Mixed Reality 4: Toggle Restoration Visibility"),
        ("MixedReality_EN_05.png", "MixedReality_TH_05.png", "Mixed Reality 5: View Historical Site Info"),
        ("MixedReality_EN_06.png", "MixedReality_TH_06.png", "Mixed Reality 6: Realign Spatial Anchor"),
        # AI Cultural Guide
        ("AIGuide_EN_01.png", "AIGuide_TH_01.png", "AI Guide 1: Type Question via Virtual Keyboard"),
        ("AIGuide_EN_02.png", "AIGuide_TH_02.png", "AI Guide 2: Ask Question by Voice via Mic"),
    ]

    for en_fn, th_fn, pair_title in guide_pairs:
        en_fp = os.path.join(guide_dir, en_fn)
        th_fp = os.path.join(guide_dir, th_fn)

        if os.path.exists(en_fp) and os.path.exists(th_fp):
            h_pair = doc.add_heading(pair_title, level=2)
            h_pair.style.font.color.rgb = COLOR_SECONDARY

            tbl = doc.add_table(rows=1, cols=2)
            tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
            tbl.autofit = False
            tbl.columns[0].width = Inches(3.45)
            tbl.columns[1].width = Inches(3.45)

            add_screenshot_pair_card(tbl.cell(0, 0), en_fp, "English Interface (EN)", label_badge="EN", badge_color="BLUE", width=Inches(3.2))
            add_screenshot_pair_card(tbl.cell(0, 1), th_fp, "Thai Interface (ภาษาไทย)", label_badge="TH", badge_color="GREEN", width=Inches(3.2))

            p_div = doc.add_paragraph()
            p_div.paragraph_format.space_before = Pt(2)
            p_div.paragraph_format.space_after = Pt(4)

    # ==========================================
    # SECTION 4: STEP-BY-STEP CONTROLLER WORKFLOW PICTURES (01 TO 28)
    # ==========================================
    doc.add_page_break()
    h4 = doc.add_heading("4. Step-by-Step Controller Interaction Workflows (Steps 01 to 28)", level=1)
    h4.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph(
        "Complete visual photographic sequence of the 28 controller interaction workflow pictures embedded "
        "inside the VR tablet to teach physical button grips, thumbstick rotation, and trigger gestures."
    )

    wf_dir = "Docs/User_Guide_Pictures"
    wf_files = sorted([f for f in os.listdir(wf_dir) if f.lower().endswith(('.jpg', '.png'))])

    for i in range(0, len(wf_files), 2):
        row_files = wf_files[i:i+2]
        tbl = doc.add_table(rows=1, cols=2)
        tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
        tbl.autofit = False
        tbl.columns[0].width = Inches(3.45)
        tbl.columns[1].width = Inches(3.45)

        fp1 = os.path.join(wf_dir, row_files[0])
        clean_title1 = row_files[0].replace('.jpg', '').replace('_', ' ')
        add_screenshot_pair_card(tbl.cell(0, 0), fp1, clean_title1, label_badge="STEP", badge_color="BLUE", width=Inches(3.2))

        if len(row_files) > 1:
            fp2 = os.path.join(wf_dir, row_files[1])
            clean_title2 = row_files[1].replace('.jpg', '').replace('_', ' ')
            add_screenshot_pair_card(tbl.cell(0, 1), fp2, clean_title2, label_badge="STEP", badge_color="BLUE", width=Inches(3.2))
        else:
            set_cell_background(tbl.cell(0, 1), HEX_LIGHT_ROW)

        p_div = doc.add_paragraph()
        p_div.paragraph_format.space_before = Pt(1)
        p_div.paragraph_format.space_after = Pt(3)

    # ==========================================
    # SECTION 5: C# CORE CODE ARCHITECTURE SCREENSHOTS
    # ==========================================
    doc.add_page_break()
    h5 = doc.add_heading("5. C# Code Implementation Architecture Figures", level=1)
    h5.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph(
        "Syntax-highlighted code figures capturing the core subsystem implementations authored for the senior project report, "
        "including scene loading, tour panorama streaming, inspection transformers, and Gemini REST API integration."
    )

    code_dir = "Docs/Report/figures/code"
    code_meta = [
        ("figure-4-1-main-menu-scene-loading.png", "Figure 5.1: Main Menu Asynchronous Scene Loading"),
        ("figure-4-2-tour-panorama-loading.png", "Figure 5.2: 360 Tour Equirectangular Panorama Addressable Streaming"),
        ("figure-4-3-tour-reconstruction-loading.png", "Figure 5.3: Architectural Monument Reconstruction Instantiation"),
        ("figure-4-4-museum-inspection-controls.png", "Figure 5.4: Museum 6-DOF Grab & Rotation Transformer Logic"),
        ("figure-4-5-mr-session-anchor.png", "Figure 5.5: Mixed Reality Passthrough Session & Spatial Anchor"),
        ("figure-4-6-mr-model-display.png", "Figure 5.6: Mixed Reality Spatial Alignment Plinth Attachment"),
        ("figure-4-7-gemini-chat-request.png", "Figure 5.7: Gemini LLM REST API Client & Request Pipeline"),
        ("figure-4-8-speech-transcript-input.png", "Figure 5.8: Speech-to-Text Microphone Input Handler"),
    ]

    for fname, title in code_meta:
        fp = os.path.join(code_dir, fname)
        if os.path.exists(fp):
            add_screenshot_fullwidth_plate(
                doc, fp, title,
                subsystem="Core C# Architecture",
                interaction_modality="Software Engineering / System Logic",
                description="Production code excerpt from the LangkaLens Unity codebase illustrating core system implementation.",
                width=Inches(6.8)
            )

    # Save
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    doc.save(output_path)
    file_size_mb = os.path.getsize(output_path) / (1024 * 1024)
    print(f"\n[OK] Successfully compiled Screenshots Lookbook: {output_path}")
    print(f"     File Size: {file_size_mb:.2f} MB")


if __name__ == "__main__":
    print("=" * 70)
    print("  LangkaLens Dedicated Screenshots Lookbook Generator")
    print("=" * 70)

    target_docx = "Docs/LangkaLens_Screenshots_Lookbook.docx"
    target_pdf = "Docs/LangkaLens_Screenshots_Lookbook.pdf"

    build_screenshots_lookbook(target_docx)

    # Convert to PDF
    print("\nTriggering high-fidelity PDF export via Microsoft Word COM...")
    try:
        from pathlib import Path
        sys.path.append(os.path.abspath(".agents/skills/docx/scripts"))
        from export_pdf import export_to_pdf
        export_to_pdf(target_docx, target_pdf)
    except Exception as e:
        print(f"PDF export notice: {e}")
