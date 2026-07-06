using OberoniaAurea_Frame;
using System.Text;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatWorker_AcademicPointsCostFactor(ResidentKnightStatDef statDef) : ResidentKnightStatWorker(statDef)
{
    public override void PostTransModify(ResidentKnightStatRequestData requestData,
                                         ref float curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        if (requestData is not ResidentKnightStatRequestData_Academic academicRequestData)
        {
            if (!resultOnly)
                explanation.AppendLine(KeyLibrary_Misc.ErrorTipWithColor);

            return;
        }

        ResidentKnight knight = academicRequestData.Knight;
        int learnedAcademicCount = knight.AcademicHandler.TotalAcademicLevel.Value;
        float learnedFactor = 1f + learnedAcademicCount * 0.01f;
        if (learnedFactor > 1f)
        {
            curValue *= learnedFactor;
            if (!resultOnly)
                explanation.AppendLine(KeyLibrary_Misc.ErrorTipWithColor);
        }



        int academicCeiling = AcademicUtility.GetNoAdditionalCostAcademicCeiling(knight.CurRank);
        if (learnedAcademicCount > academicCeiling)
        {
            float factorMulti = (knight.EffectTags.HasTag(KeyLibrary_EffectTag.VirtueOath) ? 1.5f : 3f);
            curValue *= factorMulti;
            if (!resultOnly)
                explanation.AppendLine(KeyLibrary_Misc.ErrorTipWithColor);
        }

        base.PostTransModify(requestData, ref curValue);
    }

}