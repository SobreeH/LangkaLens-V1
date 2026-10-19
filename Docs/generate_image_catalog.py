"""LangkaLens Image Catalog Generator.

Scans all project image files, extracts image metadata, computes standardized
orderly catalog IDs, and compiles a comprehensive, publication-grade Microsoft Word (.docx)
catalog document, a camera-ready PDF, and a companion Markdown catalog.
"""

import os
import sys
import re
from datetime import datetime
from collections import defaultdict, Counter
from PIL import Image

import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import parse_xml, OxmlElement
from docx.oxml.ns import nsdecls, qn

sys.stdout.reconfigure(encoding='utf-8')

# Color Palette Constants
HEX_PRIMARY_NAVY = "1F497D"
HEX_SECONDARY_NAVY = "2B579A"
HEX_LIGHT_ROW = "F2F4F7"
HEX_WHITE = "FFFFFF"
HEX_BORDER = "D3D3D3"
HEX_DARK_BORDER = "1F497D"
HEX_ACCENT_GOLD = "C59B27"
HEX_LIGHT_BG = "F8F9FA"

COLOR_PRIMARY = RGBColor(0x1F, 0x49, 0x7D)
COLOR_SECONDARY = RGBColor(0x2B, 0x57, 0x9A)
COLOR_TEXT_DARK = RGBColor(0x24, 0x29, 0x2F)
COLOR_MUTED = RGBColor(0x59, 0x59, 0x59)
COLOR_WHITE = RGBColor(0xFF, 0xFF, 0xFF)


def set_cell_background(cell, fill_hex: str):
    """Set cell shading background color."""
    tcPr = cell._element.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex.strip("#")}"/>')
    tcPr.append(shd)


def set_cell_margins(cell, top=100, bottom=100, left=140, right=140):
    """Set inner cell padding in twentieths of a point (dxa)."""
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


def set_cell_borders(cell, top="D3D3D3", bottom="D3D3D3", left="none", right="none", sz="4"):
    """Set custom cell borders."""
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


def apply_thai_font(run, font_name="TH Sarabun New"):
    """Ensure complex script fonts render properly in Word."""
    run.font.name = font_name
    rPr = run._element.get_or_add_rPr()
    rFonts = parse_xml(
        f'<w:rFonts {nsdecls("w")} '
        f'w:ascii="{font_name}" '
        f'w:hAnsi="{font_name}" '
        f'w:cs="{font_name}"/>'
    )
    rPr.append(rFonts)


def insert_callout(doc, text: str, title: str = "NOTE", accent_color=HEX_PRIMARY_NAVY, bg_color=HEX_LIGHT_ROW):
    """Insert a highlighted callout container box with left accent border."""
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl.autofit = False
    tbl.columns[0].width = Inches(6.5)

    cell = tbl.cell(0, 0)
    set_cell_background(cell, bg_color)
    set_cell_margins(cell, top=140, bottom=140, left=200, right=180)

    tcPr = cell._element.get_or_add_tcPr()
    borders = parse_xml(
        f'<w:tcBorders {nsdecls("w")}>'
        f'<w:top w:val="none"/>'
        f'<w:left w:val="single" w:sz="24" w:space="0" w:color="{accent_color}"/>'
        f'<w:bottom w:val="none"/>'
        f'<w:right w:val="none"/>'
        f'</w:tcBorders>'
    )
    tcPr.append(borders)

    p = cell.paragraphs[0]
    p.paragraph_format.line_spacing = 1.15
    p.paragraph_format.space_before = Pt(2)
    p.paragraph_format.space_after = Pt(2)

    r_hdr = p.add_run(f"[{title}] ")
    r_hdr.bold = True
    r_hdr.font.color.rgb = COLOR_PRIMARY
    r_hdr.font.size = Pt(10)

    r_body = p.add_run(text)
    r_body.font.size = Pt(10)
    r_body.font.color.rgb = COLOR_TEXT_DARK

    p_after = doc.add_paragraph()
    p_after.paragraph_format.space_before = Pt(4)
    p_after.paragraph_format.space_after = Pt(4)


def generate_unique_catalog_id(rel_path: str) -> str:
    """Generate a clean, standardized, unique Catalog ID for each image file."""
    p = rel_path.replace(os.sep, '/')
    fname = os.path.basename(p)
    name_no_ext, ext = os.path.splitext(fname)
    ext_tag = ext.strip('.').upper()

    # 1. 01_Museum_Artifacts
    if 'Docs/Images/01_Museum_Artifacts' in p:
        return f'LL-IMG-ART-{name_no_ext.upper()}'
    # 2. 02_Artifact_Comparisons
    elif 'Docs/Images/02_Artifact_Comparisons' in p:
        clean = name_no_ext.replace('_Comparison', '').upper()
        return f'LL-IMG-CMP-{clean}'
    # 3. 03_Historic_Reconstructions
    elif 'Docs/Images/03_Historic_Reconstructions' in p:
        clean = name_no_ext.replace('OG_', '').replace('_', '-').upper()
        return f'LL-IMG-REC-{clean}'
    # 4. 04_Museum_Demo_Exhibits
    elif 'Docs/Images/04_Museum_Demo_Exhibits' in p:
        clean = name_no_ext.replace('_', '-').upper()
        return f'LL-IMG-DEM-{clean}'
    # 5. 05_Interactive_Props_And_Icons
    elif 'Docs/Images/05_Interactive_Props_And_Icons' in p:
        clean = name_no_ext.replace('_3D', '').replace('_', '-').upper()
        return f'LL-IMG-PROP-{clean}'
    # 6. 06_Photogrammetry_Assets
    elif 'Docs/Images/06_Photogrammetry_Assets' in p:
        clean = name_no_ext.replace('_', '-').upper()
        return f'LL-IMG-SCAN-{clean}'
    # 7. Tour 360 Images
    elif 'Tour/360 Images' in p or '360 Images' in p:
        parent = os.path.basename(os.path.dirname(p))
        if 'หมายเลข8' in parent:
            site = 'BJ8'
        elif 'หมายเลข9' in parent:
            site = 'BW9'
        elif 'หมายเลข3' in parent:
            site = 'BJ3'
        elif 'หมายเลข2' in parent:
            site = 'BJ2'
        elif 'Environment' in parent:
            site = 'ENV'
        elif 'Test' in parent:
            site = 'TEST'
        else:
            site = 'MISC'
        scope = 'MISC' if 'Misc/360' in p else ('MIXED' if 'Mixed/360' in p else 'TOUR')
        suffix = f'-{scope}' if scope != 'TOUR' else ''
        return f'LL-IMG-PAN-{site}-{name_no_ext.upper()}{suffix}'
    # 8. User Guide Localized Screenshots
    elif 'User_Guide_Screenshots' in p or 'Screenshots/UserGuides' in p:
        sub = 'DOCS' if 'Docs/Screenshots/UserGuides' in p else 'MANUAL'
        clean = name_no_ext.replace('_', '-').upper()
        return f'LL-IMG-GUIDE-{sub}-{clean}'
    # 9. User Guide Workflow Captures
    elif 'User_Guide_Pictures' in p or 'Guide images' in p:
        sub = 'DOCS' if 'Docs/User_Guide_Pictures' in p else 'ASSETS'
        clean = name_no_ext.replace('_', '-').upper()
        return f'LL-IMG-WF-{sub}-{clean}'
    # 10. Report figures
    elif 'Docs/Report/figures' in p:
        sub = os.path.basename(os.path.dirname(p)).upper()
        clean = name_no_ext.replace('_', '-').upper()
        return f'LL-IMG-REP-{sub}-{clean}'
    # 11. Store Assets & Root Icons
    elif 'StoreAssets' in p or (os.path.dirname(p) == '' and 'LangkaLens' in p):
        sub = 'STORE' if 'StoreAssets' in p else 'ROOT'
        clean = name_no_ext.replace('LangkaLens_', '').replace('_', '-').upper()
        return f'LL-IMG-STR-{sub}-{clean}-{ext_tag}'
    # 12. UI Sprites, Menus, Logos
    elif 'Main_menu/Images' in p or 'Misc/Images' in p or 'Tour/Images' in p:
        parts = p.split('/')
        sub = parts[-2].upper() if len(parts) >= 2 else 'MAIN'
        clean = re.sub(r'[^a-zA-Z0-9]+', '-', name_no_ext).strip('-').upper()
        return f'LL-IMG-UI-{sub}-{clean}-{ext_tag}'
    # 13. Textures
    elif 'Textures' in p:
        parent = os.path.basename(os.path.dirname(p)).upper()
        clean = name_no_ext.replace('_', '-').upper()
        return f'LL-IMG-MAT-{parent}-{clean}'
    # 14. Playtesting / Dev
    elif 'Screenshots/Playtesting' in p or 'Screenshots/Development' in p:
        sub = 'PLAYTEST' if 'Playtesting' in p else 'DEV'
        clean = re.sub(r'[^a-zA-Z0-9]+', '-', name_no_ext).strip('-').upper()
        return f'LL-IMG-EVL-{sub}-{clean}'
    # 15. Plugins / Supporting
    else:
        parts = p.split('/')
        sub = parts[1].upper() if len(parts) > 2 else 'ENG'
        sub2 = parts[-2].upper() if len(parts) >= 3 else 'SUP'
        clean = re.sub(r'[^a-zA-Z0-9]+', '-', name_no_ext).strip('-').upper()
        return f'LL-IMG-ENG-{sub}-{sub2}-{clean}-{ext_tag}'


def categorize_asset(rel_path: str):
    """Categorize asset into one of 15 standard domains."""
    p = rel_path.replace(os.sep, '/')
    if 'Docs/Images/01_Museum_Artifacts' in p:
        return 'CAT-01', 'Museum Archaeological Artifacts (Excavated vs Restored)', 'VR Museum / Inspection'
    elif 'Docs/Images/02_Artifact_Comparisons' in p:
        return 'CAT-02', 'Artifact Comparison Sheets (Side-by-Side Widescreen)', 'Report / Presentation'
    elif 'Docs/Images/03_Historic_Reconstructions' in p:
        return 'CAT-03', 'Historic Monument Architectural Reconstructions', 'VR Tour & Mixed Reality'
    elif 'Docs/Images/04_Museum_Demo_Exhibits' in p:
        return 'CAT-04', 'Museum Gallery Demo Ceramic & Relic Exhibits', 'VR Museum / Secondary'
    elif 'Docs/Images/05_Interactive_Props_And_Icons' in p:
        return 'CAT-05', 'Interactive 3D Props & Navigation Elements', 'VR Tour / UI Interaction'
    elif 'Docs/Images/06_Photogrammetry_Assets' in p:
        return 'CAT-06', 'Photogrammetric Scans & Environmental Assets', 'Museum Environment'
    elif 'Tour/360 Images' in p or '360 Images' in p:
        return 'CAT-07', '360-Degree Panoramic VR Tour Nodes', 'VR Tour / Equirectangular'
    elif 'User_Guide_Screenshots' in p or 'Screenshots/UserGuides' in p:
        return 'CAT-08', 'User Guide Localized Module Screenshots (EN/TH)', 'Docs / In-Game Guide'
    elif 'User_Guide_Pictures' in p or 'Guide images' in p:
        return 'CAT-09', 'User Guide Step-by-Step Workflow Captures', 'In-Engine UI Guide / Manual'
    elif 'Docs/Report/figures' in p:
        return 'CAT-10', 'Academic Thesis & Senior Project Report Figures', 'PSU Senior Project Report'
    elif 'StoreAssets' in p or (os.path.dirname(p) == '' and 'LangkaLens' in p):
        return 'CAT-11', 'Horizon Store, Distribution & Branding Assets', 'Horizon OS Store & Marketing'
    elif 'Main_menu/Images' in p or 'Misc/Images' in p or 'Tour/Images' in p:
        return 'CAT-12', 'In-Game UI, Menus, Thumbnails & HUD Sprites', 'Main Menu & HUD System'
    elif 'Textures' in p:
        return 'CAT-13', 'Material Textures & PBR Shading Maps', '3D Graphics / Shaders'
    elif 'Screenshots/Playtesting' in p or 'Screenshots/Development' in p:
        return 'CAT-14', 'User Evaluation, Playtesting & Debugging Captures', 'Validation & QA Testing'
    else:
        return 'CAT-15', 'Engine Utilities & Supporting Asset Textures', 'Unity Engine / Plugins'


def get_image_details(abs_path: str):
    """Extract width, height, aspect ratio, color mode, and size string."""
    size_bytes = os.path.getsize(abs_path)
    if size_bytes >= 1024 * 1024:
        size_str = f"{size_bytes / (1024 * 1024):.2f} MB"
    else:
        size_str = f"{size_bytes / 1024:.1f} KB"

    w, h, mode, aspect_str = 0, 0, "Unknown", "N/A"
    try:
        with Image.open(abs_path) as im:
            w, h = im.size
            mode = im.mode
            if h > 0:
                ratio = w / h
                if abs(ratio - 1.0) < 0.05:
                    aspect_str = "1:1 Square"
                elif abs(ratio - 1.777) < 0.05:
                    aspect_str = "16:9 Wide"
                elif abs(ratio - 2.0) < 0.05:
                    aspect_str = "2:1 Pano"
                elif abs(ratio - 1.333) < 0.05:
                    aspect_str = "4:3 Standard"
                elif abs(ratio - 0.562) < 0.05:
                    aspect_str = "9:16 Portrait"
                else:
                    aspect_str = f"{ratio:.2f}:1"
    except Exception:
        pass

    return w, h, aspect_str, mode, size_bytes, size_str


def scan_all_images():
    """Scans project repository and collects rich records for all 512 image assets."""
    exts = {'.png', '.jpg', '.jpeg', '.webp', '.tga', '.bmp'}
    ignored_dirs = {'.git', 'Library', 'Temp', 'obj', 'Logs', 'Builds', 'Packages', '.tmp', 'tmp', '.development-update'}

    records = []
    for root, dirs, files in os.walk('.'):
        dirs[:] = [d for d in dirs if d not in ignored_dirs and not d.startswith('.')]
        for f in files:
            ext = os.path.splitext(f)[1].lower()
            if ext in exts:
                abs_p = os.path.abspath(os.path.join(root, f))
                rel_p = os.path.relpath(abs_p, '.').replace(os.sep, '/')
                cid = generate_unique_catalog_id(rel_p)
                cat_code, cat_title, target_subsys = categorize_asset(rel_p)
                w, h, aspect, mode, size_bytes, size_str = get_image_details(abs_p)

                records.append({
                    'id': cid,
                    'cat_code': cat_code,
                    'cat_title': cat_title,
                    'target_subsys': target_subsys,
                    'rel_path': rel_p,
                    'abs_path': abs_p,
                    'filename': f,
                    'ext': ext.strip('.').upper(),
                    'width': w,
                    'height': h,
                    'aspect': aspect,
                    'mode': mode,
                    'size_bytes': size_bytes,
                    'size_str': size_str,
                })

    records.sort(key=lambda r: (r['cat_code'], r['id']))
    return records


def build_catalog_docx(records, output_path="Docs/LangkaLens_Image_Catalog.docx"):
    """Assemble complete high-standard Microsoft Word document."""
    doc = docx.Document()
    sec = doc.sections[0]
    sec.top_margin = Inches(1.0)
    sec.bottom_margin = Inches(1.0)
    sec.left_margin = Inches(1.0)
    sec.right_margin = Inches(1.0)
    sec.different_first_page_header_footer = True

    # Header / Footer
    header = sec.header
    hp = header.paragraphs[0]
    hp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    hr = hp.add_run("LangkaLens | Visual Assets & Image Catalog")
    hr.font.name = "Segoe UI"
    hr.font.size = Pt(8.5)
    hr.font.color.rgb = RGBColor(0x88, 0x88, 0x88)

    footer = sec.footer
    fp = footer.paragraphs[0]
    fp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    fr1 = fp.add_run("LangkaLens Project — Meta Quest 3/3S VR/MR Experience  |  Page ")
    fr1.font.name = "Segoe UI"
    fr1.font.size = Pt(8.5)
    fr1.font.color.rgb = RGBColor(0x88, 0x88, 0x88)

    # Document Styles
    normal_style = doc.styles['Normal']
    normal_style.font.name = 'Segoe UI'
    normal_style.font.size = Pt(10)
    normal_style.font.color.rgb = COLOR_TEXT_DARK
    normal_style.paragraph_format.line_spacing = 1.15
    normal_style.paragraph_format.space_after = Pt(6)

    # ==========================================
    # COVER / TITLE BLOCK
    # ==========================================
    p_inst = doc.add_paragraph()
    p_inst.paragraph_format.space_before = Pt(12)
    p_inst.paragraph_format.space_after = Pt(2)
    r_inst = p_inst.add_run("PRINCE OF SONGKLA UNIVERSITY  |  FACULTY OF COMMUNICATION SCIENCES  |  TSRI")
    r_inst.font.name = "Segoe UI"
    r_inst.font.size = Pt(9.5)
    r_inst.bold = True
    r_inst.font.color.rgb = COLOR_SECONDARY

    p_title = doc.add_paragraph()
    p_title.paragraph_format.space_before = Pt(4)
    p_title.paragraph_format.space_after = Pt(4)
    r_title = p_title.add_run("LangkaLens (ลังกาเลนส์)")
    r_title.font.name = "Segoe UI"
    r_title.font.size = Pt(28)
    r_title.bold = True
    r_title.font.color.rgb = COLOR_PRIMARY

    p_sub = doc.add_paragraph()
    p_sub.paragraph_format.space_before = Pt(0)
    p_sub.paragraph_format.space_after = Pt(12)
    r_sub = p_sub.add_run("Master Visual Assets & Image Catalog — Technical Specifications & Orderly Taxonomy")
    r_sub.font.name = "Segoe UI"
    r_sub.font.size = Pt(14)
    r_sub.font.color.rgb = COLOR_MUTED

    # Meta banner box
    meta_tbl = doc.add_table(rows=1, cols=4)
    meta_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    meta_tbl.autofit = False
    col_widths = [Inches(1.6), Inches(1.6), Inches(1.6), Inches(1.7)]
    for row in meta_tbl.rows:
        for idx, width in enumerate(col_widths):
            row.cells[idx].width = width

    meta_items = [
        ("TOTAL ASSETS", f"{len(records)} Files"),
        ("CATEGORIES", "15 Structured Domains"),
        ("TARGET PLATFORM", "Meta Quest 3 & 3S"),
        ("COMPILED DATE", datetime.now().strftime("%B %Y")),
    ]
    for idx, (label, val) in enumerate(meta_items):
        cell = meta_tbl.cell(0, idx)
        set_cell_background(cell, HEX_LIGHT_ROW)
        set_cell_margins(cell, top=100, bottom=100, left=100, right=100)
        set_cell_borders(cell, top=HEX_PRIMARY_NAVY, bottom=HEX_PRIMARY_NAVY, sz="12")
        cp = cell.paragraphs[0]
        cp.paragraph_format.space_after = Pt(2)
        r_lbl = cp.add_run(label + "\n")
        r_lbl.font.size = Pt(7.5)
        r_lbl.bold = True
        r_lbl.font.color.rgb = COLOR_SECONDARY
        r_val = cp.add_run(val)
        r_val.font.size = Pt(10)
        r_val.bold = True
        r_val.font.color.rgb = COLOR_PRIMARY

    p_div = doc.add_paragraph()
    p_div.paragraph_format.space_before = Pt(12)
    p_div.paragraph_format.space_after = Pt(12)

    # ==========================================
    # SECTION 1: EXECUTIVE SUMMARY & OBJECTIVE
    # ==========================================
    h1 = doc.add_heading("1. Executive Summary & Catalog Scope", level=1)
    h1.style.font.color.rgb = COLOR_PRIMARY
    h1.style.font.name = "Segoe UI"

    doc.add_paragraph(
        "This document establishes the official Master Image Catalog and systematic naming taxonomy for "
        "LangkaLens, an immersive mixed reality and virtual reality cultural heritage experience developed for "
        "Meta Quest 3 and Meta Quest 3S. The project preserves, reconstructs, and communicates the ancient Dvaravati "
        "archaeology of Yarang, Pattani Province, Thailand through four interconnected subsystems: the Virtual Museum, "
        "the 360-degree Panoramic Tour, Spatial Alignment Mixed Reality, and an AI Cultural Guide powered by Gemini."
    )

    doc.add_paragraph(
        f"A rigorous automated audit across the active project repository identified exactly {len(records)} image assets "
        "spanning high-definition 3D studio renders, original archaeological fragments, widescreen comparison plates, "
        "equirectangular 360° environment nodes, dual-language user guide manuals, academic thesis figures, store distribution "
        "hero artwork, and physics-based rendering (PBR) texture maps."
    )

    insert_callout(
        doc,
        "All assets are categorized under standardized taxonomy codes ('LL-IMG-[DOMAIN]-[SUB]-[NAME]') "
        "to prevent nomenclature drift, eliminate duplicate runtime memory loads on standalone mobile XR hardware, "
        "and maintain pristine cross-referencing across Unity Addressables, academic publications, and store submissions.",
        title="CATALOGING GOVERNANCE STANDARD"
    )

    # ==========================================
    # SECTION 2: ORDERLY TAXONOMY & NAMING SYNTAX
    # ==========================================
    h2 = doc.add_heading("2. Standardized Orderly Naming Taxonomy", level=1)
    h2.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph(
        "To guarantee clarity and prevent ambiguity between engine runtime assets, documentation plates, and "
        "store assets, LangkaLens adheres to the following hierarchical naming syntax:"
    )

    # Syntax box
    syn_tbl = doc.add_table(rows=1, cols=1)
    syn_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    syn_cell = syn_tbl.cell(0, 0)
    set_cell_background(syn_cell, HEX_LIGHT_ROW)
    set_cell_margins(syn_cell, top=100, bottom=100, left=140, right=140)
    set_cell_borders(syn_cell, left=HEX_PRIMARY_NAVY, sz="18")
    sp = syn_cell.paragraphs[0]
    sr1 = sp.add_run("LL-IMG-[DOMAIN]-[SUBGROUP]-[DESCRIPTOR]-[VARIANT].[ext]\n")
    sr1.bold = True
    sr1.font.name = "Consolas"
    sr1.font.size = Pt(11)
    sr1.font.color.rgb = COLOR_PRIMARY
    sr2 = sp.add_run(
        "Example 1: LL-IMG-ART-01-BUDDHA-ORG.png  (Excavated standing Buddha weathered fragment)\n"
        "Example 2: LL-IMG-CMP-01-BUDDHA.png     (1920x1080 side-by-side original vs restored comparison)\n"
        "Example 3: LL-IMG-PAN-BJ8-B01.jpg       (360° equirectangular node at Ban Ja-Lae Site 8)\n"
        "Example 4: LL-IMG-STR-COVER-LANDSCAPE-2560.png (2560x1440 Meta Quest Store banner)"
    )
    sr2.font.name = "Consolas"
    sr2.font.size = Pt(9)
    sr2.font.color.rgb = COLOR_TEXT_DARK

    doc.add_paragraph().paragraph_format.space_after = Pt(4)

    # Prefix taxonomy table
    pref_tbl = doc.add_table(rows=1, cols=4)
    pref_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    pref_tbl.autofit = False
    p_widths = [Inches(1.2), Inches(1.8), Inches(1.3), Inches(2.2)]
    for idx, width in enumerate(p_widths):
        pref_tbl.columns[idx].width = width

    headers = ["Prefix", "Functional Category", "Resolution Tier", "Primary Unity / Experience Purpose"]
    hdr_row = pref_tbl.rows[0]
    for idx, h_text in enumerate(headers):
        cell = hdr_row.cells[idx]
        set_cell_background(cell, HEX_PRIMARY_NAVY)
        set_cell_margins(cell, top=80, bottom=80, left=100, right=100)
        set_cell_borders(cell, top=HEX_PRIMARY_NAVY, bottom=HEX_PRIMARY_NAVY, sz="8")
        p = cell.paragraphs[0]
        r = p.add_run(h_text)
        r.bold = True
        r.font.size = Pt(9)
        r.font.color.rgb = COLOR_WHITE

    taxonomy_rows = [
        ("ART", "CAT-01 Museum Artifacts", "1024x1024 Square", "Excavated fragment & restored model renders"),
        ("CMP", "CAT-02 Artifact Comparisons", "1920x1080 Widescreen", "Side-by-side scholarly verification plates"),
        ("REC", "CAT-03 Monument Reconstructions", "1024x1024 Square", "Architectural stupa and sanctuary models"),
        ("DEM", "CAT-04 Gallery Demo Exhibits", "1024x1024 Square", "Ancillary pottery & earthenware exhibits"),
        ("PROP", "CAT-05 Interactive 3D Props", "1024x1024 Square", "Waypoints, globes, navigation arrows, levers"),
        ("SCAN", "CAT-06 Photogrammetric Scans", "1024x1024 Square", "Ground materials and structural scan textures"),
        ("PAN", "CAT-07 360° VR Tour Panoramas", "5760x2880 Equirect.", "Teleportation nodes for Sites BJ2, BJ3, BJ8, BW9"),
        ("GUIDE", "CAT-08 Localized Manual Sheets", "1920x1080 Widescreen", "Bilingual English & Thai user guide dialogs"),
        ("WF", "CAT-09 Step-by-Step Workflows", "1920x1080 Widescreen", "Numbered controller interaction instruction pictures"),
        ("REP", "CAT-10 Academic Thesis Figures", "Vector / High-res PNG", "PSU senior project report system diagrams"),
        ("STR", "CAT-11 Horizon Store Assets", "2560x1440 / 1440x1440", "Horizon OS store icons, covers, hero banners"),
        ("UI", "CAT-12 HUD, Menus & Logos", "512x512 / Varied", "Mode buttons, Thai/English buttons, brand crests"),
        ("MAT", "CAT-13 PBR Texture Sets", "1024x1024 / 2048x2048", "Albedo (_d), Normal (_n), Specular (_s) maps"),
        ("EVL", "CAT-14 Playtesting & Dev Logs", "1920x1080 Widescreen", "User evaluation study screenshots & debug logs"),
        ("ENG", "CAT-15 Engine Utilities", "Varied", "TextMesh Pro font atlases and plugin textures"),
    ]

    for row_idx, (pref, cat_name, res_tier, desc) in enumerate(taxonomy_rows):
        row = pref_tbl.add_row()
        bg_col = HEX_LIGHT_ROW if row_idx % 2 == 0 else HEX_WHITE
        vals = [pref, cat_name, res_tier, desc]
        for col_idx, text in enumerate(vals):
            cell = row.cells[col_idx]
            cell.width = p_widths[col_idx]
            set_cell_background(cell, bg_col)
            set_cell_margins(cell, top=60, bottom=60, left=80, right=80)
            set_cell_borders(cell, top=HEX_BORDER, bottom=HEX_BORDER, sz="4")
            cp = cell.paragraphs[0]
            cp.paragraph_format.space_after = Pt(1)
            cr = cp.add_run(text)
            cr.font.size = Pt(8.5)
            if col_idx == 0:
                cr.bold = True
                cr.font.color.rgb = COLOR_PRIMARY
                cr.font.name = "Consolas"
            elif col_idx == 1:
                cr.bold = True

    doc.add_paragraph().paragraph_format.space_after = Pt(12)

    # ==========================================
    # SECTION 3: INVENTORY SUMMARY MATRIX
    # ==========================================
    h3 = doc.add_heading("3. Inventory Matrix by Category Domain", level=1)
    h3.style.font.color.rgb = COLOR_PRIMARY

    cat_groups = defaultdict(list)
    for r in records:
        cat_groups[r['cat_code']].append(r)

    sum_tbl = doc.add_table(rows=1, cols=6)
    sum_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    sum_tbl.autofit = False
    s_widths = [Inches(0.9), Inches(2.2), Inches(0.8), Inches(0.9), Inches(0.9), Inches(0.8)]
    for idx, width in enumerate(s_widths):
        sum_tbl.columns[idx].width = width

    s_headers = ["Code", "Category Domain Name", "Files", "Volume", "Formats", "Subsystem"]
    s_hdr_row = sum_tbl.rows[0]
    for idx, h_text in enumerate(s_headers):
        cell = s_hdr_row.cells[idx]
        set_cell_background(cell, HEX_PRIMARY_NAVY)
        set_cell_margins(cell, top=80, bottom=80, left=80, right=80)
        set_cell_borders(cell, top=HEX_PRIMARY_NAVY, bottom=HEX_PRIMARY_NAVY, sz="8")
        p = cell.paragraphs[0]
        r = p.add_run(h_text)
        r.bold = True
        r.font.size = Pt(9)
        r.font.color.rgb = COLOR_WHITE

    total_files = len(records)
    total_bytes = sum(r['size_bytes'] for r in records)

    cat_keys = sorted(cat_groups.keys())
    for row_idx, k in enumerate(cat_keys):
        items = cat_groups[k]
        cat_bytes = sum(x['size_bytes'] for x in items)
        if cat_bytes >= 1024 * 1024:
            cat_vol = f"{cat_bytes / (1024 * 1024):.1f} MB"
        else:
            cat_vol = f"{cat_bytes / 1024:.0f} KB"

        exts_present = "/".join(sorted(set(x['ext'] for x in items)))
        subsystem = items[0]['target_subsys'].split('/')[0].strip()

        row = sum_tbl.add_row()
        bg_col = HEX_LIGHT_ROW if row_idx % 2 == 0 else HEX_WHITE
        row_vals = [k, items[0]['cat_title'], str(len(items)), cat_vol, exts_present, subsystem]
        for col_idx, text in enumerate(row_vals):
            cell = row.cells[col_idx]
            cell.width = s_widths[col_idx]
            set_cell_background(cell, bg_col)
            set_cell_margins(cell, top=60, bottom=60, left=60, right=60)
            set_cell_borders(cell, top=HEX_BORDER, bottom=HEX_BORDER, sz="4")
            cp = cell.paragraphs[0]
            cp.paragraph_format.space_after = Pt(1)
            cr = cp.add_run(text)
            cr.font.size = Pt(8.5)
            if col_idx == 0:
                cr.bold = True
                cr.font.color.rgb = COLOR_PRIMARY
                cr.font.name = "Consolas"
            elif col_idx == 2 or col_idx == 3:
                cr.bold = True

    # Total row
    tot_row = sum_tbl.add_row()
    tot_vol = f"{total_bytes / (1024 * 1024):.1f} MB"
    tot_vals = ["TOTAL", "All 15 Project Visual Domains", str(total_files), tot_vol, "PNG/JPG/TGA", "Full Project"]
    for col_idx, text in enumerate(tot_vals):
        cell = tot_row.cells[col_idx]
        cell.width = s_widths[col_idx]
        set_cell_background(cell, HEX_SECONDARY_NAVY)
        set_cell_margins(cell, top=80, bottom=80, left=60, right=60)
        set_cell_borders(cell, top=HEX_PRIMARY_NAVY, bottom=HEX_PRIMARY_NAVY, sz="12")
        cp = cell.paragraphs[0]
        cr = cp.add_run(text)
        cr.bold = True
        cr.font.size = Pt(9)
        cr.font.color.rgb = COLOR_WHITE

    doc.add_paragraph().paragraph_format.space_after = Pt(16)

    # ==========================================
    # SECTION 4: DETAILED CATEGORY GALLERIES & INVENTORIES
    # ==========================================
    doc.add_page_break()
    h4 = doc.add_heading("4. Comprehensive Domain Catalogs & Visual Galleries", level=1)
    h4.style.font.color.rgb = COLOR_PRIMARY

    # Helper function to render a table for category items
    def render_category_table(items, show_path=True):
        tbl = doc.add_table(rows=1, cols=6)
        tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
        tbl.autofit = False
        widths = [Inches(1.8), Inches(1.8), Inches(0.9), Inches(0.7), Inches(0.6), Inches(0.7)]
        for idx, width in enumerate(widths):
            tbl.columns[idx].width = width

        hdrs = ["Standard Catalog ID", "Relative Project File Path", "Resolution", "Size", "Type", "Color"]
        for idx, h_text in enumerate(hdrs):
            cell = tbl.rows[0].cells[idx]
            set_cell_background(cell, HEX_PRIMARY_NAVY)
            set_cell_margins(cell, top=60, bottom=60, left=60, right=60)
            set_cell_borders(cell, top=HEX_PRIMARY_NAVY, bottom=HEX_PRIMARY_NAVY, sz="6")
            p = cell.paragraphs[0]
            r = p.add_run(h_text)
            r.bold = True
            r.font.size = Pt(8)
            r.font.color.rgb = COLOR_WHITE

        for row_idx, item in enumerate(items):
            row = tbl.add_row()
            bg_col = HEX_LIGHT_ROW if row_idx % 2 == 0 else HEX_WHITE
            res_str = f"{item['width']}x{item['height']}" if item['width'] > 0 else "Vector/Meta"
            vals = [
                item['id'],
                item['rel_path'],
                res_str,
                item['size_str'],
                item['ext'],
                item['mode']
            ]
            for col_idx, text in enumerate(vals):
                cell = row.cells[col_idx]
                cell.width = widths[col_idx]
                set_cell_background(cell, bg_col)
                set_cell_margins(cell, top=40, bottom=40, left=50, right=50)
                set_cell_borders(cell, top=HEX_BORDER, bottom=HEX_BORDER, sz="3")
                cp = cell.paragraphs[0]
                cp.paragraph_format.space_after = Pt(1)
                cr = cp.add_run(text)
                cr.font.size = Pt(7.5)
                if col_idx == 0:
                    cr.bold = True
                    cr.font.color.rgb = COLOR_PRIMARY
                    cr.font.name = "Consolas"
                elif col_idx == 1:
                    cr.font.name = "Consolas"
                    cr.font.size = Pt(7.0)
                    cr.font.color.rgb = COLOR_MUTED

        doc.add_paragraph().paragraph_format.space_after = Pt(10)

    # -------------------------------------------------------------
    # DOMAIN 4.1: MUSEUM ARTIFACTS & COMPARISON SHEETS (CAT-01 & CAT-02)
    # -------------------------------------------------------------
    h4_1 = doc.add_heading("4.1 Museum Archaeological Artifacts & Side-by-Side Comparisons (CAT-01 & CAT-02)", level=2)
    h4_1.style.font.color.rgb = COLOR_SECONDARY

    doc.add_paragraph(
        "The Virtual Museum gallery showcases 11 primary archaeological artifacts discovered at Yarang. "
        "Each artifact is rendered in two complementary states: (1) the original excavated weathered fragment (_ORG) "
        "and (2) the digitally restored Dvaravati cultural form (_RES). Furthermore, 11 high-definition 1920x1080 comparison "
        "plates (CAT-02) illustrate the anatomical and structural restorations side-by-side for scholarly analysis."
    )

def safe_add_picture(paragraph, image_path: str, width=Inches(5.8), max_dim=2048):
    """Safely insert image, stripping corrupt EXIF headers and constraining dimensions for clean DOCX storage."""
    import io
    try:
        with Image.open(image_path) as im:
            w, h = im.size
            needs_resample = (w > max_dim or h > max_dim or im.format == 'JPEG')
            if needs_resample:
                scale = min(1.0, max_dim / max(w, h))
                new_size = (int(w * scale), int(h * scale))
                buf = io.BytesIO()
                if im.mode in ('RGBA', 'LA') or (im.mode == 'P' and 'transparency' in im.info):
                    im_clean = im.convert('RGBA').resize(new_size, Image.Resampling.LANCZOS)
                    im_clean.save(buf, format='PNG')
                else:
                    im_clean = im.convert('RGB').resize(new_size, Image.Resampling.LANCZOS)
                    im_clean.save(buf, format='JPEG', quality=90)
                buf.seek(0)
                return paragraph.add_run().add_picture(buf, width=width)
            else:
                return paragraph.add_run().add_picture(image_path, width=width)
    except Exception as e:
        print(f"safe_add_picture fallback for {image_path}: {e}")
        try:
            return paragraph.add_run().add_picture(image_path, width=width)
        except Exception as e2:
            print(f"Direct insertion failed: {e2}")
            return None


    # Visual figure gallery: Embed representative comparison plates
    cmp_items = cat_groups['CAT-02']
    if cmp_items:
        doc.add_heading("Representative Widescreen Comparison Plates (1920x1080):", level=3)
        sample_cmps = [
            ("Buddha_Comparison.png", "Figure 4.1.1: Standing Buddha — Excavated Fragment vs. Abhaya Mudra Digital Restoration"),
            ("Tri_Comparison.png", "Figure 4.1.2: Surya Stele / Trinity Relief — Weathered Bas-Relief vs. Ornate Aureole Restoration"),
            ("Wheel_Comparison.png", "Figure 4.1.3: Dharmachakra Law Wheel — Excavated Stone Hub vs. Eight-Spoked Restoration"),
        ]
        for fname, caption in sample_cmps:
            matching = [x for x in cmp_items if fname in x['filename']]
            if matching and os.path.exists(matching[0]['abs_path']):
                p_img = doc.add_paragraph()
                p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
                p_img.paragraph_format.space_before = Pt(4)
                p_img.paragraph_format.space_after = Pt(2)
                pic = safe_add_picture(p_img, matching[0]['abs_path'], width=Inches(5.8))
                if pic:
                    p_cap = doc.add_paragraph()
                    p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
                    p_cap.paragraph_format.space_after = Pt(8)
                    r_cap = p_cap.add_run(caption)
                    r_cap.font.size = Pt(8.5)
                    r_cap.font.italic = True
                    r_cap.font.color.rgb = COLOR_MUTED

    doc.add_heading("CAT-01: Individual Museum Artifact Renders (22 Files):", level=3)
    render_category_table(cat_groups['CAT-01'])

    doc.add_heading("CAT-02: Widescreen Artifact Comparison Sheets (11 Files):", level=3)
    render_category_table(cat_groups['CAT-02'])

    # -------------------------------------------------------------
    # DOMAIN 4.2: HISTORIC RECONSTRUCTIONS (CAT-03)
    # -------------------------------------------------------------
    doc.add_page_break()
    h4_2 = doc.add_heading("4.2 Historic Architectural Monument Reconstructions (CAT-03)", level=2)
    h4_2.style.font.color.rgb = COLOR_SECONDARY

    doc.add_paragraph(
        "Ancient monument reconstructions depict the architectural marvels of Yarang at full monumental scale. "
        "These models are utilized in both the 360 VR Tour (for overlay inspection) and Mixed Reality (for 1:1 real-world "
        "spatial alignment at the actual archaeological ruins of Ban Ja-Lae Site 2, 3, 8 and Ban Bo-Wa Site 9)."
    )

    rec_items = cat_groups['CAT-03']
    if rec_items:
        fig_tbl = doc.add_table(rows=1, cols=2)
        fig_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
        fig_tbl.autofit = False
        fig_tbl.columns[0].width = Inches(3.2)
        fig_tbl.columns[1].width = Inches(3.2)

        bj3_item = [x for x in rec_items if 'BJ3' in x['filename']]
        temple_item = [x for x in rec_items if 'Temple_Complex' in x['filename']]

        if bj3_item and os.path.exists(bj3_item[0]['abs_path']):
            c0 = fig_tbl.cell(0, 0)
            p0 = c0.paragraphs[0]
            p0.alignment = WD_ALIGN_PARAGRAPH.CENTER
            safe_add_picture(p0, bj3_item[0]['abs_path'], width=Inches(3.0))
            cap0 = c0.add_paragraph("Figure 4.2.1: Ban Ja-Lae Site 3 Monument")
            cap0.alignment = WD_ALIGN_PARAGRAPH.CENTER
            cap0.runs[0].font.size = Pt(8)
            cap0.runs[0].font.italic = True

        if temple_item and os.path.exists(temple_item[0]['abs_path']):
            c1 = fig_tbl.cell(0, 1)
            p1 = c1.paragraphs[0]
            p1.alignment = WD_ALIGN_PARAGRAPH.CENTER
            safe_add_picture(p1, temple_item[0]['abs_path'], width=Inches(3.0))
            cap1 = c1.add_paragraph("Figure 4.2.2: Full Ancient Temple Complex")
            cap1.alignment = WD_ALIGN_PARAGRAPH.CENTER
            cap1.runs[0].font.size = Pt(8)
            cap1.runs[0].font.italic = True

        doc.add_paragraph().paragraph_format.space_after = Pt(8)

    render_category_table(rec_items)

    # -------------------------------------------------------------
    # DOMAIN 4.3: DEMO EXHIBITS & PHOTOGRAMMETRIC SCANS (CAT-04 & CAT-06)
    # -------------------------------------------------------------
    h4_3 = doc.add_heading("4.3 Museum Demo Exhibits & Photogrammetric Scans (CAT-04 & CAT-06)", level=2)
    h4_3.style.font.color.rgb = COLOR_SECONDARY

    doc.add_paragraph(
        "Supplementary gallery assets encompass ceramic demo jars, ancient earthenware vessels, terracotta relics, "
        "and Level-of-Detail (LOD) photogrammetric environmental scans that form the photorealistic museum flooring and plinths."
    )

    # Visual previews for Demo Exhibits
    demo_items = cat_groups['CAT-04']
    if demo_items:
        d_tbl = doc.add_table(rows=1, cols=2)
        d_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
        d_tbl.autofit = False
        d_tbl.columns[0].width = Inches(3.2)
        d_tbl.columns[1].width = Inches(3.2)
        d1 = [x for x in demo_items if 'Demo1' in x['filename']]
        d3 = [x for x in demo_items if 'Demo3' in x['filename']]
        if d1 and os.path.exists(d1[0]['abs_path']):
            safe_add_picture(d_tbl.cell(0, 0).paragraphs[0], d1[0]['abs_path'], width=Inches(2.8))
            cap = d_tbl.cell(0, 0).add_paragraph("Figure 4.3.1: Glazed Ceramic Pottery")
            cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
            cap.runs[0].font.size = Pt(8)
            cap.runs[0].font.italic = True
        if d3 and os.path.exists(d3[0]['abs_path']):
            safe_add_picture(d_tbl.cell(0, 1).paragraphs[0], d3[0]['abs_path'], width=Inches(2.8))
            cap = d_tbl.cell(0, 1).add_paragraph("Figure 4.3.2: Terracotta Storage Pottery")
            cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
            cap.runs[0].font.size = Pt(8)
            cap.runs[0].font.italic = True
        doc.add_paragraph().paragraph_format.space_after = Pt(6)

    doc.add_heading("CAT-04: Museum Gallery Demo Exhibits (5 Files):", level=3)
    render_category_table(cat_groups['CAT-04'])

    doc.add_heading("CAT-06: Photogrammetric Scans & Environmental Assets (7 Files):", level=3)
    render_category_table(cat_groups['CAT-06'])

    # -------------------------------------------------------------
    # DOMAIN 4.4: INTERACTIVE 3D PROPS & NAVIGATION (CAT-05)
    # -------------------------------------------------------------
    h4_4 = doc.add_heading("4.4 Interactive 3D Props & Navigation Elements (CAT-05)", level=2)
    h4_4.style.font.color.rgb = COLOR_SECONDARY

    doc.add_paragraph(
        "Custom 3D user-interface elements, interaction levers, museum pedestals, and teleportable 360-degree navigation "
        "arrows engineered for Meta Interaction SDK grab, ray, and poke modalities."
    )

    prop_items = cat_groups['CAT-05']
    if prop_items:
        p_tbl = doc.add_table(rows=1, cols=2)
        p_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
        p_tbl.autofit = False
        p_tbl.columns[0].width = Inches(3.2)
        p_tbl.columns[1].width = Inches(3.2)
        arrow_item = [x for x in prop_items if 'Tour_Navigation_Arrow' in x['filename']]
        globe_item = [x for x in prop_items if 'Globe_Meridians' in x['filename']]
        if arrow_item and os.path.exists(arrow_item[0]['abs_path']):
            safe_add_picture(p_tbl.cell(0, 0).paragraphs[0], arrow_item[0]['abs_path'], width=Inches(2.6))
            cap = p_tbl.cell(0, 0).add_paragraph("Figure 4.4.1: VR Teleport Navigation Arrow")
            cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
            cap.runs[0].font.size = Pt(8)
            cap.runs[0].font.italic = True
        if globe_item and os.path.exists(globe_item[0]['abs_path']):
            safe_add_picture(p_tbl.cell(0, 1).paragraphs[0], globe_item[0]['abs_path'], width=Inches(2.6))
            cap = p_tbl.cell(0, 1).add_paragraph("Figure 4.4.2: VR Tour Interactive Portal Globe")
            cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
            cap.runs[0].font.size = Pt(8)
            cap.runs[0].font.italic = True
        doc.add_paragraph().paragraph_format.space_after = Pt(6)

    render_category_table(prop_items)

    # -------------------------------------------------------------
    # DOMAIN 4.5: 360-DEGREE VR TOUR NODES (CAT-07)
    # -------------------------------------------------------------
    doc.add_page_break()
    h4_5 = doc.add_heading("4.5 360-Degree Panoramic VR Tour Nodes (CAT-07)", level=2)
    h4_5.style.font.color.rgb = COLOR_SECONDARY

    doc.add_paragraph(
        "The Virtual Tour module is constructed on a connected graph of 91 ultra-high-resolution equirectangular "
        "panoramas photographed on-site at Yarang. Panoramas are clustered into four major archaeological sectors: "
        "Ban Ja-Lae Site 8 (26 nodes), Ban Wat Site 9 (20 nodes), Ban Ja-Lae Site 3 (20 nodes), and Ban Ja-Lae Site 2 (14 nodes), "
        "complemented by environment calibration nodes."
    )

    # Embed a representative panorama preview using safe_add_picture
    pan_items = cat_groups['CAT-07']
    bj8_pan = [x for x in pan_items if '8B1' in x['filename'] or '8C1' in x['filename']]
    if bj8_pan and os.path.exists(bj8_pan[0]['abs_path']):
        p_img = doc.add_paragraph()
        p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
        pic = safe_add_picture(p_img, bj8_pan[0]['abs_path'], width=Inches(5.8), max_dim=2048)
        if pic:
            p_cap = doc.add_paragraph()
            p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p_cap.paragraph_format.space_after = Pt(8)
            r_cap = p_cap.add_run("Figure 4.5.1: Equirectangular 360° Spherical Environment at Ban Ja-Lae Site 8 (Node 8B1)")
            r_cap.font.size = Pt(8.5)
            r_cap.font.italic = True
            r_cap.font.color.rgb = COLOR_MUTED

    render_category_table(pan_items)

    # -------------------------------------------------------------
    # DOMAIN 4.6: USER GUIDE & MANUAL MEDIA (CAT-08 & CAT-09)
    # -------------------------------------------------------------
    doc.add_page_break()
    h4_6 = doc.add_heading("4.6 User Guide & Operational Manual Media (CAT-08 & CAT-09)", level=2)
    h4_6.style.font.color.rgb = COLOR_SECONDARY

    doc.add_paragraph(
        "The user onboarding and operational instruction system is supported by 112 localized screenshots (EN/TH) "
        "covering five interactive modules: Getting Started, VR Tour, VR Museum, Mixed Reality, and AI Guide. "
        "In addition, 56 step-by-step workflow illustrations (CAT-09) provide controller grip and raycast guidance."
    )

    doc.add_heading("CAT-08: Localized Manual Screenshots (112 Files):", level=3)
    render_category_table(cat_groups['CAT-08'])

    doc.add_heading("CAT-09: Step-by-Step Controller Interaction Workflows (56 Files):", level=3)
    render_category_table(cat_groups['CAT-09'])

    # -------------------------------------------------------------
    # DOMAIN 4.7: ACADEMIC THESIS REPORT FIGURES (CAT-10)
    # -------------------------------------------------------------
    doc.add_page_break()
    h4_7 = doc.add_heading("4.7 Academic Thesis & Senior Project Report Figures (CAT-10)", level=2)
    h4_7.style.font.color.rgb = COLOR_SECONDARY

    doc.add_paragraph(
        "Publication-quality system diagrams, architectural workflows, sequence diagrams, code implementation figures, "
        "and the Prince of Songkla University institutional emblem authored for the senior project report deliverable."
    )

    # Embed seal and system diagram
    rep_items = cat_groups['CAT-10']
    seal_item = [x for x in rep_items if 'psu-seal' in x['filename']]
    diag_item = [x for x in rep_items if 'figure_3_1.' in x['filename']]

    if seal_item and diag_item and os.path.exists(seal_item[0]['abs_path']) and os.path.exists(diag_item[0]['abs_path']):
        fig_tbl = doc.add_table(rows=1, cols=2)
        fig_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
        fig_tbl.autofit = False
        fig_tbl.columns[0].width = Inches(2.2)
        fig_tbl.columns[1].width = Inches(4.3)

        c0 = fig_tbl.cell(0, 0)
        p0 = c0.paragraphs[0]
        p0.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p0.add_run().add_picture(seal_item[0]['abs_path'], width=Inches(1.5))
        cap0 = c0.add_paragraph("Figure 4.7.1: PSU University Seal")
        cap0.alignment = WD_ALIGN_PARAGRAPH.CENTER
        cap0.runs[0].font.size = Pt(8)

        c1 = fig_tbl.cell(0, 1)
        p1 = c1.paragraphs[0]
        p1.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p1.add_run().add_picture(diag_item[0]['abs_path'], width=Inches(4.0))
        cap1 = c1.add_paragraph("Figure 4.7.2: System Architecture Framework")
        cap1.alignment = WD_ALIGN_PARAGRAPH.CENTER
        cap1.runs[0].font.size = Pt(8)

        doc.add_paragraph().paragraph_format.space_after = Pt(8)

    render_category_table(rep_items)

    # -------------------------------------------------------------
    # DOMAIN 4.8: STORE DISTRIBUTION & BRANDING (CAT-11)
    # -------------------------------------------------------------
    doc.add_page_break()
    h4_8 = doc.add_heading("4.8 Horizon Store, Distribution & Branding Assets (CAT-11)", level=2)
    h4_8.style.font.color.rgb = COLOR_SECONDARY

    doc.add_paragraph(
        "Commercial and distribution imagery formatted to Meta Quest Horizon Store submission guidelines, "
        "including high-resolution app icons (512x512, 1024x1024), square display covers (1440x1440), "
        "and widescreen landscape hero banners (2560x1440)."
    )

    str_items = cat_groups['CAT-11']
    hero_item = [x for x in str_items if 'LandscapeCover' in x['filename'] and x['ext'] == 'PNG']
    if hero_item and os.path.exists(hero_item[0]['abs_path']):
        try:
            p_img = doc.add_paragraph()
            p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p_img.add_run().add_picture(hero_item[0]['abs_path'], width=Inches(5.8))
            p_cap = doc.add_paragraph()
            p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p_cap.paragraph_format.space_after = Pt(8)
            r_cap = p_cap.add_run("Figure 4.8.1: LangkaLens Landscape Store Hero Banner (2560x1440)")
            r_cap.font.size = Pt(8.5)
            r_cap.font.italic = True
            r_cap.font.color.rgb = COLOR_MUTED
        except Exception as e:
            print(f"Error embedding store hero: {e}")

    render_category_table(str_items)

    # -------------------------------------------------------------
    # DOMAIN 4.9: IN-GAME UI, LOGOS & HUD (CAT-12)
    # -------------------------------------------------------------
    h4_9 = doc.add_heading("4.9 In-Game UI, Menus, Thumbnails & HUD Sprites (CAT-12)", level=2)
    h4_9.style.font.color.rgb = COLOR_SECONDARY

    doc.add_paragraph(
        "User interface controls, mode selection icons (VR Museum, VR Tour, Mixed Reality), "
        "multilingual switcher toggles, institutional sponsor logos (TSRI, Thairise, Faculty of Communication Sciences), "
        "and 3D model selection thumbnail cards."
    )
    render_category_table(cat_groups['CAT-12'])

    # -------------------------------------------------------------
    # DOMAIN 4.10: MATERIAL TEXTURES & PBR MAPS (CAT-13)
    # -------------------------------------------------------------
    doc.add_page_break()
    h4_10 = doc.add_heading("4.10 Material Textures & PBR Shading Maps (CAT-13)", level=2)
    h4_10.style.font.color.rgb = COLOR_SECONDARY

    doc.add_paragraph(
        "Physically Based Rendering (PBR) texture maps configured for Unity Universal Render Pipeline (URP) shaders. "
        "Consists of calibrated Diffuse/Albedo (_d), Normal Maps (_n), and Specular/Smoothness (_s) maps in TGA and PNG formats."
    )
    render_category_table(cat_groups['CAT-13'])

    # -------------------------------------------------------------
    # DOMAIN 4.11: PLAYTESTING, QA & ENGINE UTILITIES (CAT-14 & CAT-15)
    # -------------------------------------------------------------
    h4_11 = doc.add_heading("4.11 Evaluation Studies, QA & Engine Utilities (CAT-14 & CAT-15)", level=2)
    h4_11.style.font.color.rgb = COLOR_SECONDARY

    doc.add_paragraph(
        "User testing session captures, VR playtesting records, and supporting TextMesh Pro font atlases and utility sprites."
    )

    doc.add_heading("CAT-14: User Evaluation & Playtesting Captures (13 Files):", level=3)
    render_category_table(cat_groups['CAT-14'])

    doc.add_heading("CAT-15: Engine Utilities & Supporting Asset Textures (25 Files):", level=3)
    render_category_table(cat_groups['CAT-15'])

    # ==========================================
    # SECTION 5: COMPLETE MASTER CATALOG INDEX
    # ==========================================
    doc.add_page_break()
    h5 = doc.add_heading("5. Master Standardized Catalog Index (All 512 Assets)", level=1)
    h5.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph(
        "The following master index presents all 512 image files in the LangkaLens project repository, "
        "ordered systematically by Standard Catalog ID. This table serves as the definitive reference for "
        "Addressable asset keys, code citations, and publication inventories."
    )

    # Master Table sorted by ID
    all_sorted_by_id = sorted(records, key=lambda x: x['id'])
    render_category_table(all_sorted_by_id)

    # Ensure output dir exists
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    doc.save(output_path)
    print(f"\n[OK] Successfully compiled Master DOCX: {output_path}")
    print(f"     Total Assets Cataloged: {len(records)}")
    print(f"     File Size: {os.path.getsize(output_path) / (1024 * 1024):.2f} MB")


def build_markdown_catalog(records, output_path="Docs/IMAGE_CATALOG.md"):
    """Generate Markdown companion catalog for in-repo code navigation."""
    cat_groups = defaultdict(list)
    for r in records:
        cat_groups[r['cat_code']].append(r)

    lines = []
    lines.append("# LangkaLens Master Image & Visual Assets Catalog")
    lines.append("")
    lines.append(f"> **Automated Repository Audit & Orderly Taxonomy**  ")
    lines.append(f"> **Compiled**: {datetime.now().strftime('%B %Y')} | **Total Visual Assets**: {len(records)} Files | **Target Hardware**: Meta Quest 3 & 3S")
    lines.append("")
    lines.append("---")
    lines.append("")
    lines.append("## 1. Executive Summary & Inventory Matrix")
    lines.append("")
    lines.append("| Code | Category Domain | Total Assets | Volume | Formats | Target Experience Module |")
    lines.append("| :--- | :--- | :--- | :--- | :--- | :--- |")

    total_bytes = sum(r['size_bytes'] for r in records)
    for k in sorted(cat_groups.keys()):
        items = cat_groups[k]
        cat_bytes = sum(x['size_bytes'] for x in items)
        cat_vol = f"{cat_bytes / (1024 * 1024):.1f} MB" if cat_bytes >= 1024*1024 else f"{cat_bytes/1024:.0f} KB"
        exts_str = "/".join(sorted(set(x['ext'] for x in items)))
        lines.append(f"| **{k}** | {items[0]['cat_title']} | {len(items)} | {cat_vol} | {exts_str} | {items[0]['target_subsys']} |")

    tot_vol = f"{total_bytes / (1024*1024):.1f} MB"
    lines.append(f"| **TOTAL** | **All 15 Visual Domains** | **{len(records)}** | **{tot_vol}** | **PNG / JPG / TGA** | **Full Project Scope** |")
    lines.append("")
    lines.append("---")
    lines.append("")
    lines.append("## 2. Orderly Naming Syntax Standard")
    lines.append("")
    lines.append("```")
    lines.append("LL-IMG-[DOMAIN]-[SUBGROUP]-[DESCRIPTOR]-[VARIANT].[ext]")
    lines.append("```")
    lines.append("")
    lines.append("- `LL-IMG-ART-01-BUDDHA-ORG.png` — Excavated standing Buddha weathered fragment")
    lines.append("- `LL-IMG-CMP-01-BUDDHA.png` — Side-by-side original vs restored 1920x1080 comparison sheet")
    lines.append("- `LL-IMG-PAN-BJ8-B01.jpg` — 360° equirectangular node at Ban Ja-Lae Site 8")
    lines.append("- `LL-IMG-STR-COVER-LANDSCAPE-2560.png` — Meta Quest Store 2560x1440 hero banner")
    lines.append("")
    lines.append("---")
    lines.append("")

    for k in sorted(cat_groups.keys()):
        items = cat_groups[k]
        lines.append(f"## {k}: {items[0]['cat_title']}")
        lines.append("")
        lines.append(f"*Total Assets: {len(items)} files | Target Subsystem: {items[0]['target_subsys']}*")
        lines.append("")
        lines.append("| Standard Catalog ID | Project File Path | Resolution | Size | Format | Color Mode |")
        lines.append("| :--- | :--- | :--- | :--- | :--- | :--- |")
        for item in items:
            res_str = f"{item['width']}x{item['height']}" if item['width'] > 0 else "Vector"
            lines.append(f"| `{item['id']}` | [{item['filename']}]({item['rel_path']}) | {res_str} | {item['size_str']} | {item['ext']} | {item['mode']} |")
        lines.append("")
        lines.append("---")
        lines.append("")

    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write("\n".join(lines))
    print(f"[OK] Successfully written Markdown companion: {output_path}")


if __name__ == "__main__":
    print("=" * 70)
    print("  LangkaLens Image Catalog & Orderly Taxonomy Generator")
    print("=" * 70)
    records = scan_all_images()
    print(f"Scanned {len(records)} total project images.")

    docx_target = "Docs/LangkaLens_Image_Catalog.docx"
    md_target = "Docs/IMAGE_CATALOG.md"

    build_catalog_docx(records, docx_target)
    build_markdown_catalog(records, md_target)

    # Convert to PDF
    pdf_target = "Docs/LangkaLens_Image_Catalog.pdf"
    print("\nTriggering high-fidelity PDF export via Microsoft Word COM...")
    try:
        from pathlib import Path
        sys.path.append(os.path.abspath(".agents/skills/docx/scripts"))
        from export_pdf import export_to_pdf
        export_to_pdf(docx_target, pdf_target)
    except Exception as e:
        print(f"PDF export notice: {e}")
