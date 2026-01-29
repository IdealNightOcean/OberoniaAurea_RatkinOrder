using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class AcademicUtility
{
    public static float GetMeditationPointsNeeded(ResidentKnightAcademicDef academicDef, KnightPersonality personality, int targetLevel)
    {
        if (targetLevel < 1)
        {
            return 0f;
        }
        if (targetLevel > academicDef.MaxStageLevel)
        {
            return float.MaxValue;
        }

        float baseUnitCost = academicDef.academicType == ResidentKnightAcademicDef.AcademicType.Honor ? 500f : 250f;
        float neededPoints = baseUnitCost + (targetLevel - 1) * baseUnitCost;
        if ((academicDef.personality & personality) != 0)
        {
            neededPoints /= 2;
        }
        return neededPoints;
    }

    public static bool CanActivateAcademicBySelf(Pawn pawn, ResidentKnightAcademicDef academic)
    {
        if (pawn is null || academic is null)
            return false;

        if (!ResidentKnightsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnightRecord record))
            return false;

        Branch branch = record.Branch;
        if (branch is null)
            return false;

        switch (academic.academicType)
        {
            case ResidentKnightAcademicDef.AcademicType.Geneal:
                return true;

            case ResidentKnightAcademicDef.AcademicType.Honor:
                return branch.HonorDef?.academicDef == academic;

            case ResidentKnightAcademicDef.AcademicType.Traditional:
                foreach (BranchTradition tradition in branch.TraditionHandler.Traditions)
                {
                    if (tradition.Def.academicDef == academic)
                    {
                        return true;
                    }
                }
                return false;

            default:
                return false;
        }
    }

    public static IEnumerable<ResidentKnightAcademicDef> GetAllActivateAcademicsBySelf(ResidentKnightRecord recod)
    {
        if (recod is null)
            yield break;

        foreach (ResidentKnightAcademicDef def in DefDatabase<ResidentKnightAcademicDef>.AllDefs)
        {
            if (def.academicType == ResidentKnightAcademicDef.AcademicType.Geneal)
                yield return def;
        }

        if (recod.Branch.HonorDef?.academicDef is not null)
            yield return recod.Branch.HonorDef.academicDef;

        HashSet<ResidentKnightAcademicDef> traditionAcademics = [];
        foreach (BranchTradition tradition in recod.Branch.TraditionHandler.Traditions)
        {
            if (tradition.Def.academicDef is not null && traditionAcademics.Add(tradition.Def.academicDef))
                yield return recod.Branch.HonorDef.academicDef;
        }
    }
}
