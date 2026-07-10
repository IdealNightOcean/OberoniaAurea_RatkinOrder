using OberoniaAurea_Frame;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatWorker_AcademicPointsCostFactor(ResidentKnightStatDef statDef) : ResidentKnightStatWorker(statDef)
{
    public override bool PostTransModify(ResidentKnightStatRequestData requestData,
                                        ref StatComputeState curValue,
                                        bool resultOnly = true,
                                        StringBuilder explanation = null)
    {

        if (!TryCastRequestData<ResidentKnightStatRequestData_Academic>(
            requestData: requestData,
            targetRequestData: out ResidentKnightStatRequestData_Academic academicRequestData,
            resultOnly: resultOnly,
            explanation: explanation))
        {
            curValue.IsConverged = true;
            return false;
        }

        float costFactor = 1f;

        ResidentKnight knight = academicRequestData.Target;

        int learnedAcademicCount = knight.AcademicHandler.TotalAcademicLevel.Value;
        float learnedFactor = 1f + learnedAcademicCount * 0.01f;
        if (learnedFactor > 1f)
        {
            costFactor *= learnedFactor;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_AcademicCost_LearnedAcademic".Translate(
                        learnedAcademicCount.Named(KeyLibrary_FormatArgName.Count),
                        learnedFactor.ToStringPercent("0.##")),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        int academicCeiling = AcademicUtility.GetNoAdditionalCostAcademicCeiling(knight.CurRank);
        if (learnedAcademicCount > academicCeiling)
        {
            costFactor *= 3f;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_AcademicCost_ExceedCeiling".Translate(
                        academicCeiling.Named(KeyLibrary_FormatArgName.Count),
                        OAFrame_TextUtility.ColoredFloatNamedArgument(3f, KeyLibrary_FormatArgName.Factor, originPoint: 1f)),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        costFactor *= AcademicChivalryFactor(academicRequestData, resultOnly, explanation);

        curValue.Value *= costFactor;
        return true;
    }

    private static float AcademicChivalryFactor(ResidentKnightStatRequestData_Academic academicRequestData, bool resultOnly = true, StringBuilder explanation = null)
    {
        KnightChivalryDef academicChivalry = academicRequestData.AcademicDef.chivalry;
        float academicChivalryFactor = 1f;
        if (academicChivalry is null)
            return academicChivalryFactor;

        ResidentKnight knight = academicRequestData.Target;

        float traditionReduction = 0f;
        foreach (OrderStationTraditionDef tradition in OrderStationHandler.TraditionsManager.ActiveTraditions)
        {
            if (academicChivalry.IsSameDefNonNullable(tradition.Chivalry))
                traditionReduction += tradition.academicCostReduction;
        }

        if (traditionReduction > 0f)
        {
            float traditionFactor = 1f - traditionReduction;
            academicChivalryFactor *= traditionFactor;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_AcademicCost_StationTradition"
                    .Translate(OAFrame_TextUtility.FloatNamedArgument(traditionFactor, KeyLibrary_FormatArgName.Factor))
                    .ColorizeStrByFactor(traditionFactor, reverse: true),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        if (academicChivalry == knight.Chivalry)
        {
            float knightFactor = 0.75f;
            academicChivalryFactor *= knightFactor;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeFactor_KnightHasSameChivalryWithDef"
                    .Translate(
                        academicRequestData.AcademicDef.Named(KeyLibrary_FormatArgName.DEF),
                        academicChivalry.Named(OARO_KeyLibrary_FormatArgName.CHIVALRY),
                        OAFrame_TextUtility.FloatNamedArgument(knightFactor, KeyLibrary_FormatArgName.Factor))
                    .ColorizeStrByFactor(knightFactor, reverse: true),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        Branch branch = knight.Branch;
        if (academicChivalry == branch?.HonorDef?.chivalry)
        {
            float branchHonorFactor = 0.9f;
            academicChivalryFactor *= branchHonorFactor;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeFactor_BranchHonorHasSameChivalryWithDef"
                    .Translate(
                        academicRequestData.AcademicDef.Named(KeyLibrary_FormatArgName.DEF),
                        academicChivalry.Named(OARO_KeyLibrary_FormatArgName.CHIVALRY),
                        OAFrame_TextUtility.FloatNamedArgument(branchHonorFactor, KeyLibrary_FormatArgName.Factor))
                    .ColorizeStrByFactor(branchHonorFactor, reverse: true),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        int virtueCount = KnightVirtueUtility.GetVirtueCountOfChivalry(knight, academicChivalry);
        if (virtueCount > 0)
        {
            float virtueFactor = 1f - virtueCount * 0.1f;
            academicChivalryFactor *= virtueFactor;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeFactor_SameChivalryWithDef".Translate(
                        virtueCount.Named(KeyLibrary_FormatArgName.Count),
                        OAFrame_TextUtility.FloatNamedArgument(virtueFactor, KeyLibrary_FormatArgName.Factor))
                    .ColorizeStrByFactor(academicChivalryFactor, reverse: true),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        return academicChivalryFactor;
    }
}