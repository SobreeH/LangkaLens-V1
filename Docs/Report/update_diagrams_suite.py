import os
import shutil
import xml.etree.ElementTree as ET
from pathlib import Path

from flowcharts_definitions import (
    build_fig_3_4,
    build_fig_3_5,
    build_fig_3_6,
    build_fig_3_7,
    build_fig_3_8,
    build_fig_3_9,
    build_fig_3_10
)

ROOT_DIR = Path(r"e:\Unity\Projects\LangkaLens")
REPORT_DIR = ROOT_DIR / "Docs" / "Report"
OUT_DIR = REPORT_DIR / "extracted_diagrams"
DRAWIO_DIR = OUT_DIR / "drawio"
MMD_DIR = OUT_DIR / "mermaid"
MASTER_FILE = OUT_DIR / "LangkaLens_All_Diagrams.drawio"

new_flowcharts = {
    "fig-3-4": ("Fig_3_04_Main_Menu_Workflow.drawio", build_fig_3_4()),
    "fig-3-5": ("Fig_3_05_Tour_Navigation.drawio", build_fig_3_5()),
    "fig-3-6": ("Fig_3_06_Tour_Inspection.drawio", build_fig_3_6()),
    "fig-3-7": ("Fig_3_07_Museum_Exploration.drawio", build_fig_3_7()),
    "fig-3-8": ("Fig_3_08_MR_Alignment.drawio", build_fig_3_8()),
    "fig-3-9": ("Fig_3_09_MR_Display_Recovery.drawio", build_fig_3_9()),
    "fig-3-10": ("Fig_3_10_Shared_AI_Workflow.drawio", build_fig_3_10()),
}

# 1. Update standalone .drawio files
for fig_id, (fn, diag_el) in new_flowcharts.items():
    standalone = ET.Element('mxfile', host="app.diagrams.net", agent="Antigravity", version="24.0.0")
    standalone.append(diag_el)
    xml_str = ET.tostring(standalone, encoding='utf-8', xml_declaration=True).decode('utf-8')
    with open(DRAWIO_DIR / fn, 'w', encoding='utf-8') as f:
        f.write(xml_str)
    print(f"Updated standalone: {fn}")

# 2. Update Master Multi-Page File
tree = ET.parse(MASTER_FILE)
root = tree.getroot()

# Replace corresponding diagram elements by matching id
for i, child in enumerate(list(root)):
    cid = child.attrib.get('id')
    if cid in new_flowcharts:
        # replace
        _, new_el = new_flowcharts[cid]
        # find index
        idx = list(root).index(child)
        root.remove(child)
        root.insert(idx, new_el)
        print(f"Replaced tab {cid} in master Draw.io file!")

master_xml = ET.tostring(root, encoding='utf-8', xml_declaration=True).decode('utf-8')
with open(MASTER_FILE, 'w', encoding='utf-8') as f:
    f.write(master_xml)

# Copy to root of Docs/Report
shutil.copy2(MASTER_FILE, REPORT_DIR / "LangkaLens_All_Diagrams.drawio")
print(f"Master file successfully updated at: {MASTER_FILE} and {REPORT_DIR / 'LangkaLens_All_Diagrams.drawio'}")
