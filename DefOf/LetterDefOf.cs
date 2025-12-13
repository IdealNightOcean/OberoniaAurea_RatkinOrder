using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_LetterDefOf
{
    public static LetterDef OARO_Order_PositiveLetter;
    public static LetterDef OARO_Order_NeutralLetter;
    public static LetterDef OARO_Order_NegativeLetter;
    public static LetterDef OARO_Order_ThreatBigLetter;

    /// <summary>
    /// 坠马骑士 - 开始信件
    /// </summary>
    public static LetterDef OARO_Order_InDistressKnightStartLetter;
    /// <summary>
    /// 坠马骑士 - 结束信件
    /// </summary>
    public static LetterDef OARO_Order_InDistressKnightLeaveLetter;

    public static LetterDef OARO_AutoUpgradeRelationshipQuizLetter;

    public static LetterDef OARO_Apprentice_QuizStayIntentionLetter;
    public static LetterDef OARO_KnightGroupProactiveVisitLetter;

    static OARO_LetterDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_LetterDefOf));
    }
}
