using System;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatWorker_AcademicPointsCostFactor(ResidentKnightStatDef statDef) : ResidentKnightStatWorker(statDef)
{
    public override void PostTransModify(ResidentKnightStatRequestData requestData, ref float curValue)
    {
        if (requestData is not ResidentKnightStatRequestData_Academic academicRequestData)
            return;

        ResidentKnight knight = academicRequestData.Knight;
        int learnedAcademicCount = knight.AcademicHandler.TotalAcademicLevel.Value;
        float learnedFactor = 1f + learnedAcademicCount * 0.01f;
        if (learnedFactor > 1f)
            curValue *= learnedFactor;


        int academicCeiling = AcademicUtility.GetNoAdditionalCostAcademicCeiling(knight.CurRank);
        if (learnedAcademicCount > academicCeiling)
        {
            throw new NotImplementedException();
            curValue = (knight.EffectTags.HasTag("") ? 1.5f : 3f);
        }

        base.PostTransModify(requestData, ref curValue);
    }
}