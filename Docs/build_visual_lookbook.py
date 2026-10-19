"""LangkaLens Visual Asset Lookbook & Image Gallery Generator.

Compiles an illustrated Microsoft Word (.docx) document and camera-ready PDF
visually displaying all 512 image assets in the LangkaLens project repository
using structured 2-column photo cards, side-by-side comparisons, full-width showcase plates,
and photographic contact sheets.
"""

import os
import sys
import io
import re
from datetime import datetime
from collections import defaultdict
from PIL import Image

import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import parse_xml
from docx.oxml.ns import nsdecls

sys.stdout.reconfigure(encoding='utf-8')

# Color Palette Constants
HEX_PRIMARY_NAVY = "1F497D"
HEX_SECONDARY_NAVY = "2B579A"
HEX_LIGHT_ROW = "F8F9FA"
HEX_CARD_BG = "FFFFFF"
HEX_BORDER = "E1E4E8"
HEX_BORDER_DARK = "1F497D"
HEX_ACCENT_GOLD = "C59B27"
HEX_TAG_RED = "A82828"
HEX_TAG_GREEN = "1E7E34"

COLOR_PRIMARY = RGBColor(0x1F, 0x49, 0x7D)
COLOR_SECONDARY = RGBColor(0x2B, 0x57, 0x9A)
COLOR_TEXT_DARK = RGBColor(0x24, 0x29, 0x2F)
COLOR_MUTED = RGBColor(0x58, 0x60, 0x69)
COLOR_WHITE = RGBColor(0xFF, 0xFF, 0xFF)
COLOR_GOLD = RGBColor(0xC5, 0x9B, 0x27)


def set_cell_background(cell, fill_hex: str):
    """Set cell shading background color."""
    tcPr = cell._element.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex.strip("#")}"/>')
    tcPr.append(shd)


def set_cell_margins(cell, top=80, bottom=80, left=100, right=100):
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


def set_cell_borders(cell, top="E1E4E8", bottom="E1E4E8", left="E1E4E8", right="E1E4E8", sz="4"):
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


def make_clean_thumbnail_buffer(image_path: str, max_dim=(800, 800)):
    """Generate in-memory thumbnail buffer, stripping corrupt EXIF and handling TGA/PNG/JPEG."""
    try:
        with Image.open(image_path) as im:
            im_copy = im.copy()
            im_copy.thumbnail(max_dim, Image.Resampling.LANCZOS)
            buf = io.BytesIO()

            # Preserve alpha for transparent PNGs
            if im_copy.mode in ('RGBA', 'LA') or (im_copy.mode == 'P' and 'transparency' in im_copy.info):
                im_copy.convert('RGBA').save(buf, format='PNG', optimize=True)
            else:
                im_copy.convert('RGB').save(buf, format='JPEG', quality=84, optimize=True)

            buf.seek(0)
            return buf
    except Exception as e:
        print(f"Warning: Failed to thumbnail {image_path}: {e}")
        return None


def generate_unique_catalog_id(rel_path: str) -> str:
    """Generate a clean, standardized, unique Catalog ID for each image file."""
    p = rel_path.replace(os.sep, '/')
    fname = os.path.basename(p)
    name_no_ext, ext = os.path.splitext(fname)
    ext_tag = ext.strip('.').upper()

    if 'Docs/Images/01_Museum_Artifacts' in p:
        return f'LL-IMG-ART-{name_no_ext.upper()}'
    elif 'Docs/Images/02_Artifact_Comparisons' in p:
        clean = name_no_ext.replace('_Comparison', '').upper()
        return f'LL-IMG-CMP-{clean}'
    elif 'Docs/Images/03_Historic_Reconstructions' in p:
        clean = name_no_ext.replace('OG_', '').replace('_', '-').upper()
        return f'LL-IMG-REC-{clean}'
    elif 'Docs/Images/04_Museum_Demo_Exhibits' in p:
        clean = name_no_ext.replace('_', '-').upper()
        return f'LL-IMG-DEM-{clean}'
    elif 'Docs/Images/05_Interactive_Props_And_Icons' in p:
        clean = name_no_ext.replace('_3D', '').replace('_', '-').upper()
        return f'LL-IMG-PROP-{clean}'
    elif 'Docs/Images/06_Photogrammetry_Assets' in p:
        clean = name_no_ext.replace('_', '-').upper()
        return f'LL-IMG-SCAN-{clean}'
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
    elif 'User_Guide_Screenshots' in p or 'Screenshots/UserGuides' in p:
        sub = 'DOCS' if 'Docs/Screenshots/UserGuides' in p else 'MANUAL'
        clean = name_no_ext.replace('_', '-').upper()
        return f'LL-IMG-GUIDE-{sub}-{clean}'
    elif 'User_Guide_Pictures' in p or 'Guide images' in p:
        sub = 'DOCS' if 'Docs/User_Guide_Pictures' in p else 'ASSETS'
        clean = name_no_ext.replace('_', '-').upper()
        return f'LL-IMG-WF-{sub}-{clean}'
    elif 'Docs/Report/figures' in p:
        sub = os.path.basename(os.path.dirname(p)).upper()
        clean = name_no_ext.replace('_', '-').upper()
        return f'LL-IMG-REP-{sub}-{clean}'
    elif 'StoreAssets' in p or (os.path.dirname(p) == '' and 'LangkaLens' in p):
        sub = 'STORE' if 'StoreAssets' in p else 'ROOT'
        clean = name_no_ext.replace('LangkaLens_', '').replace('_', '-').upper()
        return f'LL-IMG-STR-{sub}-{clean}-{ext_tag}'
    elif 'Main_menu/Images' in p or 'Misc/Images' in p or 'Tour/Images' in p:
        parts = p.split('/')
        sub = parts[-2].upper() if len(parts) >= 2 else 'MAIN'
        clean = re.sub(r'[^a-zA-Z0-9]+', '-', name_no_ext).strip('-').upper()
        return f'LL-IMG-UI-{sub}-{clean}-{ext_tag}'
    elif 'Textures' in p:
        parent = os.path.basename(os.path.dirname(p)).upper()
        clean = name_no_ext.replace('_', '-').upper()
        return f'LL-IMG-MAT-{parent}-{clean}'
    elif 'Screenshots/Playtesting' in p or 'Screenshots/Development' in p:
        sub = 'PLAYTEST' if 'Playtesting' in p else 'DEV'
        clean = re.sub(r'[^a-zA-Z0-9]+', '-', name_no_ext).strip('-').upper()
        return f'LL-IMG-EVL-{sub}-{clean}'
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
    size_str = f"{size_bytes / (1024 * 1024):.2f} MB" if size_bytes >= 1024 * 1024 else f"{size_bytes / 1024:.1f} KB"
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


# -------------------------------------------------------------
# DOCX VISUAL CARD HELPERS
# -------------------------------------------------------------

def add_visual_card(cell, item, width=Inches(3.2), max_dim=(800, 800), badge_text="", badge_color=""):
    """Render a single visual asset card inside a table cell."""
    set_cell_background(cell, HEX_CARD_BG)
    set_cell_margins(cell, top=60, bottom=60, left=70, right=70)
    set_cell_borders(cell, top=HEX_BORDER, bottom=HEX_BORDER, left=HEX_BORDER, right=HEX_BORDER, sz="4")

    cp = cell.paragraphs[0]
    cp.alignment = WD_ALIGN_PARAGRAPH.CENTER
    cp.paragraph_format.space_before = Pt(2)
    cp.paragraph_format.space_after = Pt(4)

    # Insert Image
    buf = make_clean_thumbnail_buffer(item['abs_path'], max_dim=max_dim)
    if buf:
        cp.add_run().add_picture(buf, width=width)
    else:
        r_err = cp.add_run("[Image Preview Unavailable]")
        r_err.font.size = Pt(8.5)
        r_err.font.italic = True

    # Card Footer / Metadata Caption
    p_meta = cell.add_paragraph()
    p_meta.paragraph_format.space_before = Pt(2)
    p_meta.paragraph_format.space_after = Pt(2)
    p_meta.paragraph_format.line_spacing = 1.1

    # Optional badge
    if badge_text:
        r_b = p_meta.add_run(f"[{badge_text}] ")
        r_b.font.size = Pt(8)
        r_b.bold = True
        if badge_color == "RED":
            r_b.font.color.rgb = RGBColor(0xA8, 0x28, 0x28)
        elif badge_color == "GREEN":
            r_b.font.color.rgb = RGBColor(0x1E, 0x7E, 0x34)
        elif badge_color == "BLUE":
            r_b.font.color.rgb = COLOR_PRIMARY

    # Catalog ID
    r_id = p_meta.add_run(item['id'] + "\n")
    r_id.font.name = "Consolas"
    r_id.font.size = Pt(8)
    r_id.bold = True
    r_id.font.color.rgb = COLOR_PRIMARY

    # Technical specs line
    res_str = f"{item['width']}x{item['height']}" if item['width'] > 0 else "Vector"
    r_spec = p_meta.add_run(f"{res_str}  |  {item['size_str']}  |  {item['ext']} ({item['mode']})\n")
    r_spec.font.size = Pt(7.5)
    r_spec.font.color.rgb = COLOR_MUTED

    # Filename
    r_fn = p_meta.add_run(item['filename'])
    r_fn.font.name = "Consolas"
    r_fn.font.size = Pt(7)
    r_fn.font.color.rgb = COLOR_TEXT_DARK


def render_2column_gallery(doc, items, width=Inches(3.3), max_dim=(800, 800)):
    """Renders a collection of items into a 2-column visual grid."""
    num_items = len(items)
    for i in range(0, num_items, 2):
        row_items = items[i:i+2]
        tbl = doc.add_table(rows=1, cols=2)
        tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
        tbl.autofit = False
        tbl.columns[0].width = Inches(3.45)
        tbl.columns[1].width = Inches(3.45)

        # First item
        add_visual_card(tbl.cell(0, 0), row_items[0], width=width, max_dim=max_dim)

        # Second item (if exists)
        if len(row_items) > 1:
            add_visual_card(tbl.cell(0, 1), row_items[1], width=width, max_dim=max_dim)
        else:
            set_cell_background(tbl.cell(0, 1), HEX_LIGHT_ROW)
            set_cell_borders(tbl.cell(0, 1), top="none", bottom="none", left="none", right="none")

        p_spacer = doc.add_paragraph()
        p_spacer.paragraph_format.space_before = Pt(1)
        p_spacer.paragraph_format.space_after = Pt(2)


def render_fullwidth_plate(doc, item, caption_title="", width=Inches(6.8), max_dim=(1920, 1080)):
    """Render a large showcase widescreen plate."""
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl.autofit = False
    tbl.columns[0].width = Inches(7.0)

    cell = tbl.cell(0, 0)
    set_cell_background(cell, HEX_CARD_BG)
    set_cell_margins(cell, top=80, bottom=80, left=100, right=100)
    set_cell_borders(cell, top=HEX_BORDER, bottom=HEX_BORDER, left=HEX_BORDER, right=HEX_BORDER, sz="6")

    p_img = cell.paragraphs[0]
    p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_img.paragraph_format.space_before = Pt(4)
    p_img.paragraph_format.space_after = Pt(4)

    buf = make_clean_thumbnail_buffer(item['abs_path'], max_dim=max_dim)
    if buf:
        p_img.add_run().add_picture(buf, width=width)
    else:
        p_img.add_run("[Image Preview Unavailable]").font.italic = True

    p_cap = cell.add_paragraph()
    p_cap.paragraph_format.space_before = Pt(4)
    p_cap.paragraph_format.space_after = Pt(2)

    if caption_title:
        r_t = p_cap.add_run(caption_title + "\n")
        r_t.font.size = Pt(9.5)
        r_t.bold = True
        r_t.font.color.rgb = COLOR_PRIMARY

    r_id = p_cap.add_run(item['id'] + "  —  ")
    r_id.font.name = "Consolas"
    r_id.font.size = Pt(8.5)
    r_id.bold = True
    r_id.font.color.rgb = COLOR_SECONDARY

    res_str = f"{item['width']}x{item['height']}" if item['width'] > 0 else "Vector"
    r_sp = p_cap.add_run(f"Resolution: {res_str} | File Size: {item['size_str']} | Format: {item['ext']} | Source: {item['rel_path']}")
    r_sp.font.size = Pt(8)
    r_sp.font.color.rgb = COLOR_MUTED

    p_div = doc.add_paragraph()
    p_div.paragraph_format.space_before = Pt(2)
    p_div.paragraph_format.space_after = Pt(4)


# -------------------------------------------------------------
# MAIN BUILD LOOKBOOK FUNCTION
# -------------------------------------------------------------

def build_visual_lookbook(records, output_path="Docs/LangkaLens_Visual_Asset_Lookbook.docx"):
    """Assemble complete high-standard Illustrated Visual Lookbook."""
    doc = docx.Document()
    sec = doc.sections[0]
    # Widescreen-optimized margins (0.75" on all sides = 7.0" printable width)
    sec.top_margin = Inches(0.75)
    sec.bottom_margin = Inches(0.75)
    sec.left_margin = Inches(0.75)
    sec.right_margin = Inches(0.75)
    sec.different_first_page_header_footer = True

    # Header / Footer
    header = sec.header
    hp = header.paragraphs[0]
    hp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    hr = hp.add_run("LangkaLens | Illustrated Visual Asset Lookbook")
    hr.font.name = "Segoe UI"
    hr.font.size = Pt(8.5)
    hr.font.color.rgb = RGBColor(0x88, 0x88, 0x88)

    footer = sec.footer
    fp = footer.paragraphs[0]
    fp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    fr1 = fp.add_run("LangkaLens Project — Meta Quest 3/3S VR/MR Visual Lookbook  |  Page ")
    fr1.font.name = "Segoe UI"
    fr1.font.size = Pt(8.5)
    fr1.font.color.rgb = RGBColor(0x88, 0x88, 0x88)

    # Document Styles
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
    r_sub = p_sub.add_run("Complete Visual Asset Lookbook & Illustrated Image Gallery")
    r_sub.font.name = "Segoe UI"
    r_sub.font.size = Pt(15)
    r_sub.font.color.rgb = COLOR_MUTED

    # Meta banner box
    meta_tbl = doc.add_table(rows=1, cols=4)
    meta_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    meta_tbl.autofit = False
    col_widths = [Inches(1.75), Inches(1.75), Inches(1.75), Inches(1.75)]
    for row in meta_tbl.rows:
        for idx, width in enumerate(col_widths):
            row.cells[idx].width = width

    meta_items = [
        ("ILLUSTRATED ASSETS", f"{len(records)} Images Shown"),
        ("VISUAL DOMAINS", "15 Curated Categories"),
        ("TARGET HARDWARE", "Meta Quest 3 & 3S"),
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

    doc.add_paragraph().paragraph_format.space_after = Pt(10)

    # Group records by category
    cat_groups = defaultdict(list)
    for r in records:
        cat_groups[r['cat_code']].append(r)

    # ==========================================
    # DOMAIN 1: MUSEUM ARTIFACTS & COMPARISON PLATES
    # ==========================================
    h1 = doc.add_heading("1. Museum Archaeological Artifacts & Restorations (CAT-01 & CAT-02)", level=1)
    h1.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph(
        "This section visually presents the 11 key archaeological artifacts from the Yarang Ancient City complex. "
        "Each artifact is paired directly: Left displays the weathered, fragmented excavated condition (_ORG), and "
        "Right displays the 3D reconstructed museum exhibit state (_RES), followed by widescreen scholarly comparison plates."
    )

    # 1.1 Side-by-Side Artifact Pairs
    art_items = cat_groups['CAT-01']
    # Group into pairs by base name
    artifact_names = [
        ("Buddha", "Standing Buddha (พระพุทธรูปยืนศิลปะทวารวดี)"),
        ("Cow", "Nandi Sacred Bull (ประติมากรรมโคนันทิ)"),
        ("Pillar", "Carved Stone Pillar (เสาหินสลักโบราณ)"),
        ("Statue", "Standing Bodhisattva / Deity (เทวรูปโบราณ)"),
        ("Stupa1", "Stupa Model Type 1 (สถูปจำลอง แบบที่ 1)"),
        ("Stupa2", "Stupa Model Type 2 (สถูปจำลอง แบบที่ 2)"),
        ("Stupa3", "Stupa Model Type 3 (สถูปจำลอง แบบที่ 3)"),
        ("Stupa4", "Stupa Model Type 4 (สถูปจำลอง แบบที่ 4)"),
        ("Tri", "Surya Stele / Trinity Relief (แผ่นสลักสุริยเทพ / ตรีมูรติ)"),
        ("Turtle", "Kurma Sacred Turtle (ประติมากรรมเต่ามงคล)"),
        ("Wheel", "Dharmachakra Law Wheel (ธรรมจักรศิลา)"),
    ]

    for base_tag, title_desc in artifact_names:
        org_item = [x for x in art_items if f"{base_tag}_ORG" in x['filename']]
        res_item = [x for x in art_items if f"{base_tag}_RES" in x['filename']]

        if org_item and res_item:
            h_art = doc.add_heading(f"Artifact Exhibit: {title_desc}", level=2)
            h_art.style.font.color.rgb = COLOR_SECONDARY

            tbl = doc.add_table(rows=1, cols=2)
            tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
            tbl.autofit = False
            tbl.columns[0].width = Inches(3.45)
            tbl.columns[1].width = Inches(3.45)

            add_visual_card(tbl.cell(0, 0), org_item[0], width=Inches(3.1), badge_text="EXCAVATED FRAGMENT", badge_color="RED")
            add_visual_card(tbl.cell(0, 1), res_item[0], width=Inches(3.1), badge_text="DIGITALLY RESTORED", badge_color="GREEN")

            p_div = doc.add_paragraph()
            p_div.paragraph_format.space_before = Pt(2)
            p_div.paragraph_format.space_after = Pt(4)

    # 1.2 Widescreen Comparison Plates
    doc.add_page_break()
    h1_2 = doc.add_heading("1.2 Widescreen Side-by-Side Comparison Plates (CAT-02)", level=2)
    h1_2.style.font.color.rgb = COLOR_SECONDARY

    doc.add_paragraph(
        "High-definition 1920x1080 composite comparison sheets authored for senior project thesis defense and academic verification."
    )

    cmp_items = cat_groups['CAT-02']
    for item in cmp_items:
        clean_name = item['filename'].replace('_Comparison.png', '').replace('_', ' ')
        render_fullwidth_plate(doc, item, caption_title=f"Widescreen Comparison Plate: {clean_name}")

    # ==========================================
    # DOMAIN 2: HISTORIC ARCHITECTURAL RECONSTRUCTIONS
    # ==========================================
    doc.add_page_break()
    h2 = doc.add_heading("2. Historic Monument Architectural Reconstructions (CAT-03)", level=1)
    h2.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph(
        "Full 3D architectural reconstructions of ancient stupas, sanctuary bases, and temple compounds excavated at Yarang. "
        "Utilized in the 360 VR Tour and Mixed Reality spatial alignment."
    )
    rec_items = cat_groups['CAT-03']
    render_2column_gallery(doc, rec_items, width=Inches(3.2))

    # ==========================================
    # DOMAIN 3: MUSEUM DEMO EXHIBITS & PHOTOGRAMMETRY
    # ==========================================
    doc.add_page_break()
    h3 = doc.add_heading("3. Museum Gallery Demo Exhibits & Photogrammetric Scans (CAT-04 & CAT-06)", level=1)
    h3.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph("Ancillary museum exhibits including glazed ceramics, terracotta pottery, and LOD photogrammetric environmental scans.")

    doc.add_heading("3.1 Gallery Demo Exhibits (CAT-04):", level=2)
    render_2column_gallery(doc, cat_groups['CAT-04'], width=Inches(3.2))

    doc.add_heading("3.2 Photogrammetric Architectural Scans (CAT-06):", level=2)
    render_2column_gallery(doc, cat_groups['CAT-06'], width=Inches(3.2))

    # ==========================================
    # DOMAIN 4: INTERACTIVE 3D PROPS & NAVIGATION
    # ==========================================
    doc.add_page_break()
    h4 = doc.add_heading("4. Interactive 3D Props & Navigation Elements (CAT-05)", level=1)
    h4.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph("3D interactive assets engineered for Meta Interaction SDK gestures, teleportation, and museum display pedestals.")
    render_2column_gallery(doc, cat_groups['CAT-05'], width=Inches(3.2))

    # ==========================================
    # DOMAIN 5: 360-DEGREE VR TOUR NODE ATLAS
    # ==========================================
    doc.add_page_break()
    h5 = doc.add_heading("5. 360-Degree Panoramic VR Tour Node Atlas (CAT-07)", level=1)
    h5.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph(
        "Complete visual contact sheet of all 91 equirectangular spherical panoramas comprising the LangkaLens VR Tour. "
        "Each panorama is captured on-location and cataloged by archaeological sector."
    )

    pan_items = cat_groups['CAT-07']
    # Subdivide by sector
    sectors = [
        ("Ban Ja-Lae Site 8 (26 Nodes)", lambda x: 'BJ8' in x['id']),
        ("Ban Wat Site 9 (20 Nodes)", lambda x: 'BW9' in x['id']),
        ("Ban Ja-Lae Site 3 (20 Nodes)", lambda x: 'BJ3' in x['id']),
        ("Ban Ja-Lae Site 2 (14 Nodes)", lambda x: 'BJ2' in x['id']),
        ("Environment & Calibration Nodes (11 Nodes)", lambda x: 'ENV' in x['id'] or 'TEST' in x['id'] or 'MISC' in x['id']),
    ]

    for sec_title, filter_fn in sectors:
        sec_nodes = [x for x in pan_items if filter_fn(x)]
        if sec_nodes:
            h_sec = doc.add_heading(f"Tour Sector: {sec_title}", level=2)
            h_sec.style.font.color.rgb = COLOR_SECONDARY
            render_2column_gallery(doc, sec_nodes, width=Inches(3.3), max_dim=(800, 400))

    # ==========================================
    # DOMAIN 6: USER GUIDE LOCALIZED MEDIA & WORKFLOWS
    # ==========================================
    doc.add_page_break()
    h6 = doc.add_heading("6. User Guide Localized Screens & Workflow Pictures (CAT-08 & CAT-09)", level=1)
    h6.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph(
        "The onboarding instruction system features dual-language localized screens (CAT-08) "
        "and step-by-step visual workflow illustrations (CAT-09) guiding user hand-tracking and controller interactions."
    )

    doc.add_heading("6.1 User Guide Localized Screens (CAT-08, 112 Images):", level=2)
    render_2column_gallery(doc, cat_groups['CAT-08'], width=Inches(3.3))

    doc.add_page_break()
    doc.add_heading("6.2 Step-by-Step Controller Workflows (CAT-09, 56 Images):", level=2)
    render_2column_gallery(doc, cat_groups['CAT-09'], width=Inches(3.3))

    # ==========================================
    # DOMAIN 7: ACADEMIC REPORT FIGURES
    # ==========================================
    doc.add_page_break()
    h7 = doc.add_heading("7. Academic Thesis & Senior Project Report Figures (CAT-10)", level=1)
    h7.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph("Architecture diagrams, sequence workflows, code figures, and the official university seal.")
    rep_items = cat_groups['CAT-10']
    for item in rep_items:
        # Full width for diagrams, 2-col or centered for others
        if 'DIAG' in item['id'] or 'CODE' in item['id']:
            render_fullwidth_plate(doc, item, caption_title=f"Report Figure: {item['filename']}")
        else:
            tbl = doc.add_table(rows=1, cols=1)
            tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
            add_visual_card(tbl.cell(0, 0), item, width=Inches(3.5))

    # ==========================================
    # DOMAIN 8: HORIZON STORE ASSETS & BRANDING
    # ==========================================
    doc.add_page_break()
    h8 = doc.add_heading("8. Horizon Store Distribution & Branding Assets (CAT-11)", level=1)
    h8.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph("Commercial artwork and icons formatted for the Meta Quest Horizon Store.")
    str_items = cat_groups['CAT-11']
    for item in str_items:
        if 'LANDSCAPE' in item['id'] or 'COVER' in item['id'] or 'UBA' in item['id']:
            render_fullwidth_plate(doc, item, caption_title=f"Store Deliverable: {item['filename']}")
        else:
            tbl = doc.add_table(rows=1, cols=1)
            tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
            add_visual_card(tbl.cell(0, 0), item, width=Inches(3.2))

    # ==========================================
    # DOMAIN 9: IN-GAME UI, MENUS & HUD ASSETS
    # ==========================================
    doc.add_page_break()
    h9 = doc.add_heading("9. In-Game UI, Menus, Mode Icons & Crests (CAT-12)", level=1)
    h9.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph("Interface buttons, mode selectors, institutional logos, and historical gold frames.")
    render_2column_gallery(doc, cat_groups['CAT-12'], width=Inches(3.2))

    # ==========================================
    # DOMAIN 10: MATERIAL TEXTURES & PBR MAPS
    # ==========================================
    doc.add_page_break()
    h10 = doc.add_heading("10. Material Textures & PBR Shading Maps (CAT-13)", level=1)
    h10.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph("Physically Based Rendering (PBR) texture swatches: Diffuse (_d), Normal (_n), and Specular (_s) maps.")
    render_2column_gallery(doc, cat_groups['CAT-13'], width=Inches(3.2))

    # ==========================================
    # DOMAIN 11: IN-GAME VR SCREENSHOTS & DEVELOPMENT PLAYTESTING
    # ==========================================
    doc.add_page_break()
    h11 = doc.add_heading("11. In-Game VR Screenshots, Simulator Playtesting & Unity Development (CAT-14)", level=1)
    h11.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph(
        "Full-width visual showcase of all in-game gameplay captures, Meta Quest simulator sessions, "
        "and Unity Editor XR development configurations depicting the player's live perspective."
    )

    evl_items = cat_groups['CAT-14']
    for item in evl_items:
        clean_title = item['filename'].replace('.png', '').replace('-', ' ').title()
        render_fullwidth_plate(doc, item, caption_title=f"In-Game Screenshot: {clean_title}", width=Inches(6.8), max_dim=(1920, 1080))

    # Engine Utilities
    doc.add_page_break()
    h12 = doc.add_heading("12. Supporting Engine Utilities & Font Atlases (CAT-15)", level=1)
    h12.style.font.color.rgb = COLOR_PRIMARY

    doc.add_paragraph("TextMesh Pro signed distance field font atlases and supporting engine textures.")
    render_2column_gallery(doc, cat_groups['CAT-15'], width=Inches(3.2))

    # Save document
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    doc.save(output_path)
    file_size_mb = os.path.getsize(output_path) / (1024 * 1024)
    print(f"\n[OK] Successfully compiled Illustrated Lookbook DOCX: {output_path}")
    print(f"     Total Assets Visually Embedded: {len(records)}")
    print(f"     File Size: {file_size_mb:.2f} MB")


if __name__ == "__main__":
    print("=" * 70)
    print("  LangkaLens Illustrated Visual Asset Lookbook Generator")
    print("=" * 70)
    records = scan_all_images()
    print(f"Scanned {len(records)} total project images.")

    lookbook_docx = "Docs/LangkaLens_Visual_Asset_Lookbook.docx"
    lookbook_pdf = "Docs/LangkaLens_Visual_Asset_Lookbook.pdf"

    build_visual_lookbook(records, lookbook_docx)

    # Convert to PDF
    print("\nTriggering high-fidelity PDF export via Microsoft Word COM...")
    try:
        from pathlib import Path
        sys.path.append(os.path.abspath(".agents/skills/docx/scripts"))
        from export_pdf import export_to_pdf
        export_to_pdf(lookbook_docx, lookbook_pdf)
    except Exception as e:
        print(f"PDF export notice: {e}")
