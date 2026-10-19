import os
import sys
from pathlib import Path
import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import parse_xml, OxmlElement
from docx.oxml.ns import nsdecls, qn

def set_cell_background(cell, fill_hex: str):
    tcPr = cell._element.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex.strip("#")}"/>')
    tcPr.append(shd)

def set_cell_margins(cell, top=120, bottom=120, left=160, right=160):
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

def set_cell_borders(cell, top="none", bottom="none", left="none", right="none", 
                     color="D0D5DD", sz="4"):
    tcPr = cell._element.get_or_add_tcPr()
    tcBorders = parse_xml(
        f'<w:tcBorders {nsdecls("w")}>'
        f'<w:top w:val="{top}" w:sz="{sz}" w:space="0" w:color="{color}"/>'
        f'<w:left w:val="{left}" w:sz="{sz}" w:space="0" w:color="{color}"/>'
        f'<w:bottom w:val="{bottom}" w:sz="{sz}" w:space="0" w:color="{color}"/>'
        f'<w:right w:val="{right}" w:sz="{sz}" w:space="0" w:color="{color}"/>'
        f'</w:tcBorders>'
    )
    tcPr.append(tcBorders)

def add_header_footer(doc):
    for sec in doc.sections:
        # Header
        header = sec.header
        hp = header.paragraphs[0]
        hp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        hrun = hp.add_run("LangkaLens System Architecture | Prince of Songkla University")
        hrun.font.name = "Times New Roman"
        hrun.font.size = Pt(8.5)
        hrun.font.color.rgb = RGBColor(0x88, 0x88, 0x88)
        
        # Footer
        footer = sec.footer
        fp = footer.paragraphs[0]
        fp.alignment = WD_ALIGN_PARAGRAPH.CENTER
        frun = fp.add_run("Senior Project Technical Report — Bachelor of Science (CSM)")
        frun.font.name = "Times New Roman"
        frun.font.size = Pt(8.5)
        frun.font.color.rgb = RGBColor(0x88, 0x88, 0x88)

def format_run_font(run, font_name="Times New Roman", size_pt=11, bold=False, italic=False, color_rgb=(0x22, 0x22, 0x22)):
    run.font.name = font_name
    run.font.size = Pt(size_pt)
    run.bold = bold
    run.italic = italic
    run.font.color.rgb = RGBColor(*color_rgb)
    
    rPr = run._element.get_or_add_rPr()
    rFonts = parse_xml(
        f'<w:rFonts {nsdecls("w")} '
        f'w:ascii="{font_name}" '
        f'w:hAnsi="{font_name}" '
        f'w:cs="TH Sarabun New" '
        f'w:eastAsia="TH Sarabun New"/>'
    )
    rPr.append(rFonts)

def add_styled_paragraph(doc, text="", font_name="Times New Roman", size_pt=11, bold=False, italic=False, 
                         color_rgb=(0x22, 0x22, 0x22), align=WD_ALIGN_PARAGRAPH.LEFT, space_before=0, space_after=6, line_spacing=1.15):
    p = doc.add_paragraph()
    p.alignment = align
    p.paragraph_format.space_before = Pt(space_before)
    p.paragraph_format.space_after = Pt(space_after)
    p.paragraph_format.line_spacing = line_spacing
    if text:
        r = p.add_run(text)
        format_run_font(r, font_name, size_pt, bold, italic, color_rgb)
    return p

def add_heading_1(doc, text):
    p = doc.add_paragraph(style='Heading 1')
    p.paragraph_format.space_before = Pt(20)
    p.paragraph_format.space_after = Pt(6)
    p.paragraph_format.keep_with_next = True
    r = p.add_run(text)
    format_run_font(r, "Times New Roman", 16, bold=True, color_rgb=(0x1F, 0x49, 0x7D))
    return p

def add_heading_2(doc, text):
    p = doc.add_paragraph(style='Heading 2')
    p.paragraph_format.space_before = Pt(14)
    p.paragraph_format.space_after = Pt(4)
    p.paragraph_format.keep_with_next = True
    r = p.add_run(text)
    format_run_font(r, "Times New Roman", 13.5, bold=True, color_rgb=(0x2B, 0x57, 0x9A))
    return p

def add_heading_3(doc, text):
    p = doc.add_paragraph(style='Heading 3')
    p.paragraph_format.space_before = Pt(10)
    p.paragraph_format.space_after = Pt(3)
    p.paragraph_format.keep_with_next = True
    r = p.add_run(text)
    format_run_font(r, "Times New Roman", 12, bold=True, color_rgb=(0x1A, 0x2E, 0x40))
    return p

def add_heading_4(doc, text):
    p = doc.add_paragraph(style='Heading 4')
    p.paragraph_format.space_before = Pt(6)
    p.paragraph_format.space_after = Pt(2)
    p.paragraph_format.keep_with_next = True
    r = p.add_run(text)
    format_run_font(r, "Times New Roman", 11, bold=True, italic=True, color_rgb=(0x33, 0x33, 0x33))
    return p

def add_bullet_item(doc, bold_prefix, text):
    p = doc.add_paragraph(style='List Bullet')
    p.paragraph_format.space_before = Pt(1)
    p.paragraph_format.space_after = Pt(3)
    p.paragraph_format.line_spacing = 1.15
    if bold_prefix:
        r1 = p.add_run(bold_prefix)
        format_run_font(r1, "Times New Roman", 11, bold=True, color_rgb=(0x1A, 0x2E, 0x40))
    r2 = p.add_run(text)
    format_run_font(r2, "Times New Roman", 11, bold=False, color_rgb=(0x33, 0x33, 0x33))
    return p

def add_callout(doc, title, text, border_color="1F497D", bg_color="F4F6F9"):
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl.autofit = False
    tbl.columns[0].width = Inches(5.75)
    cell = tbl.cell(0, 0)
    set_cell_background(cell, bg_color)
    set_cell_margins(cell, top=100, bottom=100, left=160, right=140)
    set_cell_borders(cell, left="single", color=border_color, sz="24")
    
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(2)
    p.paragraph_format.space_after = Pt(2)
    p.paragraph_format.line_spacing = 1.15
    if title:
        r_title = p.add_run(f"[{title}] ")
        format_run_font(r_title, "Times New Roman", 10.5, bold=True, color_rgb=(0x1F, 0x49, 0x7D))
    r_text = p.add_run(text)
    format_run_font(r_text, "Times New Roman", 10.5, italic=True, color_rgb=(0x33, 0x33, 0x33))
    
    sp = doc.add_paragraph()
    sp.paragraph_format.space_before = Pt(0)
    sp.paragraph_format.space_after = Pt(4)

def add_image_figure(doc, img_path, caption, width_in=5.6):
    if not os.path.exists(img_path):
        print(f"Warning: Image not found: {img_path}")
        return
    p_img = doc.add_paragraph()
    p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_img.paragraph_format.space_before = Pt(8)
    p_img.paragraph_format.space_after = Pt(2)
    p_img.paragraph_format.keep_with_next = True
    r = p_img.add_run()
    r.add_picture(img_path, width=Inches(width_in))
    
    p_cap = doc.add_paragraph()
    p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_cap.paragraph_format.space_before = Pt(2)
    p_cap.paragraph_format.space_after = Pt(10)
    r_cap = p_cap.add_run(caption)
    format_run_font(r_cap, "Times New Roman", 9.5, italic=True, color_rgb=(0x55, 0x55, 0x55))

def add_table_data(doc, caption, headers, rows_data, col_widths=None):
    if caption:
        p_cap = doc.add_paragraph()
        p_cap.paragraph_format.space_before = Pt(10)
        p_cap.paragraph_format.space_after = Pt(3)
        p_cap.paragraph_format.keep_with_next = True
        r_cap = p_cap.add_run(caption)
        format_run_font(r_cap, "Times New Roman", 10.5, bold=True, color_rgb=(0x1F, 0x49, 0x7D))
        
    tbl = doc.add_table(rows=len(rows_data) + 1, cols=len(headers))
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl.autofit = False
    
    # Header Row
    hdr_cells = tbl.rows[0].cells
    for i, h_text in enumerate(headers):
        hdr_cells[i].text = ""
        set_cell_background(hdr_cells[i], "1F497D")
        set_cell_margins(hdr_cells[i], top=90, bottom=90, left=120, right=120)
        set_cell_borders(hdr_cells[i], top="single", bottom="single", color="163557", sz="8")
        p = hdr_cells[i].paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        p.paragraph_format.space_before = Pt(2)
        p.paragraph_format.space_after = Pt(2)
        r = p.add_run(h_text)
        format_run_font(r, "Times New Roman", 10, bold=True, color_rgb=(0xFF, 0xFF, 0xFF))
        
    # Data Rows
    for r_idx, r_data in enumerate(rows_data):
        row_cells = tbl.rows[r_idx + 1].cells
        bg_col = "F6F8FA" if r_idx % 2 == 1 else "FFFFFF"
        for c_idx, cell_value in enumerate(r_data):
            row_cells[c_idx].text = ""
            set_cell_background(row_cells[c_idx], bg_col)
            set_cell_margins(row_cells[c_idx], top=70, bottom=70, left=120, right=120)
            set_cell_borders(row_cells[c_idx], top="single", bottom="single", color="E1E4E8", sz="4")
            p = row_cells[c_idx].paragraphs[0]
            p.alignment = WD_ALIGN_PARAGRAPH.LEFT
            p.paragraph_format.space_before = Pt(2)
            p.paragraph_format.space_after = Pt(2)
            p.paragraph_format.line_spacing = 1.1
            r = p.add_run(str(cell_value))
            format_run_font(r, "Times New Roman", 9.5, color_rgb=(0x33, 0x33, 0x33))
            
    if col_widths and len(col_widths) == len(headers):
        for row in tbl.rows:
            for idx, w in enumerate(col_widths):
                row.cells[idx].width = Inches(w)
                
    sp = doc.add_paragraph()
    sp.paragraph_format.space_before = Pt(0)
    sp.paragraph_format.space_after = Pt(6)

def generate_report():
    doc = docx.Document()
    
    # Configure A4 Paper and Thesis Binding Margins
    sec = doc.sections[0]
    sec.page_width = Inches(8.27)
    sec.page_height = Inches(11.69)
    sec.top_margin = Inches(1.0)
    sec.bottom_margin = Inches(1.0)
    sec.left_margin = Inches(1.5)  # 1.5 inches binding margin
    sec.right_margin = Inches(1.0) # 1.0 inch right margin
    
    add_header_footer(doc)
    
    # Document Header Title Block
    p_title = add_styled_paragraph(doc, "LangkaLens: An Immersive Virtual Reality and Mixed Reality Cultural Heritage Platform for Ancient Yarang City with Intelligent AI Heritage Guide", 
                                   font_name="Times New Roman", size_pt=18, bold=True, color_rgb=(0x1F, 0x49, 0x7D), 
                                   align=WD_ALIGN_PARAGRAPH.CENTER, space_before=10, space_after=6)
    
    p_sub = add_styled_paragraph(doc, "1. System Architecture: 1.1 System Analysis and Design & 1.2 System Development", 
                                 font_name="Times New Roman", size_pt=13, bold=True, color_rgb=(0x2B, 0x57, 0x9A), 
                                 align=WD_ALIGN_PARAGRAPH.CENTER, space_before=0, space_after=14)
    
    # Meta Info Box
    tbl_meta = doc.add_table(rows=1, cols=1)
    tbl_meta.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_meta.autofit = False
    tbl_meta.columns[0].width = Inches(5.75)
    c_meta = tbl_meta.cell(0, 0)
    set_cell_background(c_meta, "F0F4F8")
    set_cell_margins(c_meta, top=100, bottom=100, left=150, right=150)
    set_cell_borders(c_meta, left="single", color="1F497D", sz="24")
    
    p_meta = c_meta.paragraphs[0]
    p_meta.paragraph_format.line_spacing = 1.15
    p_meta.paragraph_format.space_before = Pt(2)
    p_meta.paragraph_format.space_after = Pt(2)
    r_m = p_meta.add_run(
        "Academic Context: Senior Project Technical Specification & Architectural Report\n"
        "Author: Wansobree Hajihama (Student ID: 6620610023)\n"
        "Project Advisor: Assistant Professor Dr. Zamree Che-aron (ผศ.ดร.ซัมรี เจ๊ะอารน)\n"
        "Academic Program: Bachelor of Science in Computer and Informatics for Management (CIM)\n"
        "Institution: Faculty of Communication Sciences, Prince of Songkla University, Pattani Campus\n"
        "Target Platforms: Meta Quest 3 / Meta Quest 3S (Horizon OS / Android 12L) | Engine: Unity 6000.3.16f1 URP"
    )
    format_run_font(r_m, "Times New Roman", 9.5, italic=False, color_rgb=(0x33, 0x44, 0x55))
    
    sp0 = doc.add_paragraph()
    sp0.paragraph_format.space_before = Pt(4)
    sp0.paragraph_format.space_after = Pt(10)
    
    # =========================================================================
    # 1. SYSTEM ARCHITECTURE OVERVIEW
    # =========================================================================
    add_heading_1(doc, "1. System Architecture")
    
    add_styled_paragraph(
        doc,
        "LangkaLens is a high-performance standalone spatial computing platform developed for the Meta Quest 3 and "
        "Meta Quest 3S head-mounted displays (HMDs). The system preserves, interprets, and communicates the rich cultural "
        "and archaeological heritage of Ancient Yarang City (Ban Jalae and Ban Wat archaeological complexes in Pattani, "
        "Southern Thailand). Archaeological sites in Yarang frequently feature surviving brick foundations, moat contours, "
        "and weathered architectural baselines, but lack complete standing upper structures. To solve this visual and "
        "educational challenge, LangkaLens integrates three complementary experiential modalities within a unified application: "
        "(1) a 360-degree high-resolution panoramic photographic tour covering Ban Jalae 2, Ban Jalae 3, Ban Jalae 8, and Ban Wat 9; "
        "(2) an interactive Virtual Reality (VR) museum facilitating 6-degree-of-freedom (6-DOF) examination, manipulation, "
        "and dynamic model comparison of digitised artifacts; and (3) an on-site Mixed Reality (MR) spatial reconstruction experience "
        "that overlays 1:1 scale interpreted architectural reconstructions onto physical ruins using high-fidelity color video passthrough "
        "and hardware-tracked spatial session anchors."
    )
    
    add_styled_paragraph(
        doc,
        "The overarching architectural design adheres to a decoupled, modular service-oriented structure implemented in Unity 6 "
        "(6000.3.16f1) utilizing the Universal Render Pipeline (URP). Standalone XR headsets operate under strict thermal envelopes "
        "and mobile hardware budgets (Snapdragon XR2 Gen 2 SoC). Consequently, the architecture prioritizes: "
        "(a) scene modularity to isolate rendering state; "
        "(b) asynchronous asset loading and aggressive memory deallocation via Unity Addressables; "
        "(c) decoupled user interface (UI) and interaction helpers via Meta XR Interaction SDK; and "
        "(d) an external multimodal AI microservice gateway connecting local speech recording (Groq Whisper) and contextual "
        "large language model generation (Google Gemini REST API) without bogging down the rendering thread."
    )
    
    add_callout(
        doc,
        "Architectural Tenet",
        "LangkaLens strictly decouples core presentation scenes from external cloud intelligence. "
        "All 3D models, panoramic spheres, localization assets, and spatial alignment parameters execute client-side "
        "on the Meta Quest headset. Cloud AI services (Gemini chat and Groq transcription) operate asynchronously "
        "with robust timeout, cancellation, and local fallback routines to ensure application stability under unpredictable field network conditions."
    )
    
    # =========================================================================
    # 1.1 SYSTEM ANALYSIS AND DESIGN
    # =========================================================================
    add_heading_2(doc, "1.1 System Analysis and Design")
    
    add_styled_paragraph(
        doc,
        "System Analysis and Design establishes the foundational requirements, structural paradigms, behavioral models, "
        "and data schemas that govern the LangkaLens ecosystem. This section analyzes the user needs, formalizes functional and "
        "non-functional specifications, models dynamic workflows through UML activity diagrams and sequence charts, and delineates "
        "the data dictionary governing spatial ScriptableObjects."
    )
    
    # 1.1.1 Requirements Analysis
    add_heading_3(doc, "1.1.1 Requirements Engineering & Analysis")
    
    add_styled_paragraph(
        doc,
        "The engineering requirements for LangkaLens were derived from field visits to Ancient Yarang City, consultations with "
        "archaeological literature from the Fine Arts Department of Thailand, and spatial ergonomics standards for mobile VR headsets. "
        "The system must accommodate both seasoned researchers and first-time VR users who may be unfamiliar with 6-DOF hand controllers."
    )
    
    add_heading_4(doc, "Functional Requirements (FR)")
    
    fr_headers = ["ID", "Functional Requirement", "Subsystem & Implementation Driver", "Verification Standard"]
    fr_rows = [
        ["FR-01", "Bilingual Locale & Mode Selection", "MainMenuManager; Unity Localization 1.5.12 StringTables", "Persistent language selection across session; loads selected mode asynchronously."],
        ["FR-02", "360-Degree Panoramic Navigation", "TourManager; TourNode ScriptableObjects; Addressables", "Smooth viewpoint traversal via directional indicators; releases previous texture handles."],
        ["FR-03", "VR Reconstruction Inspection", "TourReconstructionPresenter; InspectionManager", "Left Y button toggles 3D model; provides 6-DOF rotation, scaling, and bilingual info."],
        ["FR-04", "VR Museum Artifact Manipulation", "ArtifactData; OneGrabFreeRotateTransformer; Dimmer", "Displays artifacts on pedestals; dims environment during inspection; toggles alternate LODs."],
        ["FR-05", "On-Site MR Entrance Alignment", "MRMeshReferenceAlignment; OVRSpatialAnchor API", "Curator aligns bounding reference with physical sign/table; creates session spatial anchor."],
        ["FR-06", "MR Tracking & Display Gating", "MRMeshPlayableArea; MRModelSpawner; Passthrough", "Continuous validation of head tracking, anchor localization, and passthrough; unrestricted mode."],
        ["FR-07", "Multimodal AI Heritage Guide", "GeminiChatUI; Google Gemini REST API client", "Contextual archaeological chat; localized system prompt injection; HTTP 429/5xx fallback."],
        ["FR-08", "Custom Voice Speech-to-Text", "SpeechToTextHandler; Groq Whisper Cloud API", "16kHz PCM WAV capture; transcribes speech into editable input field prior to query submission."]
    ]
    add_table_data(doc, "Table 1.1: Functional Requirements Specification (FR-01 to FR-08)", 
                   fr_headers, fr_rows, [0.7, 1.8, 1.8, 1.45])
    
    add_heading_4(doc, "Non-Functional Requirements (NFR)")
    
    nfr_headers = ["ID", "Category", "Target Specification & Metric", "Architectural Strategy"]
    nfr_rows = [
        ["NFR-01", "Performance & Frame Rate", "Consistent 72 FPS / 90 FPS; frame time <= 13.8 ms", "URP Forward+ rendering, ASTC 6x6 compression, single-pass instanced stereo rendering."],
        ["NFR-02", "Memory Management", "Peak heap footprint < 2.5 GB; zero VRAM leaks", "Unity Addressables async loading and handle release; texture cache garbage collection."],
        ["NFR-03", "Spatial Ergonomics", "Comfort FOV (30-60 deg); 1.2m-2.0m UI canvas distance", "World-space canvas anchoring; vergence-accommodation conflict mitigation."],
        ["NFR-04", "Cybersickness Mitigation", "Zero involuntary camera motion; snap turning support", "Instantaneous or quadratic ease-in transitions; universal one-handed locomotion."],
        ["NFR-05", "Fault Tolerance & Resilience", "Offline recovery; automatic fallback API models", "Single-attempt retry on HTTP 404/408/429/5xx; non-blocking coroutine timeouts."],
        ["NFR-06", "Bilingual Parity", "100% lexical symmetry between Thai and English", "Strict parity across StringTable collections for museum, tour, and UI labels."]
    ]
    add_table_data(doc, "Table 1.2: Non-Functional Requirements Specification (NFR-01 to NFR-06)", 
                   nfr_headers, nfr_rows, [0.7, 1.5, 1.8, 1.75])
    
    # 1.1.2 Conceptual & High-Level Architecture
    add_heading_3(doc, "1.1.2 Conceptual & High-Level System Architecture")
    
    add_styled_paragraph(
        doc,
        "The high-level architecture of LangkaLens follows a 4-tier layered model comprising: "
        "(1) the Spatial Presentation & User Experience Layer; "
        "(2) the Application & Domain Logic Layer; "
        "(3) the Hardware Abstraction & XR Platform Layer; and "
        "(4) the Data Persistence & External Cloud Services Layer. "
        "Figure 1.1 illustrates the structural topology and data channels interconnecting these tiers."
    )
    
    add_bullet_item(doc, "Tier 1: Spatial Presentation & UX Layer: ", 
                    "Encompasses world-space Canvas hierarchies, TextMesh Pro bilingual typography, Meta XR Passthrough visual layers, "
                    "spatial reticles, and audio feedback. All user interfaces are scaled to 0.001f world units to match physical-scale optics.")
    
    add_bullet_item(doc, "Tier 2: Application & Domain Logic Layer: ", 
                    "Comprises dedicated MonoBehaviour state machines controlling scene orchestration: MainMenuManager for lifecycle routing, "
                    "TourManager for graph navigation, InspectionManager and InspectionEnvironmentDimmer for 6-DOF exhibit interaction, "
                    "MRMeshReferenceAlignment and MRMeshPlayableArea for physical-to-virtual anchoring, and GeminiChatUI for conversational guidance.")
    
    add_bullet_item(doc, "Tier 3: Hardware Abstraction & XR Engine Layer: ", 
                    "Powered by Unity 6 Engine and the Meta XR All-In-One SDK (v203.0.2). This tier interfaces with the Snapdragon XR2 Gen 2 "
                    "SoC, translating 6-DOF Inside-Out SLAM tracking, optical passthrough buffers, spatial anchor hardware, and bimanual Touch Plus "
                    "controller events into standard OpenXR and Meta XR input streams.")
    
    add_bullet_item(doc, "Tier 4: Data & External Cloud Services Layer: ", 
                    "Splits into offline serialized assets (ScriptableObjects for tour nodes, photogrammetric asset bundles managed by Addressables) "
                    "and secure RESTful microservice gateways connecting to Google Gemini API (conversational synthesis) and Groq Cloud (Whisper ASR).")
    
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_1.png", 
                     "Figure 1.1: High-Level System Architecture and External Services Topology", width_in=5.6)
    
    # 1.1.3 Actor Profiles and Use Case Modeling
    add_heading_3(doc, "1.1.3 Actor Profiles and Functional Use Cases")
    
    add_styled_paragraph(
        doc,
        "System actors represent entities interacting with LangkaLens. The primary human actor is the Cultural Heritage Visitor / Learner, "
        "encompassing students, tourists, and museum attendees. The secondary human actor is the Field Researcher / Site Curator, responsible "
        "for establishing on-site spatial alignment at archaeological monuments. The external system actors comprise Google Gemini and Groq Cloud API. "
        "Figure 1.2 models the comprehensive functional decomposition across these actors."
    )
    
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_2.png", 
                     "Figure 1.2: System Functions and Use Case Diagram for Visitors and Curators", width_in=5.6)
    
    add_bullet_item(doc, "UC-01 (Select Language & Experience): ", 
                    "Visitor specifies Thai or English locale, confirmed for the running session, and activates Tour, Museum, or MR mode.")
    add_bullet_item(doc, "UC-02 (Traverse 360 Panoramic Viewpoints): ", 
                    "Visitor points at navigation indicators to trigger asynchronous photographic loading across Ban Jalae and Ban Wat sites.")
    add_bullet_item(doc, "UC-03 (Inspect Tour 3D Reconstructions): ", 
                    "Visitor depresses left Y button at designated nodes to spawn interpretive architectural models with bilingual archaeological notes.")
    add_bullet_item(doc, "UC-04 (Manipulate Museum Artifacts): ", 
                    "Visitor grabs display station exhibits with 6-DOF rotation, adjusts scale, dims background lighting, and toggles alternate mesh states.")
    add_bullet_item(doc, "UC-05 (Perform MR Site Registration): ", 
                    "Curator uses right grip and thumbstick to align virtual outline with physical entrance sign/table, locking a spatial session anchor.")
    add_bullet_item(doc, "UC-06 (Explore Mixed Reality Monument): ", 
                    "Visitor observes 1:1 scale reconstructed monument in passthrough, toggles visibility via A button, and reads archaeological evidence panels.")
    add_bullet_item(doc, "UC-07 (Query AI Heritage Guide via Keyboard): ", 
                    "Visitor types historical questions via world-space bilingual virtual keyboard; receives contextual LLM answers.")
    add_bullet_item(doc, "UC-08 (Query AI Heritage Guide via Speech): ", 
                    "Visitor dictates questions via headset microphone; Groq Whisper transcribes speech into editable text before submission.")
    
    # 1.1.4 System Workflow & Behavioral Design
    add_heading_3(doc, "1.1.4 System Workflow & Behavioral Design (Activity Flowcharts)")
    
    add_styled_paragraph(
        doc,
        "Behavioral workflows model the state transitions, conditional branches, and asynchronous operations governing user interaction. "
        "The following flowcharts detail the operational mechanics of each scene and shared subsystem."
    )
    
    # 1.1.4.1 App Lifecycle
    add_heading_4(doc, "Application Lifecycle & Scene Orchestration")
    add_styled_paragraph(
        doc,
        "The application follows a centralized hub-and-spoke execution pattern (Figure 1.3). Upon launch, Main_Menu_Scene is loaded first. "
        "Visitors can navigate into any of the three experiential scenes. Return requests prompt user confirmation before cleanly restoring "
        "the main menu and deallocating mode-specific assets."
    )
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_3.png", 
                     "Figure 1.3: Application Overview and Scene Navigation Flowchart", width_in=5.5)
    
    # 1.1.4.2 Main Menu
    add_heading_4(doc, "Main Menu & Session Language Confirmation Workflow")
    add_styled_paragraph(
        doc,
        "The menu workflow (Figure 1.4) enforces session-scoped language persistence. When launched, the system checks whether language "
        "has been confirmed during the current application session. If unconfirmed, the bilingual selection dialog is displayed. "
        "Subsequent returns to the main menu skip this prompt, preserving user flow while ensuring restarting the app prompts a fresh choice."
    )
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_4.png", 
                     "Figure 1.4: Main Menu Language Selection and Scene Loading Flowchart", width_in=5.4)
    
    # 1.1.4.3 VR Tour
    add_heading_4(doc, "VR Tour Viewpoint Traversal & Map Navigation Workflow")
    add_styled_paragraph(
        doc,
        "Tour navigation (Figure 1.5) operates as an event-driven state machine. Selecting a linked arrow disables user locomotion, "
        "loads the destination equirectangular texture asynchronously, animates camera container movement, disposes of the prior texture handle, "
        "and activates new navigation indicators upon successful arrival. Failed texture loads gracefully recover without stranding the user."
    )
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_5.png", 
                     "Figure 1.5: VR 360 Panoramic Tour Navigation Flowchart", width_in=5.5)
    
    # 1.1.4.4 VR Tour Inspection
    add_heading_4(doc, "VR Tour Restored-Model Inspection Workflow")
    add_styled_paragraph(
        doc,
        "Figure 1.6 models the contextual 3D reconstruction inspection available at key archaeological viewpoints. Pressing left controller Y "
        "loads the reconstruction display prefab via Addressables. If the user moves away or cancels, asynchronous loads are aborted and discarded, "
        "preventing lingering objects from polluting the scene graph."
    )
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_6.png", 
                     "Figure 1.6: VR Tour Reconstruction Inspection and Memory Cleanup Flowchart", width_in=5.4)
    
    # 1.1.4.5 VR Museum
    add_heading_4(doc, "VR Museum Artifact Exploration & Dimmer Lifecycle Workflow")
    add_styled_paragraph(
        doc,
        "The museum workflow (Figure 1.7) incorporates a dynamic focus mechanism. When an artifact is selected, player locomotion is locked, "
        "pre-inspection camera poses are cached, and InspectionEnvironmentDimmer initiates an exposure fade on the surrounding gallery "
        "while keeping the artifact illuminated. Closing inspection restores gallery lighting and returns the artifact to its pedestal."
    )
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_7.png", 
                     "Figure 1.7: VR Museum Artifact Exploration and Environment Dimmer Flowchart", width_in=5.5)
    
    # 1.1.4.6 MR Entrance Alignment
    add_heading_4(doc, "Mixed Reality Entrance Alignment & Session Anchor Workflow")
    add_styled_paragraph(
        doc,
        "Figure 1.8 illustrates the on-site physical registration pipeline. The curator aligns a virtual entrance outline to physical "
        "signage using the right controller grip and thumbstick. Confirming with button A initiates hardware spatial anchor creation "
        "with a 15-second timeout window. Upon localization, the relative transformation matrix is locked for the session."
    )
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_8.png", 
                     "Figure 1.8: On-Site Mixed Reality Entrance Alignment and Anchor Flowchart", width_in=5.4)
    
    # 1.1.4.7 MR Model Display
    add_heading_4(doc, "MR Model Display Gating & Tracking Recovery Workflow")
    add_styled_paragraph(
        doc,
        "Continuous runtime safety is governed by Figure 1.9. In the saved unrestricted configuration, model display requires "
        "alignment confirmation, active head tracking, and passthrough readiness. If tracking quality drops or passthrough is interrupted, "
        "the reconstructed building automatically conceals itself until SLAM tracking re-establishes stability."
    )
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_9.png", 
                     "Figure 1.9: MR Model Display Gating and Tracking Recovery Flowchart", width_in=5.5)
    
    # 1.1.4.8 Shared AI Guide
    add_heading_4(doc, "Shared Multimodal AI Heritage Guide Workflow")
    add_styled_paragraph(
        doc,
        "Figure 1.10 models the dual-modality AI pipeline. Users may type questions or dictate speech. Audio recordings are transmitted "
        "to Groq Whisper for transcription and returned to an editable text field. This allows users to review and amend the transcription "
        "before submitting the prompt to Google Gemini, eliminating misrecognized speech errors."
    )
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_10.png", 
                     "Figure 1.10: Shared Multimodal AI Heritage Guide Workflow", width_in=5.5)
    
    # 1.1.5 Dynamic Process & Interaction Design
    add_heading_3(doc, "1.1.5 Dynamic Process & Interaction Design (Sequence Diagrams)")
    
    add_styled_paragraph(
        doc,
        "Dynamic interaction design specifies the chronological message passing, asynchronous callbacks, and resource handoffs "
        "occurring between software components. The following UML sequence diagrams document the primary runtime processes."
    )
    
    # Process 1: Tour
    add_heading_4(doc, "Process 1: Tour Viewpoint Transition & Asynchronous Asset Loading")
    add_styled_paragraph(
        doc,
        "Figure 1.11 details the sequence when a visitor activates a navigation indicator. TourManager locks navigation inputs, "
        "requests the destination Texture2D handle via Addressables, smoothly translates the player rig, releases the preceding texture "
        "from GPU memory, and instantiates destination directional indicators upon completion."
    )
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_11.png", 
                     "Figure 1.11: Sequence Diagram: Asynchronous Tour Viewpoint Transition", width_in=5.5)
    
    # Process 2: Museum
    add_heading_4(doc, "Process 2: Museum Artifact Inspection & Environment Dimming")
    add_styled_paragraph(
        doc,
        "Figure 1.12 documents the concurrent execution during artifact examination. InspectionManager signals InspectionEnvironmentDimmer "
        "to execute a non-blocking shader lerp on gallery materials using unscaled delta time. Simultaneously, player locomotion is frozen, "
        "the 6-DOF transformer is enabled, and bilingual TextMesh Pro metadata panels are populated."
    )
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_12.png", 
                     "Figure 1.12: Sequence Diagram: Museum Artifact Inspection and Dimmer Lifecycle", width_in=5.5)
    
    # Process 3: MR Alignment
    add_heading_4(doc, "Process 3: MR Spatial Alignment & Session Anchoring")
    add_styled_paragraph(
        doc,
        "Figure 1.13 details the communication between MRMeshReferenceAlignment, OVRSpatialAnchor, and MRModelSpawner. "
        "The hardware spatial anchor localization state is polled asynchronously. Once locked, MRMeshPlayableArea updates its CanPlay state, "
        "permitting MRModelSpawner to instantiate the Ban Jalae 3 reconstruction model at true scale."
    )
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_13.png", 
                     "Figure 1.13: Sequence Diagram: MR Entrance Alignment and Spatial Anchoring", width_in=5.5)
    
    # Process 4: AI Guide
    add_heading_4(doc, "Process 4: Multimodal AI Heritage Guide (Speech & LLM Synthesis)")
    add_styled_paragraph(
        doc,
        "Figure 1.14 details the decoupled speech and chat request lifecycles. SpeechToTextHandler streams WAV audio to Groq Whisper, "
        "populates the input field, and allows the visitor to press Send. GeminiChatUI dispatches the prompt with localized Yarang "
        "historical system instructions, handling retry fallback on HTTP rate limits (429) or service outages (5xx)."
    )
    add_image_figure(doc, "Docs/Report/figures/diagrams/figure_3_14.png", 
                     "Figure 1.14: Sequence Diagram: Multimodal AI Guide Speech-to-Text and LLM Query", width_in=5.5)
    
    # 1.1.6 User Interface & Spatial Interaction Design
    add_heading_3(doc, "1.1.6 User Interface & Spatial Interaction Design")
    
    add_styled_paragraph(
        doc,
        "Spatial user interface design in VR and MR must eliminate vergence-accommodation conflict, maintain readable text sizing, "
        "and prevent neck strain. LangkaLens adheres to established Quest design guidelines: all world-space canvases are positioned "
        "at an optimal focal distance of 1.2 to 2.0 meters, scaled to 0.001f, and tilted slightly toward the user's eye level. "
        "TextMesh Pro delivers crisp distance-field bilingual rendering in TH Sarabun New (Thai) and sans-serif Latin fonts."
    )
    
    add_styled_paragraph(
        doc,
        "To ensure accessibility for both novice visitors and experienced gamers, interaction mappings are unified across controllers. "
        "Table 1.3 delineates the bimanual controller mapping matrix across the four primary operational modes."
    )
    
    ctrl_headers = ["Mode", "Primary Hand", "Input Element", "Action / Trigger", "Visual Feedback & UI State"]
    ctrl_rows = [
        ["Main Menu", "Right / Left", "Index Trigger", "Raycast Point & Click", "Button highlight, audio click, scene load overlay"],
        ["VR Tour", "Right Hand", "Index Trigger", "Select Navigation Arrow", "Quadratic camera translation, arrow pulse"],
        ["VR Tour", "Left Hand", "Button Y", "Toggle Reconstruction", "Spawns 3D model, displays historical description panel"],
        ["VR Tour", "Right Hand", "Thumbstick", "Rotate Reconstruction", "Model turns smoothly on vertical axis; click resets"],
        ["VR Museum", "Right / Left", "Grip Button", "One-Handed 6-DOF Grab", "Halts auto-spin; model follows hand translation/rotation"],
        ["VR Museum", "Right Hand", "Buttons A / B", "Scale Model (Hold)", "Holding A reduces scale; holding B enlarges model"],
        ["VR Museum", "Left Hand", "Button Y", "Toggle Alternate Model", "Swaps weathered photogrammetry model with restored form"],
        ["VR Museum", "Left Hand", "Button X / Close", "Exit Inspection", "Restores original lighting, pedestal, and locomotion"],
        ["On-Site MR", "Right Hand", "Grip + Stick", "Position Entrance Outline", "Moves outline in 3D space; stick adjusts reach & yaw"],
        ["On-Site MR", "Right Hand", "Button A", "Confirm Alignment", "Generates session spatial anchor; locks site matrix"],
        ["On-Site MR", "Right Hand", "Button A / B", "Toggle Model / Site Info", "A toggles monument visibility; B opens evidence panel"],
        ["On-Site MR", "Left Hand", "Hold Button X (2s)", "Realign Site", "Clears spatial anchor; returns to alignment mode"],
        ["Shared Submenu", "Left Hand", "Menu Button", "Toggle Tablet / AI Guide", "Opens world-space tablet for AI chat and guide"]
    ]
    add_table_data(doc, "Table 1.3: Bimanual Controller Interaction Mapping Matrix", 
                   ctrl_headers, ctrl_rows, [0.8, 0.8, 1.1, 1.5, 1.55])
    
    # 1.1.7 Data Structure & Content Asset Architecture
    add_heading_3(doc, "1.1.7 Data Structure & Content Asset Architecture")
    
    add_styled_paragraph(
        doc,
        "LangkaLens employs an asset-driven, modular data architecture leveraging Unity ScriptableObjects and Addressable AssetBundles. "
        "Separating archaeological content definitions from runtime engine code ensures that additional viewpoints, reconstructed stupas, "
        "or newly excavated artifacts can be integrated into the application without altering compiled C# scripts. "
        "Table 1.4 provides the data dictionary defining the primary data entities and their serialized attributes."
    )
    
    dd_headers = ["Data Entity", "Field / Attribute", "Type", "Functional Role & Constraint"]
    dd_rows = [
        ["TourArea", "areaName", "string", "Localized human-readable area title (e.g., 'Ban Jalae 3')."],
        ["TourArea", "startingNode", "TourNode", "Root viewpoint entry node loaded upon area selection."],
        ["TourArea", "reconstruction", "TourReconstructionDef", "Reference to the 3D reconstructed monument definition."],
        ["TourNode", "areaNumber", "int", "Archaeological area index matching Yarang site registry."],
        ["TourNode", "position", "NodePosition (Enum)", "Categorical placement: Front, Back, Left, Right, Center."],
        ["TourNode", "photoReference", "AssetReferenceTexture2D", "Addressables key pointing to the 8K equirectangular panorama."],
        ["TourNode", "previousPath / nextPath", "NodeConnection[]", "Bidirectional graph links defining navigable adjacent nodes."],
        ["NodeConnection", "targetNode", "TourNode", "Destination node loaded upon indicator interaction."],
        ["NodeConnection", "arrowPosition / Rotation", "Vector3 / Quaternion", "Spatial transform offset for rendering directional arrow in 3D."],
        ["TourReconstructionDef", "inspectionPrefab", "AssetReferenceGameObject", "Addressables prefab holding 3D stupa mesh and collider."],
        ["TourReconstructionDef", "viewingDistance / scale", "float", "Optimal offset and scale factor applied during inspection."],
        ["ArtifactData", "localizedTitle / Desc", "LocalizedString", "Unity Localization Table keys for bilingual Thai/English text."],
        ["ArtifactData", "primaryModel / altModel", "GameObject", "Primary weathered model and optional reconstructed variant."],
        ["MRMeshPlayableArea", "footprintMode", "FootprintMode (Enum)", "Set to Unrestricted (mode 3) in current saved scene configuration."],
        ["MRMeshPlayableArea", "CanPlay", "computed bool", "Runtime gate verifying alignment, tracking, and passthrough readiness."]
    ]
    add_table_data(doc, "Table 1.4: Spatial Data Dictionary & ScriptableObject Architecture", 
                   dd_headers, dd_rows, [1.2, 1.4, 1.4, 1.75])
    
    # =========================================================================
    # 1.2 SYSTEM DEVELOPMENT
    # =========================================================================
    add_heading_2(doc, "1.2 System Development")
    
    add_styled_paragraph(
        doc,
        "System Development documents the technical construction, toolchain infrastructure, codebase organization, "
        "subsystem engineering implementations, and mobile VR optimization pipelines employed to realize LangkaLens. "
        "This section details the concrete C# implementations, component wiring, Editor authoring workflows, and runtime verification captures."
    )
    
    # 1.2.1 Development Environment & Toolchain
    add_heading_3(doc, "1.2.1 Development Environment & Toolchain Infrastructure")
    
    add_styled_paragraph(
        doc,
        "Developing for standalone VR requires a synchronized development toolchain encompassing high-performance compilation workstations, "
        "target XR headsets, physical tethering interfaces, and specialized device management software. Table 1.5 enumerates the complete "
        "hardware and software environment supporting LangkaLens development."
    )
    
    env_headers = ["Category", "Hardware / Software Component", "Specification / Version", "Engineering Purpose"]
    env_rows = [
        ["Workstation", "HP ZBook Fury 15.6 G8 PC", "Intel i7-11800H, 64 GB RAM", "Primary engine compilation, shader cooking, asset packaging."],
        ["GPU", "NVIDIA RTX A4000 Laptop GPU", "16 GB GDDR6 VRAM, Studio Driver", "High-fidelity VR scene rendering and texture baking."],
        ["Target HMD", "Meta Quest 3 & Quest 3S", "Snapdragon XR2 Gen 2, Horizon OS", "Primary deployment target for standalone 6-DOF VR and MR."],
        ["Tethering", "High-Speed Link Cables", "2m USB4 (10 Gb/s) & 5m USB-C 3.0", "Rapid playtesting via Meta Quest Link and live ADB debugging."],
        ["Engine", "Unity Editor", "6000.3.16f1 (Unity 6 LTS)", "Core game engine, scene hierarchy, and build orchestrator."],
        ["Pipeline", "Universal Render Pipeline", "URP 17.3.0 (Forward+ renderer)", "Mobile-optimized single-pass stereo rendering."],
        ["XR SDK", "Meta XR All-In-One SDK", "v203.0.2 (Core 203.0.0, ISDK 203.0.0)", "Camera rig tracking, passthrough buffers, spatial anchors."],
        ["Asset Loader", "Unity Addressables", "v2.9.1", "Asynchronous texture loading and GPU memory lifecycle management."],
        ["Localization", "Unity Localization", "v1.5.12", "Dynamic runtime bilingual Thai (th) and English (en) switching."],
        ["Compilation", "Android NDK & OpenJDK", "r23c / OpenJDK 17 (IL2CPP, ARM64)", "Compiles high-performance C++ binaries for Horizon OS (Android 12L)."],
        ["Desktop Sim", "Meta XR Simulator", "v203.0 (Integrated into Editor)", "Simulates 6-DOF head motion, passthrough, and controllers on PC."],
        ["Device Hub", "Meta Quest Developer Hub (MQDH)", "v6.5.2", "APK sideloading, real-time performance metrics, and ADB log streaming."]
    ]
    add_table_data(doc, "Table 1.5: Development Toolchain and Environmental Specifications", 
                   env_headers, env_rows, [1.0, 1.6, 1.5, 1.65])
    
    # 1.2.2 Project Organization & Modular Architecture
    add_heading_3(doc, "1.2.2 Project Directory Structure & Software Organization")
    
    add_styled_paragraph(
        doc,
        "The project repository follows a strict modular structure under Assets/_LangkaLens/, cleanly dividing scenes, subsystem scripts, "
        "and data assets to maintain loose coupling. Reusable components (inspection managers, dimmers, bilingual keyboards, and AI clients) "
        "are housed in shared directories to prevent redundant code across scenes."
    )
    
    add_bullet_item(doc, "Assets/_LangkaLens/_Scenes/: ", 
                    "Houses the four enabled runtime scenes: Main_Menu_Scene.unity (build index 0), VR_Tour_Scene.unity (index 1), "
                    "VR_Museum_Scene.unity (index 2), and MR_Scene.unity (index 3).")
    add_bullet_item(doc, "Assets/_LangkaLens/Main_menu/: ", 
                    "Contains MainMenuManager.cs, audio feedback clips, and RigResetter.cs for camera orientation calibration.")
    add_bullet_item(doc, "Assets/_LangkaLens/Tour/: ", 
                    "Contains TourManager.cs, TourNode.cs, TourArea.cs, TourReconstructionDefinition.cs, and the Addressables catalog for 360 photos.")
    add_bullet_item(doc, "Assets/_LangkaLens/Museum/: ", 
                    "Contains ArtifactData.cs, OneGrabFreeRotateTransformer.cs, exhibit pedestal prefabs, and 3D photogrammetric meshes.")
    add_bullet_item(doc, "Assets/_LangkaLens/MR/: ", 
                    "Contains MRMeshReferenceAlignment.cs, MRMeshPlayableArea.cs, MRModelSpawner.cs, and MRSiteInformationPanel.cs.")
    add_bullet_item(doc, "Assets/_LangkaLens/Mixed/: ", 
                    "Houses cross-scene shared modules: InspectionManager.cs, InspectionEnvironmentDimmer.cs, GeminiChatUI.cs, "
                    "SpeechToTextHandler.cs, and BilingualVirtualKeyboard.cs.")
    add_bullet_item(doc, "Assets/_LangkaLens/Misc/: ", 
                    "Contains Unity Localization StringTable collections, Editor validation utilities, and UniversalOneHandedLocomotion.cs.")
    
    # 1.2.3 Subsystem Implementation
    add_heading_3(doc, "1.2.3 Subsystem Engineering & Implementation Details")
    
    add_styled_paragraph(
        doc,
        "This section analyzes the technical construction of each core subsystem, linking architectural principles directly to "
        "representative C# implementations, component parameters, and Unity Editor authoring setups."
    )
    
    # Subsystem 1: Main Menu & Localization
    add_heading_4(doc, "Subsystem 1: Main Menu & Localization Engine")
    add_styled_paragraph(
        doc,
        "The main menu serves as the initial entry point and session coordinator. MainMenuManager.cs manages locale initialization, "
        "session language selection, and scene routing. Upon scene load, RigResetter.cs forces the OVRCameraRig tracking space to zero "
        "and recenters the camera to eye level, preventing disorientation caused by headset repositioning between app launches. "
        "Unity Localization StringTable collections dynamically broadcast locale changes to all active TextMesh Pro labels."
    )
    add_image_figure(doc, "Docs/Screenshots/Development/unity-main-menu-editor.png", 
                     "Figure 1.15: Unity Editor Authoring: Main Menu Scene Hierarchy and Inspector Wiring", width_in=5.5)
    add_image_figure(doc, "Docs/Screenshots/Playtesting/main-menu-simulator.png", 
                     "Figure 1.16: Meta XR Simulator Playtest: Main Menu Hub and Mode Selection Interface", width_in=5.5)
    
    # Subsystem 2: VR Tour
    add_heading_4(doc, "Subsystem 2: VR 360-Degree Panoramic Tour Subsystem")
    add_styled_paragraph(
        doc,
        "The VR Tour subsystem renders high-resolution equirectangular spherical panoramas captured at Ancient Yarang City. "
        "TourManager.cs parses TourNode ScriptableObjects to construct an in-memory navigation graph. When the user selects a directional arrow, "
        "TourManager initiates an asynchronous transition: (1) user input is locked; (2) Addressables loads the new 8K Texture2D asynchronously; "
        "(3) camera container position interpolates via quadratic ease-in; (4) the previous texture is deallocated via Addressables.Release(); "
        "and (5) destination directional arrows are populated. TourReconstructionPresenter.cs enables in-situ 3D inspection of ancient stupas "
        "when the user depresses the left Y button, smoothly transitioning the user into close inspection."
    )
    add_image_figure(doc, "Docs/Screenshots/Development/unity-tour-editor.png", 
                     "Figure 1.17: Unity Editor Authoring: VR Tour Viewpoint Graph and Addressable Setup", width_in=5.5)
    add_image_figure(doc, "Docs/Screenshots/Playtesting/tour-panorama-simulator.png", 
                     "Figure 1.18: Meta XR Simulator Playtest: 360 Panoramic Viewpoint Traversal at Yarang", width_in=5.5)
    
    # Subsystem 3: VR Museum
    add_heading_4(doc, "Subsystem 3: VR Museum & 3D Artifact Inspection Subsystem")
    add_styled_paragraph(
        doc,
        "The Virtual Museum displays photogrammetric 3D scans of excavated Yarang artifacts (e.g., terracotta reliefs, votive tablets, decorated bricks). "
        "Each exhibit pedestal features an ArtifactData component linking localized titles, descriptions, and dual-mesh references. "
        "When an artifact is touched or pointed at, InspectionManager.cs triggers the inspection mode: locomotion is halted, and "
        "InspectionEnvironmentDimmer.cs executes an unscaled exposure fade on surrounding gallery renderers, darkening the room while "
        "spotlighting the exhibit. Users manipulate the model using OneGrabFreeRotateTransformer.cs (a specialized Meta Interaction SDK component "
        "supporting smooth 6-DOF trackball rotation) and can toggle between weathered photogrammetry and hypothetical restored forms via button Y."
    )
    add_image_figure(doc, "Docs/Screenshots/Development/unity-museum-editor.png", 
                     "Figure 1.19: Unity Editor Authoring: VR Museum Gallery Hierarchy and Exhibit Stations", width_in=5.5)
    add_image_figure(doc, "Docs/Screenshots/Playtesting/museum-inspection-simulator.png", 
                     "Figure 1.20: Meta XR Simulator Playtest: 6-DOF Artifact Examination with Surrounding Dimmer", width_in=5.5)
    
    # Subsystem 4: MR Scene
    add_heading_4(doc, "Subsystem 4: On-Site Mixed Reality Alignment & Reconstruction Subsystem")
    add_styled_paragraph(
        doc,
        "The Mixed Reality mode is engineered for on-site deployment at Ban Jalae 3 monument. Full-color stereoscopic video passthrough "
        "provides the real-world background. MRMeshReferenceAlignment.cs enables curators to match a virtual bounding reference box "
        "with an on-site physical benchmark (e.g., an information table or signpost) using the right grip and thumbstick. Confirming with button A "
        "creates a persistent hardware OVRSpatialAnchor. MRMeshPlayableArea.cs evaluates tracking health and gates content visibility through "
        "its CanPlay property. MRModelSpawner.cs instantiates the 1:1 scale reconstructed monument, allowing visitors to walk around the ancient "
        "stupa while observing physical brick foundations through passthrough."
    )
    add_image_figure(doc, "Docs/Screenshots/Development/unity-mr-alignment-editor.png", 
                     "Figure 1.21: Unity Editor Authoring: On-Site MR Entrance Alignment and Model Spawner", width_in=5.5)
    
    # Subsystem 5: Shared AI Guide
    add_heading_4(doc, "Subsystem 5: Multimodal AI Heritage Guide & Speech Subsystem")
    add_styled_paragraph(
        doc,
        "The intelligent guide is accessible across all scenes via a wrist tablet or submenu. GeminiChatUI.cs manages asynchronous HTTP requests "
        "to the Google Gemini API (gemini-1.5-flash / gemini-2.0-flash). The request injects curated archaeological prompt instructions "
        "emphasizing that upper reconstructions are academic interpretations. To protect against network drops, the client implements "
        "exponential backoff and a single-attempt fallback model for HTTP 404/408/429/5xx errors. "
        "SpeechToTextHandler.cs captures audio via Unity's Microphone API, encodes 16-bit PCM WAV chunks, and streams them to Groq Whisper "
        "(whisper-large-v3). The returned transcript populates an editable input field, allowing visitors to verify text before sending. "
        "A virtual bilingual keyboard (BilingualVirtualKeyboard.cs) supports direct typed entry in English and Thai."
    )
    add_image_figure(doc, "Docs/Screenshots/Playtesting/ai-guide-simulator.png", 
                     "Figure 1.22: Meta XR Simulator Playtest: Shared Multimodal AI Heritage Guide Interface", width_in=5.5)
    
    # 1.2.4 Build, Optimization & Packaging
    add_heading_3(doc, "1.2.4 Build Pipeline, Mobile XR Optimization & Asset Packaging")
    
    add_styled_paragraph(
        doc,
        "To achieve a stutter-free 72 FPS on mobile XR hardware, LangkaLens implements rigorous rendering and memory optimizations. "
        "The compilation and deployment pipeline is configured as follows:"
    )
    
    add_bullet_item(doc, "URP Forward+ Rendering & Single-Pass Stereo: ", 
                    "Forward+ rendering eliminates unnecessary multi-pass lighting overhead. Single-Pass Instanced Stereo rendering "
                    "submits draw calls for both eyes in a single GPU pass, halving CPU driver overhead.")
    
    add_bullet_item(doc, "ASTC Texture Compression: ", 
                    "All 8K panoramic equirectangular textures and 4K photogrammetric PBR material maps are encoded using ASTC 6x6 block compression, "
                    "significantly reducing GPU memory bandwidth and texture cache misses on the Adreno GPU.")
    
    add_bullet_item(doc, "Addressables Asset Bundling & Memory Management: ", 
                    "Panoramic textures and reconstruction prefabs are compiled into discrete Addressable AssetBundles. "
                    "Unused bundles are deallocated immediately upon scene exit or viewpoint transition via Addressables.Release(), "
                    "keeping the mobile heap footprint consistently below 2.5 GB.")
    
    add_bullet_item(doc, "Android XR Manifest & Permissions: ", 
                    "The AndroidManifest.xml configures android.hardware.vr.headtracking (required), RECORD_AUDIO (for voice input), "
                    "and com.oculus.feature.PASSTHROUGH (required for MR mode). The target SDK is set to Android 12L (API level 32) "
                    "with 64-bit ARM64 architecture.")
    
    add_bullet_item(doc, "Automated Verification via Meta XR Simulator: ", 
                    "The desktop development workflow leverages Meta XR Simulator v203.0 and MQDH v6.5.2 to validate 6-DOF tracking, "
                    "hand controller gestures, language switching, and UI layouts on PC before pushing release APKs to physical headsets.")
    
    # Concluding Remarks
    add_styled_paragraph(
        doc,
        "In summary, the system architecture and development methodology of LangkaLens establish a resilient, decoupled, and mobile-optimized "
        "foundation. By integrating asset-driven 360 tour navigation, 6-DOF museum interaction with dynamic focus dimming, on-site spatial-anchored "
        "mixed reality, and a fault-tolerant multimodal AI heritage guide, the platform bridges archaeological preservation with cutting-edge "
        "spatial computing standards.",
        space_before=12, space_after=14
    )
    
    # Save DOCX
    output_docx = r"Docs/Report/LangkaLens_System_Architecture.docx"
    os.makedirs(os.path.dirname(os.path.abspath(output_docx)), exist_ok=True)
    doc.save(output_docx)
    print(f"Successfully generated DOCX: {output_docx}")
    
    # Also export to Markdown for complete version-controlled transparency
    export_markdown(output_docx.replace(".docx", ".md"))

def export_markdown(md_path):
    md_content = """# 1. System Architecture: LangkaLens Cultural Heritage Platform
## 1.1 System Analysis and Design & 1.2 System Development

**Academic Context:** Senior Project Technical Specification & Architectural Report  
**Author:** Wansobree Hajihama (Student ID: 6620610023)  
**Project Advisor:** Assistant Professor Dr. Zamree Che-aron (ผศ.ดร.ซัมรี เจ๊ะอารน)  
**Academic Program:** Bachelor of Science in Computer and Informatics for Management (CIM)  
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
"""
    with open(md_path, "w", encoding="utf-8") as f:
        f.write(md_content)
    print(f"Successfully generated Markdown: {md_path}")

if __name__ == "__main__":
    generate_report()
