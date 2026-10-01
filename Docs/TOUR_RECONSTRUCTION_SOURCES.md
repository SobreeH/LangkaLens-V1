# Sources for tour site and reconstruction descriptions

Updated 2026-10-02. Short descriptions are paraphrased in English and Thai, rather than copied from the books. They are stored in `VTour_UI` under `reconstruction_title_2/3/8/9` and `reconstruction_description_2/3/8/9`. The reusable setup copy is in `TourReconstructionDescriptions.cs`.

## Books supplied by the user

1. **เมืองโบราณยะรัง**, สำนักศิลปากรที่ 11 สงขลา. Source file: `C:\Users\sobre\OneDrive\Documents\Final Project Files\Books\หนังสือเมืองโบราณยะรัง โดยสำนักศิลปากรที่ 11 สงขลา (Searchable)(Compressed)-1.pdf`.
2. **พัฒนาการทางโบราณคดีและประวัติศาสตร์ 3 จังหวัดชายแดนใต้**. Source file: `C:\Users\sobre\OneDrive\Documents\Final Project Files\Books\หนังสือพัฒนาการทางโบราณคดีและประวัติศาสตร์ 3 จังหวัดชายแดนใต้ (Searchable)-1.pdf`.

The PDFs were read locally. Relevant pages were rendered and visually checked because searchable text contains OCR errors, particularly in numbers and Thai spelling. Page references below use the printed page numbers. Book 1's referenced printed pages match its PDF positions; Book 2's referenced PDF positions are ten pages later than the printed numbers.

## Claim-to-source mapping

| Model / title | Verified source pages | Basis of the canvas description |
| --- | --- | --- |
| `OG_BJ2` / Ban Jalae 2 / บ้านจาเละ หมายเลข 2 | Book 1 p. 165 (PDF 165); Book 2 p. 65 (PDF 75) | Book 1 interprets the earlier monument as a stupa and later phases as a worship hall with a front hall and inner room possibly housing a sacred image. Book 2 records superimposed building phases and later repairs. The English and Thai copy retain the interpretive status of the stupa identification. |
| `OG_BJ3` / Ban Jalae 3 / บ้านจาเละ หมายเลข 3 | Book 1 p. 180 (PDF 180); Book 2 p. 66 (PDF 76) | Two construction phases; an estimated early phase around Buddhist century 12 / approximately the 7th century CE; stepped brick bases; votive tablets and miniature stupas associated with worship. The dating is approximate. |
| `OG_BJ8` / Ban Jalae 8 / บ้านจาเละ หมายเลข 8 | Book 1 p. 195 (PDF 195); Book 2 pp. 64, 68 (PDF 74, 78) | A west-facing monument with a square base, central chamber and sandstone column bases. The authors interpret it as the principal monument in the religious group and propose wooden corner columns. The column reconstruction is a hypothesis, not surviving timber. |
| `OG_BW9` / Ban Wat 9 / บ้านวัด หมายเลข 9 | Book 1 p. 208 (PDF 208); Book 2 p. 62 (PDF 72) | A brick monument interpreted as Buddhist, with eastern/western stairs and rooms above the base. Stucco elephant sculpture and beads occur among the finds. This is Ban Wat 9, distinct from the unexcavated Ban Jalae 9. |

`MR_Scene`'s existing BJ3/A3 information was checked through `MRSiteInformationTools.cs` and the actual `MR_UI` English/Thai entries (`SiteInfo_About`, `SiteInfo_Interpretation`, and source entries). Its account of two phases, votive tablets, miniature stupas and the incomplete upper structure agrees with the referenced book pages. Its text and scene were not changed.

## Reading the restored models

The books document ruins and archaeological interpretations. They do not establish these four supplied digital prefabs as exact reconstructions. Each canvas therefore has a distinct **Restored model / แบบจำลองบูรณะ** paragraph explaining that the complete upper structure is interpretive. Roof shapes, colours and fine decoration are not presented as verified original features. The models remain scaled inspection displays, rather than measured replicas.

The BJ2 interpretation uses the more detailed later account in Book 1, which distinguishes additional phases after recent excavation. A precise date is omitted for Ban Wat 9: Book 2 offers a broad 7th–12th-century CE range, while Book 1 reports a narrower thermoluminescence-based Buddhist-century 14–15 estimate. The short panel does not silently collapse those differing accounts into a single exact date.

## Content and layout verification

The titles now identify the actual sites. Each description has one short archaeological paragraph and one model-interpretation paragraph. The right-side panel retains its readable 30-point body text; its description area is enlarged to 710 × 360 canvas units within the existing 800 × 600 panel. The title and Close button move slightly upward to preserve spacing. Model interaction, zoom, close controls, panorama fading and the globe marker are unchanged.

The setup menu uses the same curated copy, so rebuilding the generated content will not restore the old generic descriptions. The description update menu changes only the localization tables and does not rebuild scenes or model prefabs. Source references are recorded here rather than adding dense bibliography text to the in-headset panel.

Verification passed in Unity: all 16 English/Thai title/body layouts and visible-glyph checks, 97 reconstruction asset checks, and the 119-check tour Play Mode suite. The live Thai BJ3 and English BJ8 panels were visually inspected against the dimmed panorama; these are the longest body layouts. The Editor was returned to the saved tour scene outside Play Mode. Headset comfort and packaged-build validation remain outstanding.
