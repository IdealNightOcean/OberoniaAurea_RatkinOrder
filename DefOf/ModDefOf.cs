using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_ModDefOf
{
    public static FactionDef Rakinia;
    public static FactionDef OARO_Rakinia_Sub;

    public static HediffDef OARO_Hediff_InstinctTrain;

    public static JobDef OARO_Job_CommonTalkWith;

    public static LetterDef OARO_Apprentice_QuizStayIntentionLetter;

    public static ThoughtDef OARO_Thought_ChildrenCare;
    public static ThoughtDef OARO_Thought_CelebrationHost;

    public static RulePackDef OARO_NamerOrderSquad;

    public static WorldObjectDef OARO_WO_ApprenticeHome;

    static OARO_ModDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_ModDefOf));
    }
}