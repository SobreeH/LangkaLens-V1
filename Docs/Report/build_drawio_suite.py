import os
import re
import html
import shutil
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT_DIR = Path(r"e:\Unity\Projects\LangkaLens")
REPORT_DIR = ROOT_DIR / "Docs" / "Report"
OUT_DIR = REPORT_DIR / "extracted_diagrams"
DRAWIO_DIR = OUT_DIR / "drawio"
SVG_DIR = OUT_DIR / "svg"
PNG_DIR = OUT_DIR / "png"
MMD_DIR = OUT_DIR / "mermaid"
DOCX_DIR = OUT_DIR / "from_docx"

for d in [OUT_DIR, DRAWIO_DIR, SVG_DIR, PNG_DIR, MMD_DIR, DOCX_DIR]:
    d.mkdir(parents=True, exist_ok=True)

def clean_html(text):
    if not text:
        return ""
    lines = [html.escape(l.strip()) for l in text.split("\n") if l.strip()]
    return "<br>".join(lines)

def parse_svg_file(svg_path):
    tree = ET.parse(svg_path)
    root = tree.getroot()
    w = float(root.attrib.get('width', 1600))
    h = float(root.attrib.get('height', 900))
    
    rects = []
    ellipses = []
    polygons = []
    texts = []
    paths = []
    
    def parse_path_pts(d_str):
        tokens = re.findall(r'[ML]|-?\d+(?:\.\d+)?', d_str)
        pts = []
        i = 0
        while i < len(tokens):
            if tokens[i] in ('M', 'L') and i + 2 < len(tokens):
                try:
                    pts.append((float(tokens[i+1]), float(tokens[i+2])))
                    i += 3
                except ValueError:
                    i += 1
            else:
                i += 1
        return pts

    def walk(el):
        tag = el.tag.split('}')[-1]
        if tag == 'defs':
            return
        elif tag == 'rect':
            rx = float(el.attrib.get('x', 0))
            ry = float(el.attrib.get('y', 0))
            rw = float(el.attrib.get('width', 0))
            rh = float(el.attrib.get('height', 0))
            stroke = el.attrib.get('stroke', 'none')
            fill = el.attrib.get('fill', 'white')
            dash = el.attrib.get('stroke-dasharray', '')
            rid = el.attrib.get('id', '')
            rects.append({'id': rid, 'x': rx, 'y': ry, 'w': rw, 'h': rh, 'stroke': stroke, 'fill': fill, 'dash': dash})
        elif tag == 'ellipse':
            cx = float(el.attrib.get('cx', 0))
            cy = float(el.attrib.get('cy', 0))
            rx = float(el.attrib.get('rx', 0))
            ry = float(el.attrib.get('ry', 0))
            stroke = el.attrib.get('stroke', '#334155')
            fill = el.attrib.get('fill', 'white')
            ellipses.append({'id': el.attrib.get('id', ''), 'x': cx-rx, 'y': cy-ry, 'w': 2*rx, 'h': 2*ry, 'stroke': stroke, 'fill': fill})
        elif tag == 'polygon':
            points_str = el.attrib.get('points', '')
            pts = [list(map(float, p.split(','))) for p in points_str.strip().split() if ',' in p]
            if pts:
                xs = [p[0] for p in pts]
                ys = [p[1] for p in pts]
                min_x, max_x = min(xs), max(xs)
                min_y, max_y = min(ys), max(ys)
                stroke = el.attrib.get('stroke', '#334155')
                fill = el.attrib.get('fill', 'white')
                polygons.append({'id': el.attrib.get('id', ''), 'x': min_x, 'y': min_y, 'w': max_x - min_x, 'h': max_y - min_y, 'stroke': stroke, 'fill': fill})
        elif tag == 'text':
            tx = float(el.attrib.get('x', 0))
            ty = float(el.attrib.get('y', 0))
            font_size = float(el.attrib.get('font-size', 24))
            font_weight = el.attrib.get('font-weight', '400')
            font_fill = el.attrib.get('fill', '#111827')
            anchor = el.attrib.get('text-anchor', 'middle')
            
            tspans = el.findall('.//{http://www.w3.org/2000/svg}tspan')
            if tspans:
                lines = [t.text for t in tspans if t.text]
                text_content = "\n".join(lines)
            else:
                text_content = el.text or ""
            if text_content.strip():
                texts.append({'text': text_content.strip(), 'x': tx, 'y': ty, 'size': font_size, 'weight': font_weight, 'fill': font_fill, 'anchor': anchor})
        elif tag == 'path':
            pid = el.attrib.get('id', '')
            d = el.attrib.get('d', '')
            stroke = el.attrib.get('stroke', '#475569')
            stroke_width = el.attrib.get('stroke-width', '2')
            stroke_dash = el.attrib.get('stroke-dasharray', '')
            marker_end = el.attrib.get('marker-end', '')
            pts = parse_path_pts(d)
            if len(pts) >= 2:
                paths.append({'id': pid, 'points': pts, 'stroke': stroke, 'width': stroke_width, 'dash': stroke_dash, 'has_arrow': bool(marker_end)})
        elif tag == 'g':
            for c in el:
                walk(c)
                
    for child in root:
        walk(child)
        
    return w, h, rects, ellipses, polygons, texts, paths

def generate_svg_drawio_diagram(diag_id, diag_name, svg_path):
    w, h, rects, ellipses, polygons, texts, paths = parse_svg_file(svg_path)
    
    diagram = ET.Element('diagram', id=diag_id, name=diag_name)
    model = ET.SubElement(diagram, 'mxGraphModel', dx="1422", dy="794", grid="1", gridSize="10", guides="1", tooltips="1", connect="1", arrows="1", fold="1", page="1", pageScale="1", pageWidth=str(int(w + 100)), pageHeight=str(int(h + 100)), math="0", shadow="0")
    root = ET.SubElement(model, 'root')
    ET.SubElement(root, 'mxCell', id="0")
    ET.SubElement(root, 'mxCell', id="1", parent="0")
    
    cell_idx = 2
    
    # Background frames
    bg_frames = []
    normal_rects = []
    for r in rects:
        if r['w'] > w * 0.8 and r['h'] > h * 0.4:
            bg_frames.append(r)
        else:
            normal_rects.append(r)
            
    for r in bg_frames:
        cid = f"frame_{cell_idx}"
        cell_idx += 1
        style = "rounded=1;whiteSpace=wrap;html=1;arcSize=6;fillColor=#f8fafc;strokeColor=#cbd5e1;strokeWidth=1.5;dashed=1;verticalAlign=top;align=left;spacingLeft=10;spacingTop=5;"
        cell = ET.SubElement(root, 'mxCell', id=cid, value="", style=style, vertex="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', x=str(r['x']), y=str(r['y']), width=str(r['w']), height=str(r['h']))
        geo.set('as', 'geometry')
        
    shapes = []
    for r in normal_rects:
        shapes.append(('rect', r))
    for el in ellipses:
        shapes.append(('ellipse', el))
    for p in polygons:
        shapes.append(('polygon', p))
        
    # Match texts with shapes
    shape_texts = {}
    used_texts = set()
    
    for s_idx, (s_type, s) in enumerate(shapes):
        matched = []
        for t_idx, t in enumerate(texts):
            if t_idx in used_texts:
                continue
            if (s['x'] - 15 <= t['x'] <= s['x'] + s['w'] + 15) and (s['y'] - 10 <= t['y'] <= s['y'] + s['h'] + 20):
                matched.append(t)
                used_texts.add(t_idx)
        if matched:
            matched.sort(key=lambda item: item['y'])
            shape_texts[s_idx] = matched
            
    # Add shapes
    for s_idx, (s_type, s) in enumerate(shapes):
        cid = f"node_{cell_idx}"
        cell_idx += 1
        
        txt_items = shape_texts.get(s_idx, [])
        if txt_items:
            lines = [t['text'] for t in txt_items]
            val = clean_html("\n".join(lines))
        else:
            val = ""
            
        fill = s.get('fill', '#ffffff')
        if fill == 'none' or not fill:
            fill = '#ffffff'
        stroke = s.get('stroke', '#334155')
        if stroke == 'none' or not stroke:
            stroke = '#64748b'
            
        dash_str = "dashed=1;dashPattern=8 8;" if s.get('dash') else ""
        
        if s_type == 'rect':
            style = f"rounded=1;whiteSpace=wrap;html=1;arcSize=8;fillColor={fill};strokeColor={stroke};strokeWidth=2;fontFamily=Arial;fontSize=13;fontColor=#111827;align=center;verticalAlign=middle;{dash_str}"
        elif s_type == 'ellipse':
            style = f"ellipse;whiteSpace=wrap;html=1;fillColor={fill};strokeColor={stroke};strokeWidth=2;fontFamily=Arial;fontSize=13;fontColor=#111827;align=center;verticalAlign=middle;"
        elif s_type == 'polygon':
            style = f"rhombus;whiteSpace=wrap;html=1;fillColor={fill};strokeColor={stroke};strokeWidth=2;fontFamily=Arial;fontSize=13;fontColor=#111827;align=center;verticalAlign=middle;"
            
        cell = ET.SubElement(root, 'mxCell', id=cid, value=val, style=style, vertex="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', x=str(s['x']), y=str(s['y']), width=str(s['w']), height=str(s['h']))
        geo.set('as', 'geometry')
        
    # Unmatched standalone texts
    for t_idx, t in enumerate(texts):
        if t_idx in used_texts:
            continue
        cid = f"txt_{cell_idx}"
        cell_idx += 1
        
        is_title = t['weight'] == '600' or t['size'] >= 24
        font_style = "1" if is_title else "0"
        font_size = "16" if is_title else "13"
        align = "center" if t['anchor'] == 'middle' else ("left" if t['anchor'] == 'start' else "right")
        
        tw = max(100, len(t['text']) * 10)
        th = 30 * (t['text'].count('\n') + 1)
        tx = t['x'] - tw / 2 if align == "center" else t['x']
        ty = t['y'] - th / 2
        
        style = f"text;html=1;strokeColor=none;fillColor=none;align={align};verticalAlign=middle;whiteSpace=wrap;rounded=0;fontFamily=Arial;fontSize={font_size};fontStyle={font_style};fontColor={t['fill']};"
        cell = ET.SubElement(root, 'mxCell', id=cid, value=clean_html(t['text']), style=style, vertex="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', x=str(round(tx, 1)), y=str(round(ty, 1)), width=str(round(tw, 1)), height=str(round(th, 1)))
        geo.set('as', 'geometry')
        
    # Paths / edges
    for p in paths:
        cid = f"edge_{cell_idx}"
        cell_idx += 1
        
        stroke = p.get('stroke', '#475569')
        dash = "dashed=1;dashPattern=8 6;" if p.get('dash') else ""
        arrow = "endArrow=block;endFill=1;" if p.get('has_arrow') else "endArrow=none;"
        style = f"edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;strokeColor={stroke};strokeWidth={p.get('width', 2)};{arrow}{dash}"
        
        pts = p['points']
        cell = ET.SubElement(root, 'mxCell', id=cid, value="", style=style, edge="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', relative="1")
        geo.set('as', 'geometry')
        
        sp = ET.SubElement(geo, 'mxPoint', x=str(pts[0][0]), y=str(pts[0][1]))
        sp.set('as', 'sourcePoint')
        
        tp = ET.SubElement(geo, 'mxPoint', x=str(pts[-1][0]), y=str(pts[-1][1]))
        tp.set('as', 'targetPoint')
        
        if len(pts) > 2:
            arr = ET.SubElement(geo, 'Array')
            arr.set('as', 'points')
            for mid in pts[1:-1]:
                ET.SubElement(arr, 'mxPoint', x=str(mid[0]), y=str(mid[1]))
                
    return diagram

def generate_ch4_drawio_diagram(diag_id, diag_name, caption, nodes, edges, notes):
    diagram = ET.Element('diagram', id=diag_id, name=diag_name)
    model = ET.SubElement(diagram, 'mxGraphModel', dx="1422", dy="794", grid="1", gridSize="10", guides="1", tooltips="1", connect="1", arrows="1", fold="1", page="1", pageScale="1", pageWidth="1700", pageHeight="1000", math="0", shadow="0")
    root = ET.SubElement(model, 'root')
    ET.SubElement(root, 'mxCell', id="0")
    ET.SubElement(root, 'mxCell', id="1", parent="0")
    
    cell_idx = 2
    
    title_cell = ET.SubElement(root, 'mxCell', id=f"title_{cell_idx}", value=f"<b>{html.escape(caption)}</b>", style="text;html=1;strokeColor=none;fillColor=none;align=center;verticalAlign=middle;whiteSpace=wrap;rounded=0;fontFamily=Arial;fontSize=18;fontColor=#111827;", vertex="1", parent="1")
    cell_idx += 1
    geo = ET.SubElement(title_cell, 'mxGeometry', x="200", y="5", width="1200", height="30")
    geo.set('as', 'geometry')
    
    fill = '#f0f5f7'
    accent = '#dcebed'
    line_col = '#607580'
    ink = '#243541'
    
    for x, y, w, h, title, body in nodes:
        cid = f"node_{cell_idx}"
        cell_idx += 1
        
        node_fill = accent if body == '' else fill
        val_html = f"<b>{html.escape(title)}</b>"
        if body:
            val_html += f"<br><font color='#475569' size='2'>{clean_html(body)}</font>"
            
        style = f"rounded=1;whiteSpace=wrap;html=1;arcSize=10;fillColor={node_fill};strokeColor={line_col};strokeWidth=2;fontFamily=Arial;fontSize=14;fontColor={ink};align=center;verticalAlign=middle;"
        cell = ET.SubElement(root, 'mxCell', id=cid, value=val_html, style=style, vertex="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width=str(w), height=str(h))
        geo.set('as', 'geometry')
        
    for pts in edges:
        cid = f"edge_{cell_idx}"
        cell_idx += 1
        style = f"edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;strokeColor={line_col};strokeWidth=2.5;endArrow=block;endFill=1;"
        cell = ET.SubElement(root, 'mxCell', id=cid, value="", style=style, edge="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', relative="1")
        geo.set('as', 'geometry')
        
        sp = ET.SubElement(geo, 'mxPoint', x=str(pts[0][0]), y=str(pts[0][1]))
        sp.set('as', 'sourcePoint')
        
        tp = ET.SubElement(geo, 'mxPoint', x=str(pts[-1][0]), y=str(pts[-1][1]))
        tp.set('as', 'targetPoint')
        
        if len(pts) > 2:
            arr = ET.SubElement(geo, 'Array')
            arr.set('as', 'points')
            for mid in pts[1:-1]:
                ET.SubElement(arr, 'mxPoint', x=str(mid[0]), y=str(mid[1]))
                
    div_cell = ET.SubElement(root, 'mxCell', id=f"div_{cell_idx}", value="", style=f"edgeStyle=none;html=1;strokeColor=#c5d0d6;strokeWidth=2;endArrow=none;", edge="1", parent="1")
    cell_idx += 1
    geo = ET.SubElement(div_cell, 'mxGeometry', relative="1")
    geo.set('as', 'geometry')
    sp = ET.SubElement(geo, 'mxPoint', x="40", y="795")
    sp.set('as', 'sourcePoint')
    tp = ET.SubElement(geo, 'mxPoint', x="1560", y="795")
    tp.set('as', 'targetPoint')
    
    for j, note_txt in enumerate(notes):
        nid = f"note_{cell_idx}"
        cell_idx += 1
        style = f"text;html=1;strokeColor=none;fillColor=none;align=left;verticalAlign=middle;whiteSpace=wrap;rounded=0;fontFamily=Arial;fontSize=12;fontColor={ink};"
        cell = ET.SubElement(root, 'mxCell', id=nid, value=f"• {html.escape(note_txt)}", style=style, vertex="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', x="45", y=str(814 + j * 36), width="1500", height="30")
        geo.set('as', 'geometry')
        
    return diagram

# -------------------------------------------------------------
# DEFINITIONS FOR ALL 22 DIAGRAMS
# -------------------------------------------------------------

ch3_specs = [
    ("fig-3-1", "Fig 3.1 - System Architecture", "figure_3_1.svg", "Fig_3_01_System_Architecture.drawio"),
    ("fig-3-2", "Fig 3.2 - Visitor Functions", "figure_3_2.svg", "Fig_3_02_Visitor_Functions.drawio"),
    ("fig-3-3", "Fig 3.3 - Enabled Scenes", "figure_3_3.svg", "Fig_3_03_Enabled_Scenes.drawio"),
    ("fig-3-4", "Fig 3.4 - Main Menu Workflow", "figure_3_4.svg", "Fig_3_04_Main_Menu_Workflow.drawio"),
    ("fig-3-5", "Fig 3.5 - Tour Navigation", "figure_3_5.svg", "Fig_3_05_Tour_Navigation.drawio"),
    ("fig-3-6", "Fig 3.6 - Tour Inspection", "figure_3_6.svg", "Fig_3_06_Tour_Inspection.drawio"),
    ("fig-3-7", "Fig 3.7 - Museum Exploration", "figure_3_7.svg", "Fig_3_07_Museum_Exploration.drawio"),
    ("fig-3-8", "Fig 3.8 - MR Alignment", "figure_3_8.svg", "Fig_3_08_MR_Alignment.drawio"),
    ("fig-3-9", "Fig 3.9 - MR Display & Recovery", "figure_3_9.svg", "Fig_3_09_MR_Display_Recovery.drawio"),
    ("fig-3-10", "Fig 3.10 - Shared AI Workflow", "figure_3_10.svg", "Fig_3_10_Shared_AI_Workflow.drawio"),
    ("fig-3-11", "Fig 3.11 - Tour Transition Sequence", "figure_3_11.svg", "Fig_3_11_Tour_Transition_Sequence.drawio"),
    ("fig-3-12", "Fig 3.12 - Museum Inspection Sequence", "figure_3_12.svg", "Fig_3_12_Museum_Inspection_Sequence.drawio"),
    ("fig-3-13", "Fig 3.13 - MR Alignment Sequence", "figure_3_13.svg", "Fig_3_13_MR_Alignment_Sequence.drawio"),
    ("fig-3-14", "Fig 3.14 - AI Guide Query Sequence", "figure_3_14.svg", "Fig_3_14_AI_Guide_Query_Sequence.drawio"),
]

# Chapter 4 definitions from add_visuals.py
ch4_raw_specs = [
    ('fig-4-13', 'Fig 4.13 - Environment & Setup', 'Development environment and project restoration sequence',
     [
      (40,40,700,160,'1  Install the editor','Unity 6000.3.16f1\nAndroid SDK, NDK and OpenJDK'),
      (860,40,700,160,'2  Restore project files','Assets + .meta files\nPackages + ProjectSettings'),
      (860,300,700,170,'3  Resolve packages','Meta XR + URP + OpenXR\nInput + UI + Localization + Addressables'),
      (40,300,700,170,'4  Configure Android','IL2CPP / ARM64\nXR loader and feature settings'),
      (40,590,700,150,'5  Register four scenes','Main Menu first, then MR, Tour, Museum'),
      (860,590,700,150,'6  Verify on headset','Build content and APK\nInstall, launch and record results')
     ],
     [[(740,120),(860,120)],[(1210,200),(1210,300)],[(860,385),(740,385)],[(390,470),(390,590)],[(740,665),(860,665)]],
     ['Follow the numbered sequence when reproducing the saved project.', 'Use the package lockfile to preserve resolved dependency versions.'],
     'Fig_4_13_Environment_Setup.drawio'),

    ('fig-4-14', 'Fig 4.14 - Shared Scene Components', 'Shared scene components and the Inspector references that connect them',
     [
      (470,35,660,135,'Four scene files','Main Menu / MR / VR Tour / VR Museum'),
      (40,280,460,185,'Meta rig and input','Head and controllers\nRay and grab input'),
      (570,280,460,185,'World-space interface','Canvas, buttons and TMP\nGuide and menu panels'),
      (1100,280,460,185,'Localized content','English and Thai tables\nFonts and localized assets'),
      (350,620,900,130,'Scene-specific references','Models, panels, transforms and data assets')
     ],
     [[(800,170),(800,220),(270,220),(270,280)],[(800,170),(800,280)],[(800,220),(1330,220),(1330,280)],[(270,465),(270,550),(800,550),(800,620)],[(800,465),(800,620)],[(1330,465),(1330,550),(800,550)]],
     ['Boxes group the shared setup responsibilities; they are not separate Unity scenes.', 'Copying a script does not reproduce its Inspector references or asset identifiers.'],
     'Fig_4_14_Shared_Scene_Components.drawio'),

    ('fig-4-15', 'Fig 4.15 - Menu Event Connections', 'Main-menu event connections and scene-loading destinations',
     [
      (40,40,690,155,'Language controls','SelectLanguage(index)\nConfirmAndContinue()'),
      (870,40,690,155,'MainMenuManager','Panel and icon references\nThree destination scene names'),
      (40,315,460,165,'MR button','LoadMRMode()\nMR_Scene'),
      (570,315,460,165,'Tour button','LoadVRMode()\nVR_Tour_Scene'),
      (1100,315,460,165,'Museum button','LoadMuseumMode()\nVR_Museum_Scene'),
      (450,625,700,125,'SceneLoadingScreen','Load the chosen registered scene')
     ],
     [[(730,120),(870,120)],[(1215,195),(1215,245),(270,245),(270,315)],[(800,245),(800,315)],[(1215,245),(1330,245),(1330,315)],[(270,480),(270,560),(800,560),(800,625)],[(800,480),(800,625)],[(1330,480),(1330,560),(800,560)]],
     ['Assign button events in the Inspector and register all destination scenes in the build.', 'The saved scene names override the older default strings in MainMenuManager.'],
     'Fig_4_15_Main_Menu_Connections.drawio'),

    ('fig-4-16', 'Fig 4.16 - VR Tour Relationships', 'VR Tour asset relationships and the two content-loading paths',
     [
      (40,35,700,150,'TourArea asset','startingNode + reconstruction'),
      (860,35,700,150,'TourNode assets','photoReference\npreviousPath / nextPath'),
      (860,295,700,180,'TourManager','Load photo through Addressables\nUpdate panorama and navigation arrows'),
      (40,295,700,180,'Reconstruction definition','Addressable inspectionPrefab\nDisplay size and viewing distance'),
      (40,610,700,145,'TourReconstructionPresenter','Load and release inspection display'),
      (860,610,700,145,'InspectionManager','Rotate, resize, read and close')
     ],
     [[(740,110),(860,110)],[(1210,185),(1210,295)],[(390,185),(390,295)],[(390,475),(390,610)],[(740,680),(860,680)]],
     ['Top/right branch: panorama navigation. Left/bottom branch: restored-model inspection.', 'Navigation is blocked while inspection is busy; closing restores the tour view.'],
     'Fig_4_16_VR_Tour_Asset_Relations.drawio'),

    ('fig-4-17', 'Fig 4.17 - Museum Station Config', 'Museum station configuration and the inspection lifecycle',
     [
      (40,35,700,160,'Artifact station','Model container + pedestal\nOrigin + UI anchor + information icon'),
      (860,35,700,160,'ArtifactData','Localized title and description\nPrimary and optional alternate model'),
      (860,320,700,165,'Ray selection','TriggerInspection()\nPass the selected artifact and anchors'),
      (40,320,700,165,'InspectionManager','Save state, pause locomotion\nShow panel and enable manipulation'),
      (40,610,700,145,'Inspection controls','Rotate / resize / optional model switch\nEnvironment dimmer fades surroundings'),
      (860,610,700,145,'Close inspection','Restore model, rig, environment\nand locomotion')
     ],
     [[(740,115),(860,115)],[(1210,195),(1210,320)],[(860,400),(740,400)],[(390,485),(390,610)],[(740,680),(860,680)]],
     ['An alternate model is optional; the model switch is available only when configured.', 'The original position, UI anchor and container must reference the correct station.'],
     'Fig_4_17_Museum_Station_Config.drawio'),

    ('fig-4-18', 'Fig 4.18 - MR Alignment Dependencies', 'MR alignment, session anchoring and reconstruction-display dependencies',
     [
      (40,35,700,160,'Authored site reference','Entrance sign/table mesh\nTracked controller and head references'),
      (860,35,700,160,'MRMeshReferenceAlignment','Place outline with grip and stick\nRelease grip, then confirm with A'),
      (860,315,700,170,'Session spatial anchor','Created + localized + tracked\nRetry alignment if setup fails'),
      (40,315,700,170,'MRMeshPlayableArea','Check alignment, tracking\nand passthrough readiness'),
      (40,615,700,140,'MRModelSpawner','Show aligned siteModelPrefab\nonly when display is allowed'),
      (860,615,700,140,'Visitor controls','A: model / B: information\nHold X: restart alignment')
     ],
     [[(740,115),(860,115)],[(1210,195),(1210,315)],[(860,400),(740,400)],[(390,485),(390,615)],[(740,680),(860,680)]],
     ['Session anchor only: alignment is repeated after the application is restarted.', 'Unrestricted footprint removes hallway checks; tracking and passthrough checks remain.'],
     'Fig_4_18_MR_Alignment_Dependencies.drawio'),

    ('fig-4-19', 'Fig 4.19 - Shared AI Guide Flow', 'Shared AI guide data flow for typed and spoken questions',
     [
      (40,35,700,145,'Typed question','Bilingual virtual keyboard'),
      (860,35,700,145,'Spoken question','Microphone permission and recording'),
      (860,295,700,160,'SpeechToTextHandler','Encode WAV and send to Groq\nReturn editable transcription'),
      (40,295,700,160,'Editable input field','Review or correct the question\nPress Send'),
      (40,610,700,145,'GeminiChatUI','Send localized guide instructions\nand question to Gemini'),
      (860,610,700,145,'Guide response','Display answer or error\nCancel / clear / configured fallback')
     ],
     [[(390,180),(390,295)],[(1210,180),(1210,295)],[(860,375),(740,375)],[(390,455),(390,610)],[(740,680),(860,680)]],
     ['Speech fills the input field; the visitor still chooses when to send the question.', 'Gemini and Groq require separate service credentials and an internet connection.'],
     'Fig_4_19_Shared_AI_Guide_Flow.drawio'),

    ('fig-4-20', 'Fig 4.20 - Build & Verification', 'Build and verification sequence for a reproducible educational project',
     [
      (40,35,700,150,'1  Check scene connections','Four build scenes, Inspector references\nLocalization and content assets'),
      (860,35,700,150,'2  Build Addressables','Android target and configured profile\nPhotographs and reconstruction prefabs'),
      (860,315,700,160,'3  Build the application','Android APK\nProject XR and player settings'),
      (40,315,700,160,'4  Install on Quest','Unity deployment or MQDH\nLaunch and check device logs'),
      (40,610,700,145,'5  Verify the experience','Language, navigation and inspection\nOn-site MR alignment and recovery'),
      (860,610,700,145,'6  Archive the reproduction','Project revision, assets and versions\nBuild details and recorded test results')
     ],
     [[(740,110),(860,110)],[(1210,185),(1210,315)],[(860,395),(740,395)],[(390,475),(390,610)],[(740,680),(860,680)]],
     ['Use Section 4.5 to record functional results for the installed build.', 'Preserve .meta files and package locks; omit service secrets from shared archives.'],
     'Fig_4_20_Build_Verification.drawio'),
]

# -------------------------------------------------------------
# BUILD CONSOLIDATED DRAW.IO MULTI-PAGE FILE
# -------------------------------------------------------------
print("\nStep 2: Generating consolidated multi-page Draw.io file...")

master_mxfile = ET.Element('mxfile', host="app.diagrams.net", agent="Antigravity", version="24.0.0")

# Chapter 3
for diag_id, diag_name, svg_name, drawio_fn in ch3_specs:
    svg_path = REPORT_DIR / "figures" / "diagrams" / svg_name
    diag_el = generate_svg_drawio_diagram(diag_id, diag_name, svg_path)
    master_mxfile.append(diag_el)
    
    # Save standalone .drawio file
    standalone_file = ET.Element('mxfile', host="app.diagrams.net", agent="Antigravity", version="24.0.0")
    standalone_file.append(diag_el)
    out_xml = ET.tostring(standalone_file, encoding='utf-8', xml_declaration=True).decode('utf-8')
    with open(DRAWIO_DIR / drawio_fn, 'w', encoding='utf-8') as f:
        f.write(out_xml)
    print(f"  Generated standalone: {drawio_fn}")

# Chapter 4
for diag_id, diag_name, caption, nodes, edges, notes, drawio_fn in ch4_raw_specs:
    diag_el = generate_ch4_drawio_diagram(diag_id, diag_name, caption, nodes, edges, notes)
    master_mxfile.append(diag_el)
    
    # Save standalone .drawio file
    standalone_file = ET.Element('mxfile', host="app.diagrams.net", agent="Antigravity", version="24.0.0")
    standalone_file.append(diag_el)
    out_xml = ET.tostring(standalone_file, encoding='utf-8', xml_declaration=True).decode('utf-8')
    with open(DRAWIO_DIR / drawio_fn, 'w', encoding='utf-8') as f:
        f.write(out_xml)
    print(f"  Generated standalone: {drawio_fn}")

# Save master consolidated multi-page Draw.io file
master_path = OUT_DIR / "LangkaLens_All_Diagrams.drawio"
master_xml = ET.tostring(master_mxfile, encoding='utf-8', xml_declaration=True).decode('utf-8')
with open(master_path, 'w', encoding='utf-8') as f:
    f.write(master_xml)
print(f"\n Master consolidated multi-page file saved: {master_path} ({len(master_xml)//1024} KB)")

# Also copy to root of Docs/Report for instant top-level access
shutil.copy2(master_path, REPORT_DIR / "LangkaLens_All_Diagrams.drawio")
print(f" Copied top-level shortcut: {REPORT_DIR / 'LangkaLens_All_Diagrams.drawio'}")

