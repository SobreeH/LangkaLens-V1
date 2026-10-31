using System;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

// Paraphrased from the two user-supplied Fine Arts Department books.
// Verified printed pages and interpretation limits: Docs/TOUR_RECONSTRUCTION_SOURCES.md.
public static class TourReconstructionDescriptions
{
    private static readonly string[,] Copy =
    {
        { "reconstruction_title_2", "Ban Jalae 2", "บ้านจาเละ หมายเลข 2" },
        { "reconstruction_description_2",
            "<b>History</b>  Interpreted as an early stupa at ancient Yarang, later adapted into a worship hall through rebuilding and enlargement.\n\n<b>Structure</b>  A front hall leads to an inner room, possibly for a sacred image. Later brickwork imitated timber walls.\n\n<b>Finds</b>  Excavations recovered stupa fragments, glass beads, gold-sheet fragments and earring moulds.\n\n<b>Restored model</b>  The roof and missing upper details are interpretations of a possible complete building.\n\n<size=26>Sources: Ancient Yarang, p. 165;\nArchaeology & History of the Three Southern Border Provinces, p. 65.\n3D Model: Regional Office of Fine Arts No. 11, Songkhla</size>",
            "<b>ประวัติ</b>  สันนิษฐานว่าสมัยแรกเป็นสถูปในเมืองโบราณยะรัง ต่อมาปรับเป็นวิหาร มีการสร้างทับและต่อเติมหลายครั้ง\n\n<b>รูปแบบอาคาร</b>  มีโถงด้านหน้าและห้องด้านในที่อาจประดิษฐานรูปเคารพ แนวอิฐสมัยหลังเลียนแบบผนังอาคารไม้\n\n<b>สิ่งที่พบ</b>  ชิ้นส่วนสถูป ลูกปัดแก้ว แผ่นทอง และแม่พิมพ์ต่างหู เป็นหลักฐานที่พบจากการขุดค้น\n\n<b>แบบจำลองบูรณะ</b>  หลังคาและรายละเอียดส่วนบนที่สูญหายเป็นการตีความรูปแบบที่อาจเคยสมบูรณ์\n\n<size=26>อ้างอิง: เมืองโบราณยะรัง น. 165;\nพัฒนาการทางโบราณคดีและประวัติศาสตร์ 3 จังหวัดชายแดนใต้ น. 65\nแบบจำลอง 3 มิติ: สำนักศิลปากรที่ ๑๑ สงขลา</size>" },
        { "reconstruction_title_3", "Ban Jalae 3", "บ้านจาเละ หมายเลข 3" },
        { "reconstruction_description_3",
            "<b>History</b>  A Buddhist brick monument built in two phases. Letter forms on votive tablets suggest an early phase around the 7th century CE.\n\n<b>Structure</b>  Later building enlarged the base and covered the earlier stairs. The surviving base measures about 13.5 by 13.6 metres.\n\n<b>Finds</b>  Numerous votive tablets and miniature stupa fragments were found in the central chamber and on offering platforms.\n\n<b>Restored model</b>  The missing upper structure is an interpretation, rather than an exact replica.\n\n<size=26>Sources: Ancient Yarang, p. 180;\nArchaeology & History of the Three Southern Border Provinces, p. 66.\n3D Model: Regional Office of Fine Arts No. 11, Songkhla</size>",
            "<b>ประวัติ</b>  พุทธสถานก่ออิฐสองสมัย รูปแบบอักษรบนพระพิมพ์ทำให้สันนิษฐานว่าสมัยแรกสร้างราวพุทธศตวรรษที่ 12\n\n<b>รูปแบบอาคาร</b>  สมัยหลังขยายฐานและก่อทับบันไดเดิม ฐานที่เหลือมีขนาดประมาณ 13.5 × 13.6 เมตร\n\n<b>สิ่งที่พบ</b>  พระพิมพ์และชิ้นส่วนสถูปจำลองจำนวนมาก พบในห้องกลางและบริเวณแท่นวางเครื่องบูชา\n\n<b>แบบจำลองบูรณะ</b>  ส่วนบนที่สูญหายเป็นการตีความรูปแบบที่อาจเคยมี มิใช่สำเนาที่ตรงกับของเดิมทุกประการ\n\n<size=26>อ้างอิง: เมืองโบราณยะรัง น. 180;\nพัฒนาการทางโบราณคดีและประวัติศาสตร์ 3 จังหวัดชายแดนใต้ น. 66\nแบบจำลอง 3 มิติ: สำนักศิลปากรที่ ๑๑ สงขลา</size>" },
        { "reconstruction_title_8", "Ban Jalae 8", "บ้านจาเละ หมายเลข 8" },
        { "reconstruction_description_8",
            "<b>Role</b>  Interpreted as the principal shrine of the Ban Jalae religious group at ancient Yarang. It faces west.\n\n<b>Structure</b>  A 14-metre-square brick base supports a central chamber. Sandstone corner-column bases suggest timber columns; a hall was added in front.\n\n<b>Finds</b>  Discoveries include a bronze Avalokiteshvara head, a sandstone bull sculpture and miniature stupa fragments.\n\n<b>Restored model</b>  The missing upper form, colour and decoration remain interpretations.\n\n<size=26>Sources: Ancient Yarang, p. 195;\nArchaeology & History of the Three Southern Border Provinces, pp. 64, 68-69.\n3D Model: Regional Office of Fine Arts No. 11, Songkhla</size>",
            "<b>บทบาท</b>  สันนิษฐานว่าเป็นอาคารประธานของกลุ่มศาสนสถานบ้านจาเละในเมืองโบราณยะรัง หันหน้าทางตะวันตก\n\n<b>รูปแบบอาคาร</b>  ฐานอิฐสี่เหลี่ยมจัตุรัสกว้างด้านละ 14 เมตร มีห้องกลาง ฐานเสาหินทรายทำให้สันนิษฐานว่ามีเสาไม้ และมีโถงต่อเติมด้านหน้า\n\n<b>สิ่งที่พบ</b>  เศียรพระโพธิสัตว์อวโลกิเตศวรสำริด ประติมากรรมวัวหินทราย และชิ้นส่วนสถูปจำลอง\n\n<b>แบบจำลองบูรณะ</b>  รูปแบบส่วนบนที่สูญหาย สี และการตกแต่งเป็นการตีความ\n\n<size=26>อ้างอิง: เมืองโบราณยะรัง น. 195;\nพัฒนาการทางโบราณคดีและประวัติศาสตร์ 3 จังหวัดชายแดนใต้ น. 64, 68-69\nแบบจำลอง 3 มิติ: สำนักศิลปากรที่ ๑๑ สงขลา</size>" },
        { "reconstruction_title_9", "Ban Wat 9", "บ้านวัด หมายเลข 9" },
        { "reconstruction_description_9",
            "<b>Role</b>  Interpreted as a Buddhist monument in the Ban Wat group at ancient Yarang.\n\n<b>Structure</b>  Excavation revealed a nearly square brick base, about 12.17 by 12.25 metres, with eastern and western stairs. Brick partitions and paved rooms survive above the base.\n\n<b>Finds</b>  Discoveries include a stucco elephant, pottery fragments, clay spouts, gold and glass beads.\n\n<b>Restored model</b>  The upper building and its decoration visualize a possible form above the surviving ruins.\n\n<size=26>Sources: Ancient Yarang, p. 208;\nArchaeology & History of the Three Southern Border Provinces, p. 62.\n3D Model: Regional Office of Fine Arts No. 11, Songkhla</size>",
            "<b>บทบาท</b>  สันนิษฐานว่าเป็นพุทธสถานในกลุ่มโบราณสถานบ้านวัด เมืองโบราณยะรัง\n\n<b>รูปแบบอาคาร</b>  ฐานอิฐเกือบจัตุรัสขนาดประมาณ 12.17 × 12.25 เมตร มีบันไดด้านตะวันออกและตะวันตก ด้านบนพบแนวอิฐแบ่งห้องและพื้นห้องปูอิฐ\n\n<b>สิ่งที่พบ</b>  ช้างปูนปั้น เศษภาชนะดินเผา พวยกาดินเผา ทองคำ และลูกปัดแก้ว\n\n<b>แบบจำลองบูรณะ</b>  อาคารส่วนบนและการตกแต่งเป็นการตีความรูปแบบที่อาจเคยอยู่เหนือฐานที่เหลือ\n\n<size=26>อ้างอิง: เมืองโบราณยะรัง น. 208;\nพัฒนาการทางโบราณคดีและประวัติศาสตร์ 3 จังหวัดชายแดนใต้ น. 62\nแบบจำลอง 3 มิติ: สำนักศิลปากรที่ ๑๑ สงขลา</size>" }
    };

    [MenuItem("LangkaLens/Tour/Update Reconstruction Descriptions")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before updating descriptions.");
        var collection = LocalizationEditorSettings.GetStringTableCollection("VTour_UI");
        if (collection == null) throw new InvalidOperationException("VTour_UI localization collection is required.");
        AddToTables(collection);
        AssetDatabase.SaveAssets();
        Debug.Log("Book-based reconstruction descriptions updated in English and Thai.");
    }

    public static void AddToTables(UnityEditor.Localization.StringTableCollection collection)
    {
        foreach (var table in collection.StringTables)
        {
            int column = table.LocaleIdentifier.Code == "en" ? 1 : table.LocaleIdentifier.Code == "th" ? 2 : -1;
            if (column < 0) continue;
            Undo.RecordObject(table, "Update reconstruction descriptions");
            Undo.RecordObject(collection.SharedData, "Update reconstruction description keys");
            for (int i = 0; i < Copy.GetLength(0); ++i) table.AddEntry(Copy[i, 0], Copy[i, column]);
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(collection.SharedData);
    }
}
