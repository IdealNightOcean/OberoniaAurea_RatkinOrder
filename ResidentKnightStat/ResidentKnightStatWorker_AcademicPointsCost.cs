using OberoniaAurea_Frame;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatWorker_AcademicPointsCost(ResidentKnightStatDef statDef) : ResidentKnightStatWorker(statDef)
{
    public override float PrepareInitialBaseValue(ResidentKnightStatRequestData requestData, float? baseValueOverride = null)
    {
        if (requestData is not ResidentKnightStatRequestData_Academic academicRequestData)
            return 0f;

        float baseUnitCost = academicRequestData.AcademicDef.academicType == KnightAcademicDef.AcademicType.Honor ? 500f : 250f;
        int levelDiff = academicRequestData.TargetLevel - academicRequestData.CurLevel;
        float neededPoints = levelDiff * baseUnitCost + (academicRequestData.CurLevel + academicRequestData.TargetLevel - 1) * levelDiff / 2 * baseUnitCost;

        if (academicRequestData.Knight.Chivalry.IsSameDefNonNullable(academicRequestData.AcademicDef.chivalry))
            neededPoints /= 2;

        return neededPoints;
    }
}

