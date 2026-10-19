import os
import sys
from pathlib import Path
import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import parse_xml
from docx.oxml.ns import nsdecls

def set_cell_background(cell, fill_hex: str):
    tcPr = cell._element.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex.strip("#")}"/>')
    tcPr.append(shd)

def set_cell_margins(cell, top=60, bottom=60, left=100, right=100):
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

def add_header_footer(doc, subtitle_text):
    for sec in doc.sections:
        header = sec.header
        hp = header.paragraphs[0]
        hp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        hrun = hp.add_run(f"LangkaLens | {subtitle_text}")
        hrun.font.name = "Times New Roman"
        hrun.font.size = Pt(8.0)
        hrun.font.color.rgb = RGBColor(0x77, 0x77, 0x77)
        
        footer = sec.footer
        fp = footer.paragraphs[0]
        fp.alignment = WD_ALIGN_PARAGRAPH.CENTER
        frun = fp.add_run("Prince of Songkla University, Pattani Campus — Faculty of Communication Science (CIM)")
        frun.font.name = "Times New Roman"
        frun.font.size = Pt(8.0)
        frun.font.color.rgb = RGBColor(0x77, 0x77, 0x77)

def format_run_font(run, font_name="Times New Roman", size_pt=9.5, bold=False, italic=False, color_rgb=(0x22, 0x22, 0x22)):
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

def add_styled_paragraph(doc, text="", font_name="Times New Roman", size_pt=9.5, bold=False, italic=False, 
                         color_rgb=(0x22, 0x22, 0x22), align=WD_ALIGN_PARAGRAPH.LEFT, space_before=0, space_after=3, line_spacing=1.12):
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
    p.paragraph_format.space_before = Pt(10)
    p.paragraph_format.space_after = Pt(3)
    p.paragraph_format.keep_with_next = True
    r = p.add_run(text)
    format_run_font(r, "Times New Roman", 13.5, bold=True, color_rgb=(0x1F, 0x49, 0x7D))
    return p

def add_heading_2(doc, text):
    p = doc.add_paragraph(style='Heading 2')
    p.paragraph_format.space_before = Pt(8)
    p.paragraph_format.space_after = Pt(2)
    p.paragraph_format.keep_with_next = True
    r = p.add_run(text)
    format_run_font(r, "Times New Roman", 11.5, bold=True, color_rgb=(0x2B, 0x57, 0x9A))
    return p

def add_heading_3(doc, text):
    p = doc.add_paragraph(style='Heading 3')
    p.paragraph_format.space_before = Pt(6)
    p.paragraph_format.space_after = Pt(2)
    p.paragraph_format.keep_with_next = True
    r = p.add_run(text)
    format_run_font(r, "Times New Roman", 10.5, bold=True, color_rgb=(0x1A, 0x2E, 0x40))
    return p

def add_callout(doc, text, title="", width_inches=6.4):
    table = doc.add_table(rows=1, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    
    cell = table.cell(0, 0)
    cell.width = Inches(width_inches)
    set_cell_background(cell, "F0F4F8")
    set_cell_margins(cell, top=80, bottom=80, left=140, right=140)
    set_cell_borders(cell, left="single", color="1F497D", sz="24")
    
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(1)
    p.paragraph_format.space_after = Pt(1)
    p.paragraph_format.line_spacing = 1.12
    
    if title:
        r_title = p.add_run(f"{title}\n")
        format_run_font(r_title, "Times New Roman", 9.5, bold=True, color_rgb=(0x1F, 0x49, 0x7D))
    
    r_body = p.add_run(text)
    format_run_font(r_body, "Times New Roman", 9.0, italic=False, color_rgb=(0x33, 0x41, 0x55))
    
    spacer = doc.add_paragraph()
    spacer.paragraph_format.space_before = Pt(0)
    spacer.paragraph_format.space_after = Pt(3)

def add_image_box(doc, image_path, caption="", width_inches=4.8, max_height_inches=5.2):
    if not os.path.exists(image_path):
        print(f"Warning: Image not found at {image_path}")
        return
        
    try:
        from PIL import Image
        with Image.open(image_path) as im:
            img_w, img_h = im.size
            aspect = img_w / img_h
            if (width_inches / aspect) > max_height_inches:
                width_inches = max_height_inches * aspect
    except Exception:
        pass
        
    p_img = doc.add_paragraph()
    p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_img.paragraph_format.space_before = Pt(3)
    p_img.paragraph_format.space_after = Pt(2)
    p_img.paragraph_format.keep_with_next = True
    run_img = p_img.add_run()
    run_img.add_picture(image_path, width=Inches(width_inches))
    
    if caption:
        p_cap = doc.add_paragraph()
        p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_cap.paragraph_format.space_before = Pt(1)
        p_cap.paragraph_format.space_after = Pt(4)
        run_cap = p_cap.add_run(caption)
        format_run_font(run_cap, "Times New Roman", 8.5, italic=True, color_rgb=(0x55, 0x55, 0x55))

def create_styled_table(doc, headers, data, col_widths=None, font_size=8.5, space_after=4):
    table = doc.add_table(rows=len(data) + 1, cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    
    # Header row
    hdr_cells = table.rows[0].cells
    for i, title in enumerate(headers):
        hdr_cells[i].text = ""
        p = hdr_cells[i].paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        p.paragraph_format.space_before = Pt(3)
        p.paragraph_format.space_after = Pt(3)
        r = p.add_run(title)
        format_run_font(r, "Times New Roman", font_size + 0.5, bold=True, color_rgb=(0xFF, 0xFF, 0xFF))
        set_cell_background(hdr_cells[i], "1F497D")
        set_cell_margins(hdr_cells[i], top=60, bottom=60, left=90, right=90)
        set_cell_borders(hdr_cells[i], top="single", bottom="single", color="1F497D", sz="6")
        
    # Data rows
    for row_idx, row_data in enumerate(data):
        row_cells = table.rows[row_idx + 1].cells
        bg_color = "F8F9FA" if row_idx % 2 == 1 else "FFFFFF"
        for col_idx, cell_value in enumerate(row_data):
            row_cells[col_idx].text = ""
            p = row_cells[col_idx].paragraphs[0]
            p.alignment = WD_ALIGN_PARAGRAPH.LEFT
            p.paragraph_format.space_before = Pt(2)
            p.paragraph_format.space_after = Pt(2)
            p.paragraph_format.line_spacing = 1.10
            
            # Format text
            r = p.add_run(str(cell_value))
            is_first_col = (col_idx == 0)
            format_run_font(r, "Times New Roman", font_size, bold=is_first_col, color_rgb=(0x22, 0x22, 0x22))
            
            set_cell_background(row_cells[col_idx], bg_color)
            set_cell_margins(row_cells[col_idx], top=45, bottom=45, left=80, right=80)
            set_cell_borders(row_cells[col_idx], bottom="single", color="E2E8F0", sz="4")
            
    # Apply column widths if specified
    if col_widths and len(col_widths) == len(headers):
        for row in table.rows:
            for i, w in enumerate(col_widths):
                row.cells[i].width = Inches(w)
                
    spacer = doc.add_paragraph()
    spacer.paragraph_format.space_before = Pt(0)
    spacer.paragraph_format.space_after = Pt(space_after)
    return table

def add_side_by_side_diagrams(doc, img_left, cap_left, title_left, desc_left,
                              img_right, cap_right, title_right, desc_right,
                              img_width=2.85, max_height_inches=5.8):
    table = doc.add_table(rows=1, cols=2)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    
    col_width = Inches(3.18)
    table.rows[0].cells[0].width = col_width
    table.rows[0].cells[1].width = col_width
    
    for i, cell in enumerate(table.rows[0].cells):
        set_cell_background(cell, "FFFFFF")
        set_cell_margins(cell, top=40, bottom=40, left=60, right=60)
        set_cell_borders(cell)
        
        p = cell.paragraphs[0]
        p.paragraph_format.space_before = Pt(0)
        p.paragraph_format.space_after = Pt(2)
        p.paragraph_format.line_spacing = 1.10
        
        title = title_left if i == 0 else title_right
        desc = desc_left if i == 0 else desc_right
        img = img_left if i == 0 else img_right
        cap = cap_left if i == 0 else cap_right
        
        r_title = p.add_run(f"{title}\n")
        format_run_font(r_title, "Times New Roman", 10.0, bold=True, color_rgb=(0x1F, 0x49, 0x7D))
        r_desc = p.add_run(f"{desc}\n")
        format_run_font(r_desc, "Times New Roman", 8.5, color_rgb=(0x44, 0x44, 0x44))
        
        if os.path.exists(img):
            actual_w = img_width
            try:
                from PIL import Image
                with Image.open(img) as im:
                    img_w, img_h = im.size
                    aspect = img_w / img_h
                    if (actual_w / aspect) > max_height_inches:
                        actual_w = max_height_inches * aspect
            except Exception:
                pass
                
            p_img = cell.add_paragraph()
            p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p_img.paragraph_format.space_before = Pt(2)
            p_img.paragraph_format.space_after = Pt(1)
            r_img = p_img.add_run()
            r_img.add_picture(img, width=Inches(actual_w))
            
            p_cap = cell.add_paragraph()
            p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p_cap.paragraph_format.space_before = Pt(1)
            p_cap.paragraph_format.space_after = Pt(2)
            r_cap = p_cap.add_run(cap)
            format_run_font(r_cap, "Times New Roman", 8.0, italic=True, color_rgb=(0x66, 0x66, 0x66))
            
    spacer = doc.add_paragraph()
    spacer.paragraph_format.space_before = Pt(0)
    spacer.paragraph_format.space_after = Pt(4)

def init_document(subtitle_text):
    doc = docx.Document()
    sec = doc.sections[0]
    sec.page_width = Inches(8.27)
    sec.page_height = Inches(11.69)
    sec.top_margin = Inches(0.75)
    sec.bottom_margin = Inches(0.75)
    sec.left_margin = Inches(1.0)
    sec.right_margin = Inches(0.8)
    add_header_footer(doc, subtitle_text)
    return doc

def build_arranged_document():
    print("Building document with updated Faculty, Course, and Visual Asset specifications...")
    doc = init_document("Objectives, Scope, Diagrams & Storyboards")
    
    # =========================================================================
    # PAGE 1: TITLE, ACADEMIC METADATA & EXECUTIVE SUMMARY (FITS 100% ON PAGE 1)
    # =========================================================================
    seal_path = "Docs/Report/figures/cover/psu-seal.png"
    if os.path.exists(seal_path):
        p_seal = doc.add_paragraph()
        p_seal.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_seal.paragraph_format.space_before = Pt(4)
        p_seal.paragraph_format.space_after = Pt(4)
        r_seal = p_seal.add_run()
        r_seal.add_picture(seal_path, width=Inches(1.1))
        
    add_styled_paragraph(doc, "Prince of Songkla University, Pattani Campus", 
                         font_name="Times New Roman", size_pt=11.5, bold=True, color_rgb=(0x44, 0x44, 0x44), 
                         align=WD_ALIGN_PARAGRAPH.CENTER, space_before=0, space_after=1)
    add_styled_paragraph(doc, "Faculty of Communication Science", 
                         font_name="Times New Roman", size_pt=10.5, bold=True, color_rgb=(0x1F, 0x49, 0x7D), 
                         align=WD_ALIGN_PARAGRAPH.CENTER, space_before=0, space_after=1)
    add_styled_paragraph(doc, "Computer and Informatics for Management (CIM) | สาขาวิชาคอมพิวเตอร์และวิทยาการสารสนเทศเพื่อการจัดการ", 
                         font_name="Times New Roman", size_pt=9.5, bold=False, color_rgb=(0x55, 0x55, 0x55), 
                         align=WD_ALIGN_PARAGRAPH.CENTER, space_before=0, space_after=8)
    
    add_styled_paragraph(doc, "LangkaLens: An Immersive Virtual Reality and Mixed Reality Cultural Heritage Platform for Ancient Yarang City", 
                         font_name="Times New Roman", size_pt=15, bold=True, color_rgb=(0x1F, 0x49, 0x7D), 
                         align=WD_ALIGN_PARAGRAPH.CENTER, space_before=2, space_after=3)
                         
    add_styled_paragraph(doc, "Project Objectives, System Scope, Updated System Diagrams, and Spatial Storyboards", 
                         font_name="Times New Roman", size_pt=11.5, bold=True, color_rgb=(0x2B, 0x57, 0x9A), 
                         align=WD_ALIGN_PARAGRAPH.CENTER, space_before=1, space_after=8)
                         
    # Metadata Table
    meta_headers = ["Project Specification Attribute", "Academic Credential & System Environment"]
    meta_data = [
        ["Author / Student Researcher", "Wansobree Hajihama (Student ID: 6620610023)"],
        ["Project Advisor", "Assistant Professor Dr. Zamree Che-aron (ผศ.ดร.ซัมรี เจ๊ะอารน)"],
        ["Course / Major", "Computer and Informatics for Management (CIM)\nสาขาวิชาคอมพิวเตอร์และวิทยาการสารสนเทศเพื่อการจัดการ"],
        ["Faculty / Institution", "Faculty of Communication Science (คณะวิทยาการสื่อสาร)\nPrince of Songkla University, Pattani Campus"],
        ["Target Platforms", "Meta Quest 3 & Meta Quest 3S (Horizon OS / Android 12L / API 32, 64-bit ARM64)"],
        ["Core Software Stack", "Unity 6000.3.16f1 LTS URP 17.3.0, Meta XR SDK v203.0.2, Addressables 2.9.1"],
        ["Reference Master Report", "Docs/Report/LangkaLens_Senior_Project_Report.docx (Approved Thesis Edition, Updated October 2026)"],
        ["Visual Asset Repository", "512 Multi-Format Assets Across 15 Domains (Docs/IMAGE_CATALOG.md)"],
        ["Academic Year", "2026 (2569 B.E.)"]
    ]
    create_styled_table(doc, meta_headers, meta_data, col_widths=[2.1, 4.3], font_size=8.0, space_after=4)
    
    add_callout(doc, 
        "Executive Summary & Seminar Progression Context:\n"
        "This specification document is compiled specifically for print and academic review. It consolidates the definitive Project Objectives, System Scope, updated Architectural/Activity/Sequence Diagrams (Figures 3.1 through 3.14), and comprehensive Spatial Storyboards for LangkaLens. "
        "These materials represent the latest, updated versions of the concepts originally presented during the preliminary seminar course last semester. Key updates include: transitioning from theoretical administrative portals to a focused standalone mobile VR/MR architecture; adopting session-scoped language persistence; upgrading Mixed Reality to an unrestricted boundary with persistent spatial session anchors (OVRSpatialAnchor); integrating an unscaled environment dimmer during artifact inspection; implementing an asynchronous speech-to-text pipeline (Groq Whisper) with an editable input field before Google Gemini AI query dispatch; and incorporating the complete 15-domain visual repository (512 assets) featuring 11 curated archaeological relics and 4 full-scale architectural reconstructions.",
        title="DOCUMENT PURPOSE & SEMINAR PROGRESSION SUMMARY", width_inches=6.4)
        
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 2: PART 1: PROJECT OBJECTIVES & TRACEABILITY MATRIX (FITS ON PAGE 2)
    # =========================================================================
    add_heading_1(doc, "Part 1: Project Objectives (วัตถุประสงค์ของโครงงาน)")
    
    add_styled_paragraph(doc, 
        "LangkaLens addresses a critical preservation and pedagogical challenge: the surviving archaeological remains of Ancient Yarang City "
        "(Pattani Province, Southern Thailand) consist primarily of low brick foundation baselines, earthen ramparts, and moats without surviving "
        "upper superstructures. Without interpreted spatial visualization, it is exceptionally difficult for students, educators, and the general "
        "public to conceptualize the monumental scale and architectural grandeur of 7th-century Dvaravati-era Southeast Asia.")
        
    add_styled_paragraph(doc, 
        "Based on consultation with official Fine Arts Department excavation monographs [1], [2] and technical evaluation in the senior thesis, "
        "the six formal project objectives are defined as follows:")
        
    objs = [
        ("Objective 1: Standalone Mobile XR Application Development",
         "Develop a standalone Virtual Reality (VR) and Mixed Reality (MR) cultural heritage application targeting the Meta Quest 3 and Meta Quest 3S head-mounted displays, providing an immersive, accessible experience without tethered PC requirements."),
        ("Objective 2: 360-Degree Panoramic Tour with In-Situ Reconstruction",
         "Provide an interactive 360-degree photographic tour across four key Yarang archaeological complexes (Ban Jalae 2, Ban Jalae 3, Ban Jalae 8, and Ban Wat 9), supporting intuitive waypoint wayfinding and on-demand in-situ 3D inspection of interpreted ancient stupa superstructures."),
        ("Objective 3: 6-DOF Virtual Museum & Artifact Exploration",
         "Allow visitors to freely inspect, rotate, and resize digitised photogrammetric 3D artifacts using natural 6-degree-of-freedom (6-DOF) spatial grabbing, accompanied by an automatic environment exposure dimmer to focus visual attention, and alternate mesh comparisons between weathered and restored archaeological forms."),
        ("Objective 4: On-Site Mixed Reality Alignment & Session Anchoring",
         "Provide an on-site Mixed Reality (MR) experience using high-definition stereoscopic color video passthrough, allowing curators and on-site visitors to align virtual entrance references to physical benchmarks and anchor full-scale 1:1 architectural reconstructions using hardware-tracked Meta spatial session anchors."),
        ("Objective 5: Bilingual Parity & Multimodal AI Heritage Guide",
         "Implement fully symmetrical bilingual Thai (th) and English (en) interfaces across all scenes via Unity Localization, paired with an integrated multimodal AI heritage guide supporting both typed questions and Groq Whisper speech transcription prior to dispatching prompts to Google Gemini."),
        ("Objective 6: Systematic Functional Verification & Simulator Testing",
         "Rigorously verify the application's stability, navigation transitions, memory deallocation, and interaction flows through structured functional checklists and Meta XR Simulator playtesting.")
    ]
    
    for title, desc in objs:
        p = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(2)
        p.paragraph_format.space_after = Pt(2)
        p.paragraph_format.line_spacing = 1.10
        r_t = p.add_run(f"• {title}: ")
        format_run_font(r_t, "Times New Roman", 9.0, bold=True, color_rgb=(0x1F, 0x49, 0x7D))
        r_d = p.add_run(desc)
        format_run_font(r_d, "Times New Roman", 8.5, color_rgb=(0x33, 0x33, 0x33))
        
    add_heading_2(doc, "Objectives & Engineering Deliverables Traceability Matrix")
    obj_table_headers = ["ID", "Project Objective", "Primary Subsystem & Controller", "Deliverable & Verification Standard"]
    obj_table_data = [
        ["OBJ-01", "Standalone VR/MR App", "OVRCameraRig / URP Forward+", "Standalone Android APK (Horizon OS), sustaining 72/90 FPS with Single-Pass Stereo."],
        ["OBJ-02", "VR Tour & Stupa Overlay", "TourManager.cs / Addressables", "4-area tour graph, 8K panoramas, Left Y 3D stupa inspection with async release."],
        ["OBJ-03", "VR Museum & Dimmer", "InspectionManager.cs / Dimmer", "Pedestal gallery, OneGrabFreeRotateTransformer, 0.18 exposure fade, dual-mesh toggle."],
        ["OBJ-04", "On-Site MR Alignment", "MRMeshReferenceAlignment.cs", "Color passthrough, right-controller reference gizmo, OVRSpatialAnchor session lock."],
        ["OBJ-05", "Bilingual AI Guide", "GeminiChatUI.cs / Groq Whisper", "100% Thai/EN StringTable parity, 16kHz speech transcription, Gemini REST chat with retry."],
        ["OBJ-06", "System Verification", "Meta XR Simulator / ADB", "14-case functional checklist, zero memory leaks (<2.5GB heap), zero unhandled exceptions."]
    ]
    create_styled_table(doc, obj_table_headers, obj_table_data, col_widths=[0.7, 1.8, 1.8, 2.1], font_size=8.0, space_after=2)
    
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 3: PART 2: SYSTEM SCOPE (FITS ON PAGE 3)
    # =========================================================================
    add_heading_1(doc, "Part 2: System Scope (ขอบเขตของโครงงานและระบบ)")
    
    add_styled_paragraph(doc, 
        "The system scope defines the exact boundaries, supported functionality, operational modes, target hardware specifications, "
        "and visual data schemas encompassed by LangkaLens, as finalized in Section 1.3 and Appendix A.7 of the updated senior project report:")
        
    scopes = [
        ("2.1 Application Lifecycle, Onboarding & Spatial Accessibility Scope",
         "• Centralized Hub-and-Spoke Lifecycle: Application boot loads Main_Menu_Scene. Visitors choose between Thai or English and select one of three destination scenes (VR Tour, VR Museum, MR Mode).\n"
         "• Session-Scoped Language Persistence: Language selection is recorded for the active runtime session. Returning to the main menu preserves language without redundant prompts; restarting the app begins a clean session.\n"
         "• Accessibility & Ergonomics: World-space UI canvases positioned at comfortable optical distances (1.2m to 2.0m) within the natural 30°-60° field of view to eliminate vergence-accommodation conflict. Universal one-handed locomotion prevents cybersickness."),
        
        ("2.2 VR 360-Degree Panoramic Tour Scope & Historic Monuments",
         "• Archaeological Monument Coverage: Four major monument complexes: Ban Jalae 2 (Monument No. 2, stupa baseline sanctuary), Ban Jalae 3 (Monument No. 3, tiered terracotta stupa base), Ban Jalae 8 (Monument No. 8, central mandapa complex), and Ban Wat 9 (Monument No. 9, moated shrine).\n"
         "• Wayfinding Navigation Graph: Bidirectional node traversal driven by ScriptableObjects (TourArea, TourNode, NodeConnection) covering 91 equirectangular 8K photographic nodes.\n"
         "• Addressables Memory Lifecycle: 8K textures load asynchronously via Unity Addressables; previous texture handles are disposed immediately upon arrival to conserve mobile VRAM.\n"
         "• In-Situ 3D Stupa Inspection: Viewpoints allow pressing Left Controller Button Y to instantiate a full-scale 3D interpreted reconstruction stupa with localized historical metadata."),
        
        ("2.3 VR Virtual Museum Scope & 11 Curated Archaeological Relics",
         "• Curated 11-Relic Roster: Complete photogrammetric 3D scans: (1) Standing Buddha (ศิลปะทวารวดี), (2) Nandi Sacred Bull (โคนันทิ), (3) Carved Stone Pillar (เสาหินสลัก), (4) Ancient Deity Statue (เทวรูปโบราณ), (5-8) Stupa Model Types 1–4 (สถูปจำลอง แบบที่ 1-4), (9) Surya/Trinity Stele (แผ่นสลักสุริยเทพ/ตรีมูรติ), (10) Kurma Sacred Turtle (เต่ามงคล), and (11) Dharmachakra Wheel (ธรรมจักรศิลา).\n"
         "• 6-DOF Natural Manipulation: Powered by Meta Interaction SDK (OneGrabFreeRotateTransformer). Visitors grab, rotate 360°, and hold buttons A/B to smoothly scale objects.\n"
         "• Unscaled Environment Exposure Dimmer: Selecting an exhibit initiates an exposure fade on surrounding gallery renderers (0.18 multiplier over 0.35s), isolating the illuminated artifact.\n"
         "• Dual-Mesh Comparison: Supported exhibits allow toggling between the weathered excavation state (_ORG) and an archaeologically restored visual form (_RES)."),
        
        ("2.4 On-Site Mixed Reality Scope",
         "• Stereoscopic Color Passthrough: Utilizes Meta Quest 3/3S high-definition RGB cameras to blend digital monuments directly into the physical archaeological park.\n"
         "• Curator Physical-to-Virtual Alignment: Curators position a virtual bounding entrance box over an on-site physical sign/table using right controller grip and thumbstick controls.\n"
         "• Session Spatial Anchoring: Confirming alignment creates a hardware OVRSpatialAnchor. The resulting spatial matrix locks the 1:1 scale reconstructed monument in physical space for the session.\n"
         "• Unrestricted Footprint Mode: Adopts an open, unrestricted movement volume matching open-air historical parks.\n"
         "• Tracking Loss Concealment: Model visibility is continuously gated by head tracking stability and passthrough status, preventing disorienting visual drift."),
        
        ("2.5 Multimodal AI Heritage Guide Scope",
         "• Omnipresent Access: Accessible across all scenes via a persistent wrist tablet / submenu.\n"
         "• Dual Input Modes: Supports direct text typing via a bilingual virtual keyboard or voice recording via Unity Microphone.\n"
         "• Speech-to-Text Pipeline: 16kHz PCM audio WAV is streamed to Groq Cloud (Whisper-large-v3) and populates an editable input field, enabling users to verify speech before submitting.\n"
         "• Grounded Gemini Dialogue: Sends questions to Google Gemini REST API (gemini-1.5-flash / gemini-2.0-flash) with curated archaeological system instructions, automatic retry, and fallback model routing."),
        
        ("2.6 Hardware, OS, Toolchain & Visual Asset Repository Scope",
         "• Development Workstation: HP ZBook Fury 15.6 G8 Mobile Workstation (Intel Core i7-11800H, 64 GB RAM, NVIDIA RTX A4000 Laptop GPU, Windows 11 Pro 64-bit).\n"
         "• Target Headsets: Meta Quest 3 and Meta Quest 3S running Meta Horizon OS (Android 12L / API 32, 64-bit ARM64 architecture).\n"
         "• Core Engine Stack: Unity 6000.3.16f1 LTS, URP 17.3.0, Meta XR SDK v203.0.2, Addressables 2.9.1, Unity Localization 1.5.12.\n"
         "• Visual Asset Inventory: Structured repository containing 512 files across 15 domains (1.72 GB total volume), documented in Docs/IMAGE_CATALOG.md.")
    ]
    
    for title, desc in scopes:
        add_heading_3(doc, title)
        p = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(1)
        p.paragraph_format.space_after = Pt(2)
        p.paragraph_format.line_spacing = 1.08
        r = p.add_run(desc)
        format_run_font(r, "Times New Roman", 8.0, color_rgb=(0x33, 0x33, 0x33))
        
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 4: FORMAL REQUIREMENTS SPECIFICATIONS MATRIX (FITS ON PAGE 4)
    # =========================================================================
    add_heading_2(doc, "Formal Requirements Specifications Matrix (FR-01 to FR-08 & NFR-01 to NFR-06)")
    add_styled_paragraph(doc, 
        "The requirements engineering matrix below formalizes the functional behaviors and non-functional engineering standards "
        "governing LangkaLens, verified against the physical Meta Quest 3 and Quest 3S headsets:")
        
    req_headers = ["Req ID", "Requirement Classification", "Functional Scope & Implementation", "Pass / Acceptance Standard"]
    req_data = [
        ["FR-01", "Bilingual Locale Switching", "MainMenuManager; Unity Localization 1.5.12", "Session persistence; dynamic label updates; zero untranslated text."],
        ["FR-02", "360° Viewpoint Navigation", "TourManager; TourNode; Addressables 2.9.1", "Smooth quadratic translation; instant deallocation of prior textures."],
        ["FR-03", "VR Reconstruction Inspection", "TourReconstructionPresenter; InspectionManager", "Left Y spawns 3D stupa; 6-DOF rotation and scaling; localized info panel."],
        ["FR-04", "VR Museum Artifact 6-DOF", "ArtifactData; OneGrabFreeRotate; Dimmer", "Pedestal exhibit grab; 0.18 environment fade; dual-mesh toggle support."],
        ["FR-05", "MR Entrance Alignment", "MRMeshReferenceAlignment; OVRSpatialAnchor", "Curator gizmo alignment; hardware anchor generation within 15 seconds."],
        ["FR-06", "MR Display Gating", "MRMeshPlayableArea; Passthrough Buffers", "Automatic concealment upon tracking degradation; unrestricted park volume."],
        ["FR-07", "Multimodal AI Heritage Guide", "GeminiChatUI; Google Gemini REST API", "Context-grounded prompt injection; automatic fallback on HTTP 429/5xx."],
        ["FR-08", "Voice Speech-to-Text", "SpeechToTextHandler; Groq Whisper Cloud", "16kHz WAV streaming; editable text field population before prompt dispatch."],
        ["NFR-01", "Performance & Frame Rate", "URP Forward+; Single-Pass Instanced Stereo", "Rock-solid 72/90 FPS; frame time <= 13.8 ms; draw calls <= 120 per eye."],
        ["NFR-02", "Memory Footprint", "Addressables.Release(); ASTC 6x6 Textures", "Peak heap allocation < 2.5 GB; zero VRAM leaks during scene traversal."],
        ["NFR-03", "Spatial Ergonomics", "World-Space Canvas; 1.2m-2.0m Distance", "Eliminates vergence-accommodation conflict; comfort FOV between 30°-60°."],
        ["NFR-04", "Cybersickness Mitigation", "Universal One-Handed Locomotion System", "Zero involuntary camera motion; instantaneous snap-turning options."],
        ["NFR-05", "Network Fault Resilience", "UnityWebRequest Coroutines; Retry Handlers", "Graceful non-blocking error display on offline status or API timeouts."],
        ["NFR-06", "Bilingual Symmetry", "Parallel StringTable Collections (Thai / EN)", "100% lexical symmetry across UI buttons, descriptions, and guide prompts."]
    ]
    create_styled_table(doc, req_headers, req_data, col_widths=[0.7, 1.8, 1.9, 2.0], font_size=8.0, space_after=4)
    
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 5: PART 3: ARCHITECTURE & FIGURE 3.1 (FITS ON PAGE 5)
    # =========================================================================
    add_heading_1(doc, "Part 3: Updated System Diagrams (แผนภาพระบบฉบับปรับปรุงล่าสุด)")
    
    add_styled_paragraph(doc, 
        "During the preliminary seminar course last semester, initial conceptual diagrams were presented to illustrate the early vision of LangkaLens. "
        "As development matured and technical evaluations were conducted on the physical Meta Quest 3 hardware, the system design underwent significant refinement. "
        "All 14 diagrams below reflect the final approved architecture documented in Chapter 3 of the thesis report, also available as an editable multi-page Draw.io vector suite (Docs/Report/LangkaLens_All_Diagrams.drawio).")
        
    add_heading_2(doc, "3.1 High-Level System Architecture Diagram")
    add_styled_paragraph(doc, 
        "The updated system architecture follows a clean 4-tier layered model that enforces separation of concerns across presentation, "
        "domain logic, hardware abstraction, and external services:\n"
        "• Tier 1 (Presentation & Spatial UX): World-space Canvas hierarchies, TextMesh Pro distance-field rendering, Meta XR Color Passthrough, and 3D spatialized audio cues.\n"
        "• Tier 2 (Application & Domain Logic): Core state controllers (MainMenuManager, TourManager, InspectionManager, InspectionEnvironmentDimmer, MRMeshReferenceAlignment, GeminiChatUI).\n"
        "• Tier 3 (Engine & XR Hardware Abstraction): Unity 6 Engine and Meta XR All-In-One SDK (v203.0.2), handling 6-DOF SLAM tracking, optical passthrough buffers, spatial anchor hardware, and Touch Plus controller events.\n"
        "• Tier 4 (Data Persistence & Cloud Microservices): ScriptableObjects and Addressable AssetBundles paired with external REST endpoints for Google Gemini and Groq Whisper.")
        
    add_image_box(doc, "Docs/Report/figures/diagrams/figure_3_1.png", 
                  caption="Figure 3.1: Updated High-Level System Architecture and External Services Topology", width_inches=5.4)
                  
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 6: ACTOR PROFILES, USE CASES & FIGURE 3.2 (FITS ON PAGE 6)
    # =========================================================================
    add_heading_2(doc, "3.2 System Actor Profiles & Use Case Diagram")
    add_styled_paragraph(doc, 
        "LangkaLens identifies three primary system actors with distinct operational scopes:\n"
        "1. Cultural Heritage Visitor / Student (Primary Actor): Explores 360° panoramas, examines 3D reconstructed stupas, inspects and manipulates museum artifacts in 6-DOF, explores on-site MR reconstructions, and converses with the AI Heritage Guide.\n"
        "2. Site Curator / Field Researcher (Secondary Actor): Calibrates physical-to-virtual coordinate frames using the entrance alignment tool, establishes hardware session anchors, and verifies spatial tracking integrity.\n"
        "3. External AI Microservices (Cloud Actor): Processes 16kHz speech streams into text (Groq Whisper) and generates contextual archaeological responses (Google Gemini).")
        
    actor_headers = ["System Actor", "Operational Classification", "Core Functional Responsibilities"]
    actor_data = [
        ["Cultural Heritage Visitor", "Primary End-User", "Viewpoint navigation, 6-DOF artifact manipulation, AI heritage inquiries."],
        ["Field Curator / Researcher", "Administrative Operator", "Entrance reference gizmo alignment, OVRSpatialAnchor anchoring, site verification."],
        ["External AI Microservices", "Cloud Intelligence Endpoint", "Asynchronous 16kHz audio transcription and contextual LLM prompt responses."]
    ]
    create_styled_table(doc, actor_headers, actor_data, col_widths=[1.7, 1.8, 2.9], font_size=8.0, space_after=2)
    
    add_image_box(doc, "Docs/Report/figures/diagrams/figure_3_2.png", 
                  caption="Figure 3.2: Updated System Functions and Use Case Diagram for Visitors and Curators", width_inches=4.2)
                  
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 7: ACTIVITY FLOWCHARTS: FIG 3.3 & FIG 3.4 (FITS ON PAGE 7)
    # =========================================================================
    add_heading_2(doc, "3.3 System Workflow & Behavioral Activity Flowcharts")
    
    add_heading_3(doc, "3.3.1 Application Lifecycle & Scene Navigation Flowchart")
    add_styled_paragraph(doc, 
        "Governs the hub-and-spoke execution pattern. Launching initializes Main_Menu_Scene. Visitors choose between Thai or English and route into any destination scene. Return requests prompt confirmation before restoring the main menu and releasing mode-specific assets.")
    add_image_box(doc, "Docs/Report/figures/diagrams/figure_3_3.png", 
                  caption="Figure 3.3: Application Overview and Scene Navigation Flowchart", width_inches=4.8, max_height_inches=2.6)
                  
    doc.add_page_break()
    
    add_heading_3(doc, "3.3.2 Main Menu Language Selection & Scene Loading Flowchart")
    add_styled_paragraph(doc, 
        "Governs session-scoped language persistence and async scene transitions. When launched, MainMenuManager checks HasSelectedLanguage in session memory. If unconfirmed, the bilingual modal displays and visitor selection updates LocalizationSettings.SelectedLocale. Clicking Confirm remembers the choice and displays mode buttons. Clicking a mode button validates SceneLoadingScreen.IsLoading (ignoring double-clicks), invokes BeginContentLoading(), and calls SceneManager.LoadSceneAsync().")
    add_image_box(doc, "Docs/Report/figures/diagrams/figure_3_4.png", 
                  caption="Figure 3.4: Main Menu Language Selection and Scene Loading Flowchart", width_inches=5.0, max_height_inches=5.8)
                  
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 8: ACTIVITY FLOWCHARTS: FIG 3.5 & FIG 3.6 (SIDE-BY-SIDE ON PAGE 8)
    # =========================================================================
    add_heading_2(doc, "VR Tour Viewpoint Navigation & Reconstruction Inspection Flowcharts")
    add_styled_paragraph(doc, 
        "The two flowcharts below illustrate the core operational logic of the VR Panoramic Tour: viewpoint wayfinding via Addressables "
        "and contextual 3D reconstructed stupa inspection:")
        
    add_side_by_side_diagrams(
        doc,
        img_left="Docs/Report/figures/diagrams/figure_3_5.png",
        cap_left="Figure 3.5: VR Tour Viewpoint Traversal",
        title_left="3.3.3 VR 360 Panoramic Navigation",
        desc_left="Selecting a NavigationArrow checks IsNavigationBlocked (suppressing transitions, open map, or active inspection) and validates Addressable runtime keys. It locks navigation, requests the 8K equirectangular texture via Addressables.LoadAssetAsync<Texture2D>(), animates a 3m camera lunge with 3x sphere stretch, verifies load status within a 30s deadline, updates material.mainTexture, disposes of the previous texture handle from GPU VRAM, eases out camera rig, instantiates ArrowPlacementHelper, and unlocks navigation.",
        img_right="Docs/Report/figures/diagrams/figure_3_6.png",
        cap_right="Figure 3.6: Tour Reconstruction Inspection",
        title_right="3.3.4 Tour Reconstruction Inspection",
        desc_right="Pressing Left Controller Button Y invokes TourReconstructionPresenter.ToggleInspection(). If already inspecting, it fades panorama backdrop back to 1.0, destroys the model instance, and releases the Addressables handle. If opening, it creates a staging parent, calls Addressables.InstantiateAsync(inspectionPrefab), places model at 0.8m distance, dims backdrop to 0.15 with #533A2D tint, attaches OneGrabFreeRotateTransformer (0.5x-3.0x zoom), displays the bilingual tablet, and handles clean teardown upon exit.",
        img_width=2.85,
        max_height_inches=4.2
    )
    
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 9: ACTIVITY FLOWCHARTS: FIG 3.7 & FIG 3.8 (DEDICATED PAGES)
    # =========================================================================
    add_heading_3(doc, "3.3.5 VR Museum Artifact Exploration & Environment Dimmer Flowchart")
    add_styled_paragraph(doc, 
        "Raycasting an artifact pedestal verifies ArtifactData, pauses visitor locomotion, and triggers EnvironmentDimmer.Begin(0.18, 0.35s) to fade surrounding museum geometry. The relic glides to 0.6m eye distance with OneGrabFreeRotateTransformer. The inspection canvas tablet binds localized strings and displays a 'Switch Form' toggle button if alternateModel is present to switch between weathered original (_ORG) and digitally restored (_RES) meshes. Closing smoothly restores gallery lighting, returns the model to its pedestal, and resumes locomotion.")
    add_image_box(doc, "Docs/Report/figures/diagrams/figure_3_7.png", 
                  caption="Figure 3.7: VR Museum Artifact Exploration and Environment Dimmer Flowchart", width_inches=5.4, max_height_inches=2.8)
                  
    doc.add_page_break()
    
    add_heading_3(doc, "3.3.6 Mixed Reality Entrance Alignment & Session Anchor Flowchart")
    add_styled_paragraph(doc, 
        "Verifies Meta Quest 6-DOF head and Touch Plus controller tracking. A 2-second hold of Left X clears existing anchors to restart alignment. The curator positions the virtual entrance wireframe outline using the right grip, adjusting rotation yaw (45 deg/s) and reach distance (0.15m-3.0m) with the thumbstick. Pressing button A invokes OVRSpatialAnchor.CreateAsync() with a 15-second timeout window. Upon localization, the relative transformation matrix locks, and MRModelSpawner.SpawnAlignedModel() instantiates the site reconstruction.")
    add_image_box(doc, "Docs/Report/figures/diagrams/figure_3_8.png", 
                  caption="Figure 3.8: On-Site Mixed Reality Entrance Alignment and Session Anchor Flowchart", width_inches=4.8, max_height_inches=6.2)
                  
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 10: ACTIVITY FLOWCHARTS: FIG 3.9 & FIG 3.10 (DEDICATED PAGES)
    # =========================================================================
    add_heading_3(doc, "3.3.7 MR Model Display Gating & Tracking Recovery Flowchart")
    add_styled_paragraph(doc, 
        "Executing in unrestricted footprint mode (allowing free visitor walking), runtime display requires alignment confirmation, active 6-DOF head tracking, spatial anchor localization, and active color passthrough. If tracking quality drops or passthrough is interrupted, virtual monuments and UI panels suppress immediately to prevent drift. Curators can toggle the 3D monument via button A, toggle the site information tablet via button B, or hold Left X for 2 seconds to trigger realignment.")
    add_image_box(doc, "Docs/Report/figures/diagrams/figure_3_9.png", 
                  caption="Figure 3.9: MR Model Display Gating and Tracking Recovery Flowchart", width_inches=5.4, max_height_inches=3.0)
                  
    doc.add_page_break()
    
    add_heading_3(doc, "3.3.8 Shared Multimodal AI Heritage Guide Workflow Flowchart")
    add_styled_paragraph(doc, 
        "Visitors submit questions via either 16kHz PCM audio microphone recording (transcribed via Groq Cloud Whisper API whisper-large-v3 with runtime Android permission checks) or bilingual virtual keyboard. The transcribed text is placed into an editable inputField for review. Clicking Send dispatches gemini-3.5-flash-lite with contextual archaeological prompts (150 max output tokens). On HTTP 404, 408, 429, or 5xx errors, an automatic fallback to gemini-3.1-flash-lite executes before reporting an error.")
    add_image_box(doc, "Docs/Report/figures/diagrams/figure_3_10.png", 
                  caption="Figure 3.10: Shared Multimodal AI Heritage Guide Workflow Flowchart", width_inches=4.6, max_height_inches=6.4)
                  
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 11: SEQUENCE DIAGRAMS: FIG 3.11 & FIG 3.12 (FITS ON PAGE 11)
    # =========================================================================
    add_heading_2(doc, "3.4 Process Sequence Diagrams (Dynamic Component Interactions)")
    
    add_heading_3(doc, "3.4.1 Process 1: Tour Viewpoint Transition & Asynchronous Asset Loading")
    add_styled_paragraph(doc, 
        "Chronicles the interaction between TourManager, Addressables, and URP materials. When an arrow indicator is clicked, input is disabled, the new 8K texture handle is requested, camera translation begins, the old texture handle is deallocated via Addressables.Release(), and indicators are refreshed.")
    add_image_box(doc, "Docs/Report/figures/diagrams/figure_3_11.png", 
                  caption="Figure 3.11: Sequence Diagram: Asynchronous Tour Viewpoint Transition and Memory Cleanup", width_inches=4.9)
                  
    add_heading_3(doc, "3.4.2 Process 2: Museum Artifact Inspection & Dimmer Exposure Lifecycle")
    add_styled_paragraph(doc, 
        "Documents the collaboration between InspectionManager, OneGrabFreeRotateTransformer, and InspectionEnvironmentDimmer. When an exhibit is grabbed, player locomotion is suspended, gallery renderers fade to 18% brightness, and localized metadata cards open. Closing restores full lighting.")
    add_image_box(doc, "Docs/Report/figures/diagrams/figure_3_12.png", 
                  caption="Figure 3.12: Sequence Diagram: Museum Artifact Inspection and Dimmer Lifecycle", width_inches=4.9)
                  
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 12: SEQUENCE DIAGRAMS: FIG 3.13 & FIG 3.14 (FITS ON PAGE 12)
    # =========================================================================
    add_heading_3(doc, "3.4.3 Process 3: MR Spatial Alignment & Hardware Session Anchoring")
    add_styled_paragraph(doc, 
        "Illustrates the hardware synchronization between MRMeshReferenceAlignment, OVRSpatialAnchor, and MRModelSpawner. Bounding gizmo adjustments yield a relative transform, which is bound to an active Meta spatial anchor upon button confirmation.")
    add_image_box(doc, "Docs/Report/figures/diagrams/figure_3_13.png", 
                  caption="Figure 3.13: Sequence Diagram: MR Entrance Alignment and Spatial Anchoring", width_inches=4.6)
                  
    add_heading_3(doc, "3.4.4 Process 4: Multimodal AI Heritage Guide (Speech & LLM Synthesis)")
    add_styled_paragraph(doc, 
        "Details the decoupled cloud interaction: speech recording triggers a POST request to Groq Whisper, populating the local input field. Upon Send, GeminiChatUI dispatches the prompt with injected archaeological context. If an HTTP 429/5xx error occurs, a single-attempt fallback model is automatically invoked.")
    add_image_box(doc, "Docs/Report/figures/diagrams/figure_3_14.png", 
                  caption="Figure 3.14: Sequence Diagram: Multimodal AI Guide Speech Transcription and Gemini LLM Query", width_inches=4.7)
                  
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 13: PART 4: STORYBOARDS MASTER SPECIFICATION MATRIX (FITS ON PAGE 13)
    # =========================================================================
    add_heading_1(doc, "Part 4: Updated System Storyboards & Spatial Interaction Flows (สตอรี่บอร์ดและผังการปฏิสัมพันธ์)")
    
    add_styled_paragraph(doc, 
        "Spatial computing interfaces on the Meta Quest 3 and Quest 3S require intuitive 3D spatial user experience (UX) design. "
        "Unlike flat mobile screens, LangkaLens utilizes world-space floating canvases, physical 6-DOF controller manipulation, "
        "optical video passthrough, and distance-scaled TextMesh Pro typography. "
        "The updated storyboards below represent the complete, 7-scene user journey across the application:")
        
    add_heading_2(doc, "4.1 Master Storyboard Specification Matrix")
    
    sb_headers = ["#", "Scene / Experience", "Visual Environment", "Spatial UI & Components", "User Triggers & Controls", "Audio & Haptic Feedback", "C# Controller Link"]
    sb_data = [
        ["01", "Language Portal", "Terracotta atmospheric void with golden dust motes", "Curved floating modal (800x520px) with Thai/English flag cards and Confirm CTA", "Raycast point & index trigger click on language card", "Subtle ambient hum; crisp tactile haptic pulse on click", "MainMenuManager.cs / Unity Localization"],
        ["02", "Main Menu Hub", "3D sunset panorama overlooking Yarang ruins", "4 Glassmorphic Portal Cards: VR Tour, VR Museum, MR Mode, AI Guide", "Raycast hover & trigger pull on chosen experience card", "Soft evening wind; gentle haptic buzz on card hover", "MainMenuManager.cs / SceneManager"],
        ["03", "VR 360 Tour", "8K photosphere of Ban Jalae Site (BN2, BN3, BN8, BW9)", "3D floating directional chevrons on walking paths + Mini-Map tablet", "Aim controller ray at forward chevron & click trigger", "Tropical bird ambience; forward spatial warp sound", "TourManager.cs / Addressables"],
        ["04", "3D Stupa Overlay", "Photospheric ruin with superimposed 3D stupa", "Reconstructed 7th-century Dvaravati Stupa + Spatial historical data card", "Press Left Controller Button Y at configured viewpoints", "Sacred temple gong chime; golden bell shimmer", "TourReconstructionPresenter.cs"],
        ["05", "VR Museum", "Modern gallery with spotlighted pedestals", "Pedestals with 3D relics; anchored metadata card; unscaled dimmer", "Grip to grab artifact; thumbstick/buttons A/B to scale; Y to toggle alt model", "Soft museum footsteps; haptic snap upon relic grab", "InspectionManager.cs / Dimmer.cs"],
        ["06", "On-Site MR", "Real-world archaeological park via Color Passthrough", "1:1 Reconstructed brick stupa + Curator bounding reference box", "Right Grip + Stick to align outline; Button A to anchor; B for info", "Spatial room reverberation; tactile click on anchor lock", "MRMeshReferenceAlignment.cs / Passthrough"],
        ["07", "Multimodal AI Guide", "Floating glassmorphic assistant tablet interface", "Dedicated tablet window with chat history, keyboard, and mic controls", "Speak question via mic or type via virtual keyboard; click Send", "Digital chime; typewriter audio on response stream", "GeminiChatUI.cs / Groq Whisper"]
    ]
    create_styled_table(doc, sb_headers, sb_data, col_widths=[0.3, 1.0, 1.2, 1.3, 1.0, 1.0, 0.6], font_size=7.5, space_after=2)
    
    add_callout(doc,
        "Master Storyboard Architectural Synopsis:\n"
        "The 7 core storyboard scenes correspond directly to the modular Unity scene hierarchy and state controllers. "
        "Each mode is self-contained: memory is isolated, rendering settings are tuned per mode (URP Forward+ in VR, Passthrough Buffers in MR), "
        "and external services (Gemini/Groq) operate asynchronously without impeding the 72/90 FPS rendering thread.",
        title="SPATIAL STATE DECOUPLING PRINCIPLE", width_inches=6.4)
        
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 14: STORYBOARD FRAMES 1 & 2 (FITS ON PAGE 14)
    # =========================================================================
    add_heading_2(doc, "4.2 Step-by-Step Storyboard Walkthrough & Interface Design")
    
    # Frame 1
    add_heading_3(doc, "Storyboard Frame 1: Language Onboarding Portal")
    add_styled_paragraph(doc, 
        "• Visual Scene: Atmospheric dark void with subtle rising golden dust motes.\n"
        "• UI Architecture: Curved floating modal (800x520px) suspended at 1.4m optical distance. Presents high-contrast Thai and English flag cards.\n"
        "• User Interaction: The visitor aims the controller ray pointer at their preferred language card and pulls the index trigger. The selected card highlights in Dvaravati Gold (#E5A93C), unlocking the 'Confirm & Continue' button.\n"
        "• System Implementation: MainMenuManager.cs initialises the selected locale code ('th' or 'en') via Unity Localization StringTable collections and records session confirmation.")
    add_image_box(doc, "Docs/User_Guide_Pictures/01_GettingStarted_PointAndSelect.jpg", 
                  caption="Storyboard Frame 1: Language Onboarding Portal & Controller Raycast Selection", width_inches=3.6)
                  
    # Frame 2
    add_heading_3(doc, "Storyboard Frame 2: Spatial Heritage Hub (Main Menu Navigation)")
    add_styled_paragraph(doc, 
        "• Visual Scene: High-resolution 3D twilight panorama overlooking the ancient brick stupa foundations of Ban Jalae at sunset.\n"
        "• UI Architecture: 4-card horizontal glassmorphic portal grid: [1] VR 360 Tour, [2] VR Museum Gallery, [3] Mixed Reality Mode, [4] AI Heritage Guide.\n"
        "• User Interaction: Pointing the raycast at any portal card triggers depth elevation and haptic pulse. Clicking loads the destination scene.\n"
        "• System Implementation: MainMenuManager.cs routes the selection to SceneManager.LoadSceneAsync with garbage collection.")
    add_image_box(doc, "Docs/User_Guide_Pictures/02_GettingStarted_ChooseExperience.jpg", 
                  caption="Storyboard Frame 2: Spatial Heritage Hub (Main Menu Navigation)", width_inches=3.6)
                  
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 15: STORYBOARD FRAME 3 & 4-MONUMENT ARCHITECTURAL RECONSTRUCTIONS (FITS ON PAGE 15)
    # =========================================================================
    # Frame 3
    add_heading_3(doc, "Storyboard Frame 3: VR 360-Degree Panoramic Tour & Wayfinding")
    add_styled_paragraph(doc, 
        "• Visual Scene: Immersive 8K equirectangular photosphere of Ancient Yarang excavation complexes (Ban Jalae 2, 3, 8, or Ban Wat 9).\n"
        "• UI Architecture: 3D animated directional chevrons anchored on walking paths. A floating tablet provides an interactive site map for rapid zone transitions.\n"
        "• User Interaction: The visitor aims at a directional chevron and clicks the index trigger. The system executes a smooth quadratic translation to the arrival node.\n"
        "• System Implementation: TourManager.cs parses TourNode graphs, loads new textures via Unity Addressables, and calls Addressables.Release() on prior textures.")
    add_image_box(doc, "Docs/User_Guide_Pictures/12_VRTour_AimAtNavigationArrow.jpg", 
                  caption="Storyboard Frame 3: VR 360-Degree Panoramic Tour & Wayfinding Chevrons", width_inches=3.3)
                  
    # 4-Monument Reconstructions Plate
    add_heading_3(doc, "3D Interpreted Architectural Reconstructions of Ancient Yarang Monuments")
    add_styled_paragraph(doc, 
        "LangkaLens incorporates full-scale architectural reconstructions of interpreted Dvaravati religious monuments: (a) Ban Ja-Lae Site 2 (stupa sanctuary), (b) Ban Ja-Lae Site 3 (tiered terracotta stupa base), (c) Ban Ja-Lae Site 8 (mandapa complex), and (d) Ban Wat Site 9 (moated foundation shrine).")
    add_image_box(doc, "Docs/Report/figures/reconstructions_grid_4monuments.png", 
                  caption="Architectural Reconstruction Plate: 3D interpreted monuments of Ban Ja-Lae 2, Ban Ja-Lae 3, Ban Ja-Lae 8, and Ban Wat 9", width_inches=4.3)
                  
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 16: STORYBOARD FRAME 4 & FRAME 5 WITH ARTIFACT COMPARISON PLATE (FITS ON PAGE 16)
    # =========================================================================
    # Frame 4
    add_heading_3(doc, "Storyboard Frame 4: In-Situ 3D Stupa Architectural Reconstruction Overlay")
    add_styled_paragraph(doc, 
        "• Visual Scene: Standing directly before the surviving brick foundation of Monument No. 3 (Ban Jalae).\n"
        "• UI Architecture: A 1:1 scale interpreted 3D model of the 7th-century Dvaravati Stupa appears superimposed directly over the ruin foundation, paired with an anchored historical description panel.\n"
        "• User Interaction: Pressing Left Controller Button Y toggles the 3D stupa. The right thumbstick allows smooth vertical rotation of the model for comprehensive inspection.\n"
        "• System Implementation: TourReconstructionPresenter.cs instantiates the reconstruction prefab via Addressables.")
    add_image_box(doc, "Docs/User_Guide_Pictures/15_VRTour_EnterReconstructionInspection.jpg", 
                  caption="Storyboard Frame 4: In-Situ 3D Stupa Architectural Reconstruction Overlay", width_inches=3.3)
                  
    # Frame 5 with Dual-Mesh Comparison Plate
    add_heading_3(doc, "Storyboard Frame 5: VR Museum Artifact Examination & Dual-Mesh Comparison")
    add_styled_paragraph(doc, 
        "• Visual Scene: Virtual museum gallery with pedestals illuminating 3D scans of excavated relics. InspectionEnvironmentDimmer fades ambient gallery lighting to 18%.\n"
        "• Dual-Mesh Comparison: Visitors press Left Button Y to dynamically toggle between the weathered excavation state (_ORG) and an archaeologically restored visual form (_RES), such as the Standing Buddha in Vitarka Mudra shown below:")
    add_image_box(doc, "Docs/Images/02_Artifact_Comparisons/Buddha_Comparison.png", 
                  caption="Artifact Comparison Plate: Standing Buddha (พระพุทธรูปยืนศิลปะทวารวดี) — Excavated Weathered Scan (_ORG, Left) vs 3D Restored Form (_RES, Right)", width_inches=4.3)
                  
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 17: STORYBOARD FRAMES 6 & 7 (FITS ON PAGE 17)
    # =========================================================================
    # Frame 6
    add_heading_3(doc, "Storyboard Frame 6: On-Site Mixed Reality Passthrough & Spatial Alignment")
    add_styled_paragraph(doc, 
        "• Visual Scene: Live real-world archaeological park captured through Meta Quest 3 full-color high-definition stereoscopic video passthrough.\n"
        "• UI Architecture: Virtual entrance reference outline matching physical sign/pedestal, accompanied by an alignment HUD and site evidence panel.\n"
        "• User Interaction: The curator grabs the virtual reference gizmo with the right grip, matches it to the physical sign, and presses Button A to lock a hardware spatial anchor.\n"
        "• System Implementation: MRMeshReferenceAlignment.cs and Meta OVRSpatialAnchor API manage coordinate transformation and session locking.")
    add_image_box(doc, "Docs/User_Guide_Pictures/07_MR_ReleaseGripAndConfirm.jpg", 
                  caption="Storyboard Frame 6: On-Site Mixed Reality Passthrough Alignment & Session Anchoring", width_inches=3.4)
                  
    # Frame 7
    add_heading_3(doc, "Storyboard Frame 7: Independent Multimodal AI Heritage Assistant")
    add_styled_paragraph(doc, 
        "• Visual Scene: Floating spatial tablet interface accessible across all experiential modes via a persistent submenu.\n"
        "• UI Architecture: Modern glassmorphic chat window displaying rich conversation history, typing bar, virtual bilingual keyboard, and microphone recording button.\n"
        "• User Interaction: Visitors type questions or record speech. Speech is transcribed by Groq Whisper into an editable input field before submitting to Google Gemini.\n"
        "• System Implementation: GeminiChatUI.cs manages RESTful chat requests with prompt grounding; SpeechToTextHandler.cs encodes 16kHz PCM WAV audio chunks.")
    add_image_box(doc, "Docs/User_Guide_Pictures/27_AIGuide_TypeAndSendQuestion.jpg", 
                  caption="Storyboard Frame 7: Multimodal Gemini AI Heritage Assistant (Voice & Typed Chat)", width_inches=3.4)
                  
    doc.add_page_break()
    
    # =========================================================================
    # PAGE 18: SPATIAL CONTROLLER INPUT MAPPING REFERENCE TABLE (FITS ON PAGE 18)
    # =========================================================================
    add_heading_2(doc, "4.3 Spatial Controller Input Mapping Reference Table")
    add_styled_paragraph(doc, 
        "The table below provides a comprehensive reference of all physical controller inputs and gestures across application modes, "
        "mapping Left and Right Touch Plus controllers to core subsystem actions:")
        
    ctrl_headers = ["Mode", "Hand", "Input Element", "Physical Action", "Operational Function & Visual Feedback"]
    ctrl_data = [
        ["Universal / Menu", "Right / Left", "Index Trigger", "Raycast Point & Pull", "Selects menu cards, language toggles, and UI buttons. Audio click + highlight."],
        ["Universal / Submenu", "Left Hand", "Menu Button", "Single Click", "Toggles the omnipresent wrist tablet containing the AI Heritage Guide."],
        ["VR Tour", "Right Hand", "Index Trigger", "Point & Click Chevron", "Triggers smooth quadratic camera translation to the next panoramic viewpoint."],
        ["VR Tour", "Left Hand", "Button Y", "Single Click", "Spawns 3D stupa reconstruction and localized historical information panel."],
        ["VR Tour", "Right Hand", "Thumbstick", "Horizontal Deflection", "Smoothly rotates the inspected 3D stupa model; thumbstick click resets rotation."],
        ["VR Museum", "Right / Left", "Grip Button", "Hold Grip on Relic", "Halts auto-spin; model follows 6-DOF hand motion; triggers gallery dimming."],
        ["VR Museum", "Right Hand", "Buttons A / B", "Hold Button A or B", "Button A reduces artifact scale; Button B enlarges artifact scale smoothly."],
        ["VR Museum", "Left Hand", "Button Y", "Single Click", "Swaps between weathered excavation scan and archaeologically restored model."],
        ["VR Museum", "Left Hand", "Button X / Close", "Single Click", "Exits inspection mode; restores original gallery lighting and pedestal pose."],
        ["On-Site MR", "Right Hand", "Grip + Stick", "Hold Grip + Move Stick", "Translates and rotates the virtual entrance outline to match physical park sign."],
        ["On-Site MR", "Right Hand", "Button A", "Single Click", "Confirms alignment; invokes hardware OVRSpatialAnchor; anchors 1:1 monument."],
        ["On-Site MR", "Right Hand", "Buttons A / B", "Single Click (Post-Align)", "Button A toggles monument visibility; Button B opens archaeological evidence panel."],
        ["On-Site MR", "Left Hand", "Button X", "Hold for 2 Seconds", "Clears active spatial session anchor; returns curator to alignment calibration mode."]
    ]
    create_styled_table(doc, ctrl_headers, ctrl_data, col_widths=[1.0, 0.7, 1.0, 1.2, 2.5], font_size=7.5, space_after=4)
    
    add_callout(doc,
        "Specification Concluding Note:\n"
        "This document reflects the finalized system design and implementation of LangkaLens, incorporating the 15-domain visual repository (512 files), "
        "11 curated archaeological relics, 4 historical architectural reconstructions, and 14 system architecture diagrams. "
        "All visual components and interactive state machines are fully aligned with the senior project report (Docs/Report/LangkaLens_Senior_Project_Report.docx).",
        title="SYSTEM SPECIFICATION CONCLUDING SUMMARY", width_inches=6.4)
        
    out_docx = r"Docs/Report/LangkaLens_Objectives_Scope_Diagrams_and_Storyboards.docx"
    doc.save(out_docx)
    print(f"Successfully generated DOCX: {out_docx}")
    
    # Also write Markdown counterpart
    out_md = out_docx.replace(".docx", ".md")
    write_markdown_counterpart(out_md)
    print(f"Successfully updated Markdown counterpart: {out_md}")

def write_markdown_counterpart(md_path):
    md_content = """# LangkaLens: Project Objectives, System Scope, Updated System Diagrams, and Spatial Storyboards
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
This specification document is compiled specifically for print and academic review. It consolidates the definitive Project Objectives, System Scope, updated Architectural/Activity/Sequence Diagrams (Figures 3.1 through 3.14), and comprehensive Spatial Storyboards for LangkaLens.

These materials represent the latest, updated versions of the concepts originally presented during the preliminary seminar course last semester. Key updates include:
1. **Decoupled Cloud Architecture:** Transitioning from theoretical administrative portals to a focused, standalone mobile VR/MR architecture. Cloud AI (Gemini and Groq) is strictly decoupled from the core presentation loop.
2. **Session-Scoped Language Persistence:** Adopting session-scoped language persistence (confirming once per runtime session).
3. **Unrestricted Mixed Reality with Hardware Anchors:** Upgrading Mixed Reality to an unrestricted boundary matching open-air historical parks, backed by persistent hardware spatial session anchors (`OVRSpatialAnchor`).
4. **Unscaled Museum Dimmer:** Integrating an unscaled environment exposure dimmer (0.18 multiplier over 0.35s) during artifact inspection to maximize focus.
5. **Two-Stage Voice Transcription:** Implementing an asynchronous speech-to-text pipeline (Groq Whisper) with an editable input field before Google Gemini AI query dispatch.
6. **Enriched Visual Asset Integration:** Incorporating the complete 15-domain visual repository (512 assets, 1.72 GB) featuring 11 curated archaeological relics and 4 full-scale architectural reconstructions.

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
| **FR-07** | Multimodal AI Heritage Guide | GeminiChatUI; Google Gemini REST API | Context-grounded prompt injection; automatic fallback on HTTP 429/5xx. |
| **FR-08** | Voice Speech-to-Text | SpeechToTextHandler; Groq Whisper Cloud | 16kHz WAV streaming; editable text field population before prompt dispatch. |
| **NFR-01** | Performance & Frame Rate | URP Forward+; Single-Pass Instanced Stereo | Rock-solid 72/90 FPS; frame time <= 13.8 ms; draw calls <= 120 per eye. |
| **NFR-02** | Memory Footprint | Addressables.Release(); ASTC 6x6 Textures | Peak heap allocation < 2.5 GB; zero VRAM leaks during scene traversal. |
| **NFR-03** | Spatial Ergonomics | World-Space Canvas; 1.2m-2.0m Distance | Eliminates vergence-accommodation conflict; comfort FOV between 30°-60°. |
| **NFR-04** | Cybersickness Mitigation | Universal One-Handed Locomotion System | Zero involuntary camera motion; instantaneous snap-turning options. |
| **NFR-05** | Network Fault Resilience | UnityWebRequest Coroutines; Retry Handlers | Graceful non-blocking error display on offline status or API timeouts. |
| **NFR-06** | Bilingual Symmetry | Parallel StringTable Collections (Thai / EN) | 100% lexical symmetry across UI buttons, descriptions, and guide prompts. |

---

# Part 3: Updated System Diagrams (แผนภาพระบบฉบับปรับปรุงล่าสุด)

### 3.1 High-Level System Architecture Diagram
![Figure 3.1: Updated High-Level System Architecture and External Services Topology](figures/diagrams/figure_3_1.png)

### 3.2 System Actor Profiles & Use Case Diagram
![Figure 3.2: Updated System Functions and Use Case Diagram for Visitors and Curators](figures/diagrams/figure_3_2.png)

### 3.3 System Workflow & Behavioral Activity Flowcharts
- **Figure 3.3:** Application Overview and Scene Navigation Flowchart  
  ![Figure 3.3](figures/diagrams/figure_3_3.png)
- **Figure 3.4:** Main Menu Language Selection and Scene Loading Flowchart  
  ![Figure 3.4](figures/diagrams/figure_3_4.png)
- **Figure 3.5:** VR 360 Panoramic Tour Navigation Flowchart  
  ![Figure 3.5](figures/diagrams/figure_3_5.png)
- **Figure 3.6:** VR Tour Reconstruction Inspection and Memory Cleanup Flowchart  
  ![Figure 3.6](figures/diagrams/figure_3_6.png)
- **Figure 3.7:** VR Museum Artifact Exploration and Environment Dimmer Flowchart  
  ![Figure 3.7](figures/diagrams/figure_3_7.png)
- **Figure 3.8:** On-Site Mixed Reality Entrance Alignment and Session Anchor Flowchart  
  ![Figure 3.8](figures/diagrams/figure_3_8.png)
- **Figure 3.9:** MR Model Display Gating and Tracking Recovery Flowchart  
  ![Figure 3.9](figures/diagrams/figure_3_9.png)
- **Figure 3.10:** Shared Multimodal AI Heritage Guide Workflow Flowchart  
  ![Figure 3.10](figures/diagrams/figure_3_10.png)

### 3.4 Process Sequence Diagrams
- **Figure 3.11:** Sequence Diagram: Asynchronous Tour Viewpoint Transition and Memory Cleanup  
  ![Figure 3.11](figures/diagrams/figure_3_11.png)
- **Figure 3.12:** Sequence Diagram: Museum Artifact Inspection and Dimmer Lifecycle  
  ![Figure 3.12](figures/diagrams/figure_3_12.png)
- **Figure 3.13:** Sequence Diagram: MR Entrance Alignment and Spatial Anchoring  
  ![Figure 3.13](figures/diagrams/figure_3_13.png)
- **Figure 3.14:** Sequence Diagram: Multimodal AI Guide Speech Transcription and Gemini LLM Query  
  ![Figure 3.14](figures/diagrams/figure_3_14.png)

---

# Part 4: Updated System Storyboards & Spatial Interaction Flows

### 4.1 Master Storyboard Specification Matrix

| # | Scene / Experience | Visual Environment | Spatial UI & Components | User Triggers & Controls | Audio & Haptic Feedback | C# Controller Link |
|---|---|---|---|---|---|---|
| **01** | Language Portal | Terracotta void with dust motes | Curved modal (800x520px) with Thai/EN cards | Raycast point & trigger click | Low hum; crisp tactile haptic pulse | MainMenuManager.cs |
| **02** | Main Menu Hub | 3D sunset panorama over ruins | 4 Glassmorphic Portal Cards | Raycast hover & trigger pull | Evening wind; gentle haptic buzz | MainMenuManager.cs |
| **03** | VR 360 Tour | 8K photosphere of Yarang ruins | 3D directional chevrons + Mini-Map | Aim ray at chevron & click trigger | Bird ambience; forward warp sound | TourManager.cs |
| **04** | 3D Stupa Overlay | Photosphere with 3D stupa | Reconstructed stupa + historical card | Press Left Controller Button Y | Sacred temple gong chime | TourReconstructionPresenter.cs |
| **05** | VR Museum | Gallery with spotlight pedestals | Pedestals with 3D relics; dimmer fade | Grip to grab; stick/AB to scale; Y alt | Soft footsteps; haptic snap on grab | InspectionManager.cs |
| **06** | On-Site MR | Real park via Color Passthrough | 1:1 Stupa + Curator reference gizmo | Right Grip+Stick align; Button A anchor | Spatial reverb; tactile anchor lock | MRMeshReferenceAlignment.cs |
| **07** | Multimodal AI Guide | Floating glassmorphic tablet | Tablet window with chat and mic bar | Dictate voice or type; click Send | Digital chime; typewriter audio stream | GeminiChatUI.cs |

### 4.2 Step-by-Step Storyboard Walkthrough & Interface Design

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

### 4.3 Spatial Controller Input Mapping Reference Table

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
"""
    with open(md_path, "w", encoding="utf-8") as f:
        f.write(md_content)

if __name__ == "__main__":
    build_arranged_document()
