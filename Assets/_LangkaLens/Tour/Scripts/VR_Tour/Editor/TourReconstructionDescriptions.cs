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
            "Thought to begin as a brick stupa, this monument at ancient Yarang was later adapted as a worship hall, with a front hall and a room for a sacred image.\n\n<b>Restored model:</b> explore a possible complete building. Its roof and upper details interpret parts that no longer survive.",
            "พุทธสถานในเมืองโบราณยะรัง สมัยแรกสันนิษฐานว่าเป็นสถูป ต่อมาปรับเป็นวิหาร มีโถงด้านหน้าและห้องด้านในที่อาจใช้ประดิษฐานรูปเคารพ\n\n<b>แบบจำลองบูรณะ:</b> แสดงรูปแบบอาคารที่อาจเคยสมบูรณ์ หลังคาและรายละเอียดส่วนบนเป็นการตีความส่วนที่ไม่เหลืออยู่" },
        { "reconstruction_title_3", "Ban Jalae 3", "บ้านจาเละ หมายเลข 3" },
        { "reconstruction_description_3",
            "A Buddhist brick monument at ancient Yarang, built in two phases. The first is dated to around the 7th century CE. Votive tablets and miniature stupas found inside show its religious role.\n\n<b>Restored model:</b> explore a possible form above the stepped base. The lost upper structure is interpreted, rather than an exact replica.",
            "พุทธสถานก่ออิฐในเมืองโบราณยะรัง พบการก่อสร้างสองสมัย สมัยแรกราวพุทธศตวรรษที่ 12 พระพิมพ์และสถูปจำลองที่พบภายในเป็นหลักฐานของการบูชา\n\n<b>แบบจำลองบูรณะ:</b> แสดงรูปแบบที่อาจเคยอยู่เหนือฐานซ้อนชั้น ส่วนบนที่สูญหายเป็นการตีความ มิใช่สำเนาที่ตรงกับของเดิมทุกประการ" },
        { "reconstruction_title_8", "Ban Jalae 8", "บ้านจาเละ หมายเลข 8" },
        { "reconstruction_description_8",
            "A west-facing brick monument at ancient Yarang, interpreted as the complex's main shrine. Its square base has a central chamber and sandstone corner-column bases.\n\n<b>Restored model:</b> imagine the missing upper building. The books propose timber corner columns; its upper form, colour and details remain interpretations.",
            "สันนิษฐานว่าเป็นอาคารประธานของกลุ่มพุทธสถานในเมืองโบราณยะรัง หันหน้าทางตะวันตก มีฐานสี่เหลี่ยม ห้องกลาง และฐานเสาหินทรายบริเวณมุมอาคาร\n\n<b>แบบจำลองบูรณะ:</b> ช่วยจินตนาการส่วนบนที่สูญหาย หนังสือสันนิษฐานว่ามีเสาไม้ประดับมุม รูปแบบส่วนบน สี และรายละเอียดเป็นการตีความ" },
        { "reconstruction_title_9", "Ban Wat 9", "บ้านวัด หมายเลข 9" },
        { "reconstruction_description_9",
            "Interpreted as a Buddhist monument at ancient Yarang. Excavation revealed a brick base with eastern and western stairs and rooms above it. Finds include a stucco elephant and beads.\n\n<b>Restored model:</b> visualize a possible complete structure above the surviving base. Its upper form and decoration are reconstructed interpretations.",
            "สันนิษฐานว่าเป็นพุทธสถานในเมืองโบราณยะรัง พบฐานก่ออิฐ มีบันไดด้านตะวันออกและตะวันตก และการแบ่งห้องด้านบน โบราณวัตถุที่พบมีรูปช้างปูนปั้นและลูกปัด\n\n<b>แบบจำลองบูรณะ:</b> แสดงอาคารที่อาจเคยสมบูรณ์เหนือฐานที่เหลืออยู่ รูปแบบส่วนบนและการตกแต่งเป็นการตีความ" }
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
