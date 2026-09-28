# Ban Jalae 3 information panel

`MR_Scene` contains `BanJalaeInformation/BanJalaeInfoCanvas`. The panel appears only while `MRModelSpawner.IsModelVisible` is true. It follows the viewer's position at a default offset of 1.65 m right, 0.12 m below eye level and 1.35 m forward (about 2.14 m away). Its heading stays fixed while the user turns to read; a turn more than 120 degrees away repositions it. Tracking/alignment pauses hide it along with the model. Page selection survives these pauses.

The three pages cover the site, surviving evidence, and reconstruction uncertainty. Previous/Next wrap through the pages using the existing Meta ray interaction system. All copy uses `SiteInfo_` keys in the existing `MR_UI` English and Thai tables. Sarabun-Medium SDF supplies both scripts. Edit the table entries to revise the wording; no runtime network access is needed.

## Evidence and wording

User-supplied sources, visually checked against the scanned pages:

- *เมืองโบราณยะรัง*, สำนักศิลปากรที่ 11 สงขลา: printed/PDF p. 180 (architecture, construction phases, interpretation); p. 192 (plan); pp. 193-194 (surviving monument photographs).
- *พัฒนาการทางโบราณคดีและประวัติศาสตร์ 3 จังหวัดชายแดนใต้*: printed p. 66 / PDF p. 76 (Ban Jalae 3 account), printed p. 67 / PDF p. 77 (finds and conserved monument).

The publications describe two construction phases, a stepped/projecting base, decorative window-like recesses, an approximately 13.5 x 13.6 m base, and a 5.2 x 5.2 m central chamber. They date the first phase approximately to the 12th Buddhist century / 7th century CE. They describe the possible relationship to Candi Mendut in Java as a hypothesis. The upper structure does not survive complete.

The `OG_BJ3` preview shows a reconstructed upper structure. Neither supplied publication certifies this particular digital asset. The panel therefore identifies features to compare with the remains and explicitly treats the upper form as interpretation. It does not claim a measured accuracy percentage, exact historical colours, or certified original height. The displayed model is fitted to the opening by `MRModelSpawner`, so the source measurements describe the archaeological remains, not a validated measurement of the displayed model.

No scans or figures from the ebooks are bundled in the application. Source notes on the panel use abbreviated titles and printed page numbers. Full source PDFs remain in the user's Books folder.

## Authoring and validation

`MRSiteInformationTools` creates the layout and populates the existing localization tables. Its setup menu rejects duplicate installation and leaves scene saving to the caller. Meta ray-canvas setup is a separate editor operation; the saved scene includes it. Prefer editing the existing panel and tables rather than rerunning setup.

On-device checks still required: text comfort, Thai typography, controller/hand ray selection, placement around physical obstacles, and repeated real-world alignment/tracking loss. This is an information display, not a boundary or obstacle-avoidance system.

Validated in the Editor: all localized entries have text and font glyphs; six English/Thai page renders fit without clipping. Play Mode checks passed for hidden-before-alignment, visible-with-model, next/previous page wrapping, live Thai switching, stable placement during a 50-degree turn toward the panel, and hiding when playback pauses. The development bypass and synthetic viewer used for the test were runtime-only; the saved scene retains Development Mode off. Existing Meta bridge port-conflict errors were observed separately from panel behavior.
