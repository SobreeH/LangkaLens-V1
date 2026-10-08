# LangkaLens report revision notes

Revised: 6 October 2026.

The revised report is [LangkaLens_Senior_Project_Report.md](LangkaLens_Senior_Project_Report.md). The requested edits use the Unity application in this workspace as the project scope.

## Requested changes

- Section 4.3 now records the actual development computer, Windows version, Quest 3 and Quest 3S, the reported USB-C USB 3.0 / USB4 cables, and project frameworks and SDKs. The original C# examples were removed from this equipment section; the later requested code figures are in Section 4.2.
- Sections 1.4.1 and 4.3.2 include the developer-confirmed cable details: 2 m USB4 at a reported 10 Gb/s for quick testing, and 5 m USB-C USB 3.0 at a reported 5 Gb/s for longer testing sessions. The speeds describe the supplied specifications, not measured transfer results.
- Sections 1.4.2 and 4.3.4 also record developer-confirmed use of Meta XR Simulator, Meta Quest Developer Hub, and Meta's developer web resources (Developer Dashboard, documentation, and SDK downloads). MQDH 6.5.2 was found in this PC's Windows installed-program records. The simulator version was subsequently inspected during the screenshot session: 207.0, executable build 207.0.0.18.123. Official Meta documentation is included as references [9]–[11], and the technical-setup phase now names these tools. Simulator development checks are distinguished from physical-headset evaluation; no new test outcomes or publication status are claimed.
- Both headsets are recorded as running the latest available Meta Horizon OS as of this revision, based on the developer's confirmation. OS/build details for both Quest 3 and Quest 3S were read directly through Meta VR device tools on 5 October 2026, with the headsets connected separately. The queries identify installed firmware and do not independently verify update availability.
- The performance measurement section and its result templates have been removed at the author's request. Chapter 4 now ends with Section 4.4, System Testing and Verification.
- Section 4.4 now covers system testing and verification: functional checks, Meta XR Simulator screenshots, and technical evidence limits. Real-user testing and survey reporting were removed at the author's request.
- Section 5.2 discusses first-time VR users, limited headset ownership, cost, and the need for clear guidance.
- Section 5.3 uses simpler wording for future improvements.
- Section 3.4 contains eight flowcharts: application overview, main menu, tour navigation, tour inspection, museum, MR alignment, MR display/tracking, and shared AI input/response.
- Section 3.1 now introduces the system and design goals. The justification for omitting DFDs has been removed.
- The cover uses academic year 2026 (2569 B.E.), updated to the current year at the author's request on 6 October 2026.
- The cover and acknowledgments name Assistant Professor Dr. Zamree Che-aron (ผศ.ดร.ซัมรี เจ๊ะอารน). His biography and contact details are not needed for these sections.
- References [1] and [2] now contain the full Thai book titles, English translations, corporate author, publisher, publication years, page counts, and ISBNs supplied by the author. Numbered in-text citations were added to the background, tour, MR, and literature-review sections. Section 2.1 maps the four sites to printed source pages already recorded in the project's tour and MR source notes; the example page numbers in the author's citation-format guidance were not used as evidence.
- Section 1.6 follows the 11-month December-October plan in pages 7-8 of the supplied earlier report. The eleven phases and their overlaps are retained in a milestone table and monthly schedule, with activities adapted to the standalone Unity application. The former staff/admin phase becomes AI-guide and speech-input integration. Calendar years are not assigned because the source timeline labels months only.
- Section 4.2 contains eight editor-style code images with feature explanations, source links, original line numbers, and sequential Figure 4.1-4.8 captions. They cover menu scene loading, tour panorama loading, reconstruction inspection, museum controls, MR session alignment, MR display readiness, Gemini requests, and speech transcript input. The contents list includes the five new subsections.
- The figure presentation follows the supplied senior reports: `C:/Users/sobre/Downloads/1761673553_นายนิฮาฟีส นิสะแม 6520610008.pdf` (PDF pages 100-101) and `C:/Users/sobre/Downloads/1730088678_แอปพลิเคชันจัดการเรียนการสอน รายงาน.pdf` (PDF pages 58 and 65). The examples were used for layout only. All displayed C# comes from LangkaLens, rendered directly from the files rather than copied from those reports or captured from a running code editor.
- PNG assets are stored in `Docs/Report/figures/code/`. `source-manifest.json` records the file paths, line ranges, exact excerpts, and SHA-256 hashes for the 5 October 2026 snapshot. Long source lines wrap visually without changing their contents. Both Markdown report copies refer to the same absolute workspace image paths.

## Consistency corrections

The revision also corrects related passages, diagrams, the contents list, and the user manual. These changes distinguish session language confirmation from old persistent-preference claims, session anchors from saved cross-launch alignment, and unrestricted MR movement from the historical hallway configuration. The AI pathway uses GeminiChatUI and separate Groq transcription. Tour Y input opens reconstruction inspection. Alternate model switching applies only where museum content supports it.

Claims of eliminated motion sickness, automatically verified AI answers, fixed response times, and proven numerical performance have been removed. The earlier draft's invented archaeological English title/year has been replaced with the two source books and their publication details supplied by the author. The references use the corporate author because the supplied individual names were described as authors/editorial teams. Printer details are omitted from the citation entries. Technical and academic bibliography entries [3]–[8] remain explicitly marked for verification.

## Information still needed

- The final application build identifier and details of any technical checks described in the report.
- Exact cable models and connector details, where available.
- Any changes to the adopted monthly plan, relevant device/on-site MR captures, verified technical/academic references, and final document page numbers. Code, Unity development, and Meta XR Simulator interface figures are now included.

## Verification

The revision was checked for section order, synchronized contents, balanced Markdown code fences, eight scene flowcharts, fifteen numbered diagram captions, the requested identifying details, and removal of conflicting claims. No Unity build or headset performance test was performed. A later simulator screenshot session is documented below. Mermaid diagrams remain editable source in the Markdown report.

For the code-figure addition, the eight source excerpts were checked against the current files and original line numbers. Image layout, text wrapping, filenames, and figure numbering were reviewed, and the image/source links were checked. These figures document source behavior and do not add runtime or performance-test results.

An unchanged copy of the input report is retained at `E:/Unity/Projects/LangkaLens/tmp/report-edit/LangkaLens_Senior_Project_Report.original.md`.

## Unity development and Meta XR Simulator screenshots

- Added four actual Unity Editor development screenshots in Section 4.2.6 (Figures 4.9-4.12), covering the menu, tour, museum, and MR alignment setup.
- Added nine actual Meta XR Simulator window screenshots in Section 4.4.2 (Figures 4.13-4.21). They show language selection, the main menu, user guidance, tour selection, Ban Ja-Lae 3 panorama and reconstruction inspection, museum gallery and artifact inspection, and the shared AI interface. Testing Scope and Limitations is now Section 4.4.3; the contents list and screenshot references were synchronized.
- Originals are saved in `Docs/Screenshots/Development/` and `Docs/Screenshots/Playtesting/`. The reusable `README.md` figure index and `capture-manifest.json` record captions, scenes, dimensions, context, and file hashes. Both report copies use these shared image paths.
- Meta XR Simulator 207.0 (executable build 207.0.0.18.123), its active OpenXR runtime, Quest 3 profile, and left-eye viewport were verified. The visible 72 fps refresh-rate setting is explicitly identified as a setting rather than a measured result. Section 4.3.4 now records the inspected version.
- Application callbacks and inspection methods staged the depicted runtime screens. Museum/tour inspection viewpoints were temporarily adjusted for framing. No AI request or recording was made, and no scene edits were saved. Unity returned to the museum scene with Play Mode stopped and selection cleared; the existing simulator activation was preserved. Unity's automatic OpenXR feature-reference remapping was restored to the pre-capture Git baseline, with a copy retained under `tmp/report-edit/`.
- Simulator captures document selected runtime states opened using app callbacks and inspection methods. The functional checklist describes test actions and expected behavior. Physical passthrough and on-site MR tracking are outside the simulator captures.
- All 13 captures were visually inspected. The final figure numbering, links, PNG integrity, captions, and saved-file hashes were checked.

## Report scope update: technical verification

At the author's request, the report no longer includes real-user testing, participant counts, questionnaires, or survey-result templates. Related passages were removed or rewritten in the abstract, acknowledgments, objectives, expected benefits, timeline, design introduction, implementation chapter, conclusion, recommendations, and appendix.

Section 4.4 is now **System Testing and Verification**, with 4.4.1 Functional Checks, 4.4.2 Meta XR Simulator Playtest Screenshots, and 4.4.3 Testing Scope and Limitations. The contents list and cross-references were updated. First-time VR users remain part of the audience and interface-design discussion. All 21 Chapter 4 figures, the 13 saved screenshots, the architecture diagrams, and the Quest 3/3S equipment records are retained.

The screenshot library notes and manifest now describe technical capture limits without referring to earlier participant sessions. Both report copies were synchronized and checked for stale evaluation references, section numbering, image preservation, and balanced Markdown fences.

## Connected Quest 3S OS record

The connected device was identified as Meta Quest 3S (codename `panther`) on 5 October 2026. Read-only device queries returned display version 2.7.0, Android 14/API 34, build ID UP1A.231005.007.A1, incremental build 3814840024700610, and firmware branch releases-oculus-14.0-v207. These readings replace the missing Quest 3S values in Section 4.3.3. The evidence is saved in `Docs/Report/evidence/quest-3s-os-2026-10-05.json`. No app installation, headset-setting change, or performance test was carried out for this metadata update.

## Functional checklist table update

Removed the Recorded result column and all observation/result placeholders from Section 4.4.1 at the author's request. The 14 test cases retain their IDs, functions, actions, and expected behavior. Related instructions to complete the removed result fields were deleted. Simulator screenshots and headset equipment records are retained.

## Report scope update: no performance metrics

Removed Section 4.5, its comparison table, numerical placeholders, and measurement instructions at the author's request. Related requirements and cross-references were removed or rewritten in the abstract, objectives, timeline, theory, implementation, conclusion, recommendations, and appendix. The report now focuses on implementation, development equipment, functional checks, and screenshots. No benchmarking or profiling results are claimed. The simulator's visible 72 fps selection remains a capture setting. All diagrams, code figures, screenshots, and recorded headset OS details are retained.

## Museum inspection environment fade

Documented the museum fade added by the author on 6 October 2026. The saved scene enables `dimEnvironmentDuringInspection`, with brightness multiplier 0.18 and animation duration 0.35 seconds. The implementation excludes the selected artifact, rig, and inspection canvas; uses temporary materials and supported skybox exposure; and restores originals after the return fade or immediately on disable/destruction. Stationary/tour inspection skips this helper.

Updated the museum scope, flowchart, component relationships, sequence diagram, implementation explanation, functional checklist, and user manual. Figure 4.4 was refreshed from the unchanged controls excerpt at its current lines 449-467; the other seven code snapshots remain dated 5 October. The museum simulator captures remain historical 5 October views, and their earlier presentation is identified in the report and screenshot metadata. This documentation update did not run Play Mode or modify Unity assets.

The original external report was no longer present at its earlier Antigravity path when this update was made. The revised workspace report is the saved copy for this update.

## Additional acknowledgment

Added Assistant Professor Dr. Wannisa Matcha (ผศ.ดร.วรรนิศา มัจฉา) to the acknowledgments, thanking her for her input and suggestions for LangkaLens. The project advisor remains Assistant Professor Dr. Zamree Che-aron. The supplied education, expertise, and contact details are not included in the acknowledgment paragraph.

## University cover page

Replaced the opening title block with a centered cover following the first page of the supplied senior-project PDF. The Prince of Songkla University seal is extracted directly from that page and stored in `figures/cover/psu-seal.png`. The cover retains LangkaLens's existing English title, author, student ID, advisor, degree program, Faculty of Communication Sciences, and Pattani Campus. Its academic year is 2026 (2569 B.E.), updated to the current year at the author's request. HTML alignment, spacing, and a print page break keep the cover separate from the abstract in renderers that support inline HTML and CSS. The sample's author, project title, and academic year are not used.

The rendered cover was visually checked at A4 width. The seal loads correctly and all cover content fits within the cover block. The report body from the abstract onward was compared with the pre-edit copy and is unchanged.
