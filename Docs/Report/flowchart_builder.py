import xml.etree.ElementTree as ET
import html

class FlowchartBuilder:
    def __init__(self, diag_id, diag_name, width=1600, height=1400):
        self.diag_id = diag_id
        self.diag_name = diag_name
        self.width = width
        self.height = height
        
        self.diagram = ET.Element('diagram', id=diag_id, name=diag_name)
        self.model = ET.SubElement(self.diagram, 'mxGraphModel', 
                                   dx="1422", dy="794", grid="1", gridSize="10", 
                                   guides="1", tooltips="1", connect="1", arrows="1", 
                                   fold="1", page="1", pageScale="1", 
                                   pageWidth=str(width), pageHeight=str(height), 
                                   math="0", shadow="0")
        self.root = ET.SubElement(self.model, 'root')
        ET.SubElement(self.root, 'mxCell', id="0")
        ET.SubElement(self.root, 'mxCell', id="1", parent="0")
        self.cell_idx = 2

    def next_id(self, prefix="cell"):
        cid = f"{prefix}_{self.cell_idx}"
        self.cell_idx += 1
        return cid

    def add_title(self, text, x=200, y=15, w=1200, h=40):
        cid = self.next_id("title")
        val = f"<b>{html.escape(text)}</b>"
        style = "text;html=1;strokeColor=none;fillColor=none;align=center;verticalAlign=middle;whiteSpace=wrap;rounded=0;fontFamily=Arial;fontSize=20;fontColor=#0f172a;fontStyle=1;"
        cell = ET.SubElement(self.root, 'mxCell', id=cid, value=val, style=style, vertex="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width=str(w), height=str(h))
        geo.set('as', 'geometry')
        return cid

    def add_start(self, text, x, y, w=220, h=60):
        cid = self.next_id("start")
        val = f"<b>{html.escape(text)}</b>"
        style = "ellipse;whiteSpace=wrap;html=1;fillColor=#1e293b;strokeColor=#0f172a;strokeWidth=2;fontFamily=Arial;fontSize=13;fontColor=#ffffff;fontStyle=1;align=center;verticalAlign=middle;"
        cell = ET.SubElement(self.root, 'mxCell', id=cid, value=val, style=style, vertex="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width=str(w), height=str(h))
        geo.set('as', 'geometry')
        return cid

    def add_event(self, text, subtext="", x=100, y=100, w=260, h=70):
        cid = self.next_id("event")
        val = f"<b>⚡ {html.escape(text)}</b>"
        if subtext:
            val += f"<br><font color='#475569' size='2'>{html.escape(subtext)}</font>"
        style = "shape=hexagon;perimeter=hexagonPerimeter2;whiteSpace=wrap;html=1;fixedSize=1;size=15;fillColor=#f1f5f9;strokeColor=#475569;strokeWidth=2;fontFamily=Arial;fontSize=13;fontColor=#0f172a;align=center;verticalAlign=middle;"
        cell = ET.SubElement(self.root, 'mxCell', id=cid, value=val, style=style, vertex="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width=str(w), height=str(h))
        geo.set('as', 'geometry')
        return cid

    def add_decision(self, condition, subtext="", x=100, y=100, w=260, h=90):
        cid = self.next_id("dec")
        val = f"<b>if ({html.escape(condition)})?</b>"
        if subtext:
            val += f"<br><font color='#78350f' size='1'>{html.escape(subtext)}</font>"
        style = "rhombus;whiteSpace=wrap;html=1;fillColor=#fffbeb;strokeColor=#b45309;strokeWidth=2;fontFamily=Arial;fontSize=12;fontColor=#78350f;align=center;verticalAlign=middle;"
        cell = ET.SubElement(self.root, 'mxCell', id=cid, value=val, style=style, vertex="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width=str(w), height=str(h))
        geo.set('as', 'geometry')
        return cid

    def add_action(self, title, code_lines="", x=100, y=100, w=280, h=80, accent=False):
        cid = self.next_id("act")
        val = f"<b>{html.escape(title)}</b>"
        if code_lines:
            lines_html = "<br>".join([f"<code>{html.escape(l)}</code>" for l in code_lines.split("\n") if l.strip()])
            val += f"<br><font color='#334155' size='2'>{lines_html}</font>"
        fill = "#e0f2fe" if accent else "#f8fafc"
        stroke = "#0284c7" if accent else "#334155"
        style = f"rounded=1;whiteSpace=wrap;html=1;arcSize=8;fillColor={fill};strokeColor={stroke};strokeWidth=2;fontFamily=Arial;fontSize=13;fontColor=#0f172a;align=center;verticalAlign=middle;"
        cell = ET.SubElement(self.root, 'mxCell', id=cid, value=val, style=style, vertex="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width=str(w), height=str(h))
        geo.set('as', 'geometry')
        return cid

    def add_async_call(self, service, method, x=100, y=100, w=300, h=80):
        cid = self.next_id("async")
        val = f"<b>⚙️ {html.escape(service)}</b><br><font color='#1e3a8a' size='2'><code>{html.escape(method)}</code></font>"
        style = "shape=process;whiteSpace=wrap;html=1;backgroundOutline=1;size=0.05;fillColor=#eff6ff;strokeColor=#1d4ed8;strokeWidth=2;fontFamily=Arial;fontSize=13;fontColor=#1e3a8a;align=center;verticalAlign=middle;"
        cell = ET.SubElement(self.root, 'mxCell', id=cid, value=val, style=style, vertex="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width=str(w), height=str(h))
        geo.set('as', 'geometry')
        return cid

    def add_terminal(self, text, is_error=False, x=100, y=100, w=200, h=50):
        cid = self.next_id("term")
        val = f"<b>{html.escape(text)}</b>"
        fill = "#fef2f2" if is_error else "#f0fdf4"
        stroke = "#dc2626" if is_error else "#16a34a"
        color = "#991b1b" if is_error else "#166534"
        style = f"rounded=1;whiteSpace=wrap;html=1;arcSize=50;fillColor={fill};strokeColor={stroke};strokeWidth=2;fontFamily=Arial;fontSize=12;fontColor={color};align=center;verticalAlign=middle;fontStyle=1;"
        cell = ET.SubElement(self.root, 'mxCell', id=cid, value=val, style=style, vertex="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width=str(w), height=str(h))
        geo.set('as', 'geometry')
        return cid

    def add_edge(self, src_id, tgt_id, label="", label_color="#475569", waypoints=None, is_error=False, dashed=False):
        cid = self.next_id("edge")
        stroke = "#dc2626" if is_error else "#475569"
        dash_str = "dashed=1;dashPattern=8 6;" if dashed else ""
        label_style = f"fontColor={label_color};fontStyle=1;labelBackgroundColor=#ffffff;fontSize=12;fontFamily=Arial;" if label else ""
        style = f"edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;strokeColor={stroke};strokeWidth=2.2;endArrow=block;endFill=1;{dash_str}{label_style}"
        
        cell = ET.SubElement(self.root, 'mxCell', id=cid, value=label, style=style, edge="1", parent="1", source=src_id, target=tgt_id)
        geo = ET.SubElement(cell, 'mxGeometry', relative="1")
        geo.set('as', 'geometry')
        
        if waypoints:
            arr = ET.SubElement(geo, 'Array')
            arr.set('as', 'points')
            for wx, wy in waypoints:
                ET.SubElement(arr, 'mxPoint', x=str(wx), y=str(wy))
        return cid

    def add_free_edge(self, pt1, pt2, label="", waypoints=None, is_error=False, dashed=False):
        cid = self.next_id("edge")
        stroke = "#dc2626" if is_error else "#475569"
        dash_str = "dashed=1;dashPattern=8 6;" if dashed else ""
        style = f"edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;strokeColor={stroke};strokeWidth=2.2;endArrow=block;endFill=1;{dash_str}"
        cell = ET.SubElement(self.root, 'mxCell', id=cid, value=label, style=style, edge="1", parent="1")
        geo = ET.SubElement(cell, 'mxGeometry', relative="1")
        geo.set('as', 'geometry')
        
        sp = ET.SubElement(geo, 'mxPoint', x=str(pt1[0]), y=str(pt1[1]))
        sp.set('as', 'sourcePoint')
        tp = ET.SubElement(geo, 'mxPoint', x=str(pt2[0]), y=str(pt2[1]))
        tp.set('as', 'targetPoint')
        
        if waypoints:
            arr = ET.SubElement(geo, 'Array')
            arr.set('as', 'points')
            for wx, wy in waypoints:
                ET.SubElement(arr, 'mxPoint', x=str(wx), y=str(wy))
        return cid

print("FlowchartBuilder ready!")
