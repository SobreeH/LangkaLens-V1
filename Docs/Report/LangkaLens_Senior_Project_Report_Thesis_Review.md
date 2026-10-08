# LangkaLens senior project report: thesis review

Reviewed on 6 October 2026. The original report has not been edited.

Source: [LangkaLens_Senior_Project_Report.md](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md). Line references below refer to this version of the report.

The most important revisions concern the strength of the evidence, the literature review, and unfinished references. The sampled implementation descriptions generally agree with the local Unity project. This is a review of the written report and selected source/configuration files, not a new runtime test or certification of compliance with a university template.

The revision notes say that participant surveys and performance benchmarks were deliberately removed. This review respects that scope: neither needs to be added simply to resolve the issues below. Claims should match the evidence actually included.

## Highest-priority corrections

### 1. Remove the unsupported claim that benefits were evaluated through actual use

**Location:** [Section 1.1, line 131](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:131).

The final sentence says that the project's benefits “are evaluated through actual use.” However, Sections 4.4.2–4.4.3 document selected simulator states and describe the limitations of those captures. They do not present evidence of visitor learning, usability, or other achieved benefits. Expected benefits in Section 1.5 are appropriately described as intentions; keep that distinction consistent.

**Suggested replacement for the final sentence:** “These are intended benefits; this report documents implementation and selected simulator states rather than measured visitor outcomes.”

### 2. Distinguish a functional test checklist from completed verification

**Locations:** [objective 6, line 142](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:142), [Section 4.1, line 841](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:841), and [Section 4.4.1, line 1079](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:1079).

The fourteen test cases state actions and expected behavior, but contain no execution outcomes. Lines 1104–1106 explicitly explain that application callbacks opened the captured states, that a complete controller-input sequence was not performed, and that no AI question or speech recording was submitted. Physical MR is also outside those captures. The museum screenshots precede the environment fade addition.

This is a gap in demonstrating achievement of the verification objective, not proof that the application fails. Screenshots establish that particular views were displayed; they do not establish successful end-to-end input, service, tracking, or recovery behavior.

**Correction:** Describe the table as a functional test specification/checklist unless actual execution evidence is available. In the limitations and conclusion, distinguish implemented functions, simulator states observed, and functions whose execution is not established by this report. If genuine test records already exist, they can be summarized or linked separately without reinstating the removed results column. Do not infer pass results from code or screenshots.

**Suggested replacement for Section 4.4.3:** “This report includes implementation evidence, a functional test checklist, and selected simulator interface captures. The checklist specifies intended checks and expected behavior; completed execution results are not reported. The captures do not establish complete controller-input operation, successful AI chat or speech transcription, physical-site MR behavior, or the subsequently added museum environment fade.”

### 3. Develop Chapter 2 into a literature review

**Location:** [Sections 2.2–2.6, starting at line 257](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:257).

These sections mainly describe LangkaLens components and behavior. They contain no supporting citations, discussion of prior systems, comparison of research findings, or clear explanation of the gap addressed by this project. That does not fulfill the purpose implied by “Related Theories and Literature Review.”

**Correction:** For each relevant topic, explain a concept or finding from a source actually consulted, describe its relevance and limits, and connect it to a design decision. Add a concise comparison of relevant heritage VR/MR applications or studies and explain LangkaLens's contribution. Keep detailed script behavior primarily in Chapters 3 and 4. A comparison can cover the source, approach, principal findings or limitations, and implications for this project; do not invent results or references.

### 4. Finish and consistently format the references

**Location:** [References, lines 1191–1203](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:1191).

The introduction explicitly says that entries [3]–[8] remain unverified. None of these six entries is cited in the report body. Entries [3]–[5] also combine broad documentation topics under generic titles and dates. Entry [5] points to a documentation directory rather than one identifiable URP/Addressables manual; the [Unity documentation page](https://docs.unity.com/en-us) links to separate manuals and package documentation.

**Correction:** Verify the exact sources consulted, cite them where they support the discussion, and remove unused entries. Cite specific documentation pages and relevant versions rather than combining unrelated manuals. Check publication/update dates instead of treating the access year as a publication year. Use one department-approved reference style consistently; the current entries mix conventions for dates, publishers, and access information. Remove the verification reminder after completing this work.

The existence and core metadata of references [6] and [7] are supported by [Azuma's publication list](https://www.ronaldazuma.com/publications.html) and the [Frontiers article page](https://www.frontiersin.org/journals/robotics-and-ai/articles/10.3389/frobt.2016.00074/full). Their missing role in the discussion is the concern, not evidence that they are invented. The archaeological books and their cited pages were not independently checked against the original publications in this review.

## Other substantive improvements

### 5. Explain where the visual heritage content came from and what the author contributed

**Locations:** [acknowledgments, line 35](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:35), [archaeological source discussion, line 253](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:253), and [development process, line 839](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:839).

The report thanks contributors for photogrammetric models and distinguishes archaeological evidence from reconstruction, but it does not explain the origin and preparation of the panoramas, artifact models, and interpreted buildings sufficiently to establish the project's contribution. The site-description references do not establish who produced each visual asset or how its reconstructed features were chosen.

**Correction:** Add a short content-preparation subsection identifying asset creators/providers, capture or acquisition methods where known, the author's modifications, and the evidence used for interpreted features. Record any historical-content review that actually occurred. Clearly distinguish supplied assets from original modeling and application integration. Do not claim photogrammetry, expert validation, or accurate reconstruction for assets without supporting records.

### 6. Reframe unsupported statements about visitors as design assumptions

**Locations:** [line 129](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:129) and [line 1166](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:1166).

Statements about how many visitors lack headsets, their purchasing interest, and controls being a larger obstacle than heritage learning have no cited basis in this report. These may be reasonable design considerations, but they are not established findings about the target population.

**Correction:** Use wording such as “The application is designed for visitors who may have little prior VR experience and may need help learning the controls,” or support the broader statements with relevant consulted research. A new visitor survey is not necessary to make this wording accurate.

### 7. Separate two different actions in Figure 3.5

**Location:** [Figure 3.5 source, lines 437–451](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:437).

The same “Show area map and starting view” node represents opening the map and returning to the tour starting menu. The implementation treats these differently. [ToggleMapMenu](E:/Unity/Projects/LangkaLens/Assets/_LangkaLens/Tour/Scripts/VR_Tour/TourManager.cs:365) changes interface visibility and closes inspection; [MainMenuRoutine](E:/Unity/Projects/LangkaLens/Assets/_LangkaLens/Tour/Scripts/VR_Tour/TourManager.cs:97) separately clears the current viewpoint and loads the title panorama.

**Correction:** Use separate nodes for “Open area map over current panorama” and “Return to tour starting view.” Show map closing/resumption without implying that opening the map resets the viewpoint.

### 8. Define the interpolation equation or replace it with prose

**Location:** [Section 2.2, line 263](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:263).

The equation introduces time, an easing function, and two scale vectors without defining them. Its relationship to the actual transition is therefore unclear.

**Correction:** Define the variables, normalized time interval, vector meaning, and interpolation operation. The [forward transition code](E:/Unity/Projects/LangkaLens/Assets/_LangkaLens/Tour/Scripts/VR_Tour/TourManager.cs:259) squares normalized elapsed time before interpolation, so a description of that phase can identify a quadratic ease-in. Do not imply that one formula describes every phase of the transition. If the equation adds little to the argument, replace it with a plain explanation. Number it if required by the thesis format.

### 9. Make the appendix instructions actionable

**Locations:** [tour instructions, line 1221](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:1221) and [museum instructions, lines 1229–1231](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:1229).

The manual tells the reader to rotate, resize, switch, and close without consistently specifying how. [InspectionManager](E:/Unity/Projects/LangkaLens/Assets/_LangkaLens/Mixed/Scripts/VR_Museum/InspectionManager.cs:431) supplies the missing mappings.

**Correction:** State that holding A reduces model size and holding B increases it, and that X or Close ends inspection. Explain Y for eligible museum model switching separately from Y for tour inspection. Add the tour's right-stick rotation and stick-click reset where applicable. Keep the mappings specific to each mode because MR uses A and B for different actions.

## Details requiring confirmation or final preparation

| Item | Location | Recommended action |
|---|---|---|
| Calendar years for the schedule | [204](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:204), [220](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:220) | Confirm the actual December–October plan and its years. Remove references to an unspecified earlier report. The schedule is already labeled planned; do not recast it as completed work without evidence. |
| Cable specification | [176](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:176), [1022](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:1022) | Clarify whether “USB4 … 10 Gb/s” identifies a vendor claim, cable capability, or connection rate. Verify the model instead of automatically changing the speed. USB4 supports backward-compatible connections at mutually supported capabilities, so a lower connection rate alone does not prove the cable description false. See the [USB-IF overview](https://www.usb.org/usb4). |
| Build identity | [1067](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:1067), [1257](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:1257) | Identify the build/source snapshot associated with the evidence where available. Current project settings do not prove the identity of an earlier tested build. |
| Remaining editorial instructions | [117](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:117), [1024](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:1024), [1067](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:1067), [1191](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:1191), [1258](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:1258) | Complete the fields or state the evidence limit. Move instructions such as “add” and “record” into revision notes. |
| Portable figures and source links | [900](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:900) and subsequent image links | The absolute E: drive paths work in this workspace but cannot be relied on elsewhere. Embed the images in the final document or package them with portable links. Render the Mermaid diagrams and inspect the exported document. |
| University formatting | [contents, line 39](E:/Unity/Projects/LangkaLens/Docs/Report/LangkaLens_Senior_Project_Report.md:39) and tables throughout | Final page numbers are missing; eleven tables lack numbers/captions, and there are no lists of figures or tables. Check the department's actual template before treating any specific front-matter item as compulsory. The academic year should also follow the approved submission record, not be inferred solely from today's date. |

## Small language and consistency corrections

- **Title, line 5:** Change “with Intelligent AI Heritage Guide” to “with an AI Heritage Guide.” This supplies the missing article and removes redundancy. Retain an officially approved title if one has already been registered.
- **Screenshot description, line 831:** Replace “current” with “captured.” Line 1108 correctly explains that the museum images show the presentation before the fade addition.
- **Site names:** Standardize “Ban Jalae” and “Ban Ja-Lae” in report prose and captions. Explain the interface's displayed spelling if it needs to remain different.
- **Revision-history language:** Phrases about the old language preference at line 431 and earlier hallway layout at line 1241 distract from the final system. State the final behavior directly unless a design-history discussion is intentional. Preserve relevant limitations, including session-only anchors and unrestricted movement.
- **Acknowledgments, lines 27–29:** Phrases such as “sincere gratitude and profound appreciation” can be shortened for a more natural academic tone. This is a style improvement, not a factual error.

## Checks that passed and review limits

- Section and subsection titles match the contents list.
- Figure numbering is sequential: Figures 3.1–3.15 and 4.1–4.21.
- All 36 local Markdown link targets checked, plus the cover-seal image, exist. Portability is a separate issue from missing files.
- The source contains fifteen Mermaid diagrams. Their final PDF/Word layout has not been verified in this review.
- Sampled source and configuration support the descriptions of four enabled scenes, session language confirmation, session anchors, the MR wait period and unrestricted footprint, inspection controls and cleanup, museum fade settings, and separate chat/transcription pathways.
- The report appropriately distinguishes simulator refresh-rate selection from measured performance and warns against treating complete reconstructions or AI answers as verified archaeological facts.

No application build, headset session, external AI request, archaeological book-page audit, or full university-template compliance review was performed. The original report and Unity assets remain unchanged by this review.
