using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class AcademicUtility
{
    public static int GetNoAdditionalCostAcademicCeiling(ResidentKnightRank rank)
    {
        return rank switch
        {
            ResidentKnightRank.Regular => 5,
            ResidentKnightRank.Elite => 10,
            ResidentKnightRank.Honor => 20,
            ResidentKnightRank.Crown => 60,
            _ => 60
        };
    }

    public static float GetMeditationPointsNeeded(KnightAcademicDef academicDef, KnightChivalryDef chivalry, int oldLevel, int newLevel)
    {
        if (newLevel < 1 || newLevel <= oldLevel)
            return 0f;

        if (newLevel > academicDef.MaxStageLevel)
            return float.MaxValue;

        float baseUnitCost = academicDef.academicType == KnightAcademicDef.AcademicType.Honor ? 500f : 250f;
        int levelDiff = newLevel - oldLevel;
        float neededPoints = levelDiff * baseUnitCost + (oldLevel + newLevel - 1) * levelDiff / 2 * baseUnitCost;
        if (chivalry is not null && academicDef.chivalry == chivalry)
        {
            neededPoints /= 2;
        }
        return neededPoints;
    }

    public static float GetMeditationPointsNeeded(KnightAcademicDef academicDef, KnightChivalryDef chivalry, int targetLevel)
    {
        return GetMeditationPointsNeeded(academicDef, chivalry, targetLevel - 1, targetLevel);
    }

    /// <summary>
    /// 获取课业修习的实际花费倍率
    /// </summary>
    public static float GetAcademicCostFactor(ResidentKnight knight, KnightAcademicDef academicDef, bool resultOnly, out string explanation)
    {
        explanation = string.Empty;
        if (knight is null || academicDef is null)
            return 1f;

        StringBuilder sb = resultOnly ? null : new(256);
        float totalFactor = 1f;

        int learnedAcademicCount = knight.AcademicHandler.TotalAcademicLevel.Value;
        float learnedFactor = 1f + learnedAcademicCount * 0.01f;
        if (learnedFactor > 1f)
        {
            totalFactor *= learnedFactor;
            if (!resultOnly)
                sb.AppendLine("OARO_AcademicCost_LearnedAcademic".Translate(learnedAcademicCount.Named(KeyLibrary_FormatArgName.Count), learnedFactor.ToStringPercent("F0")));
        }

        if (knight.Chivalry == OARO_ModDefOf.OARO_Oath)
        {
            totalFactor *= 0.9f;
            if (!resultOnly)
                sb.AppendLine("OARO_AcademicCost_OathChivalry".Translate(0.9f.ToStringPercent("F0")));
        }

        int academicCeiling = GetNoAdditionalCostAcademicCeiling(knight.CurRank);
        if (learnedAcademicCount > academicCeiling)
        {
            totalFactor *= 3f;
            if (!resultOnly)
                sb.AppendLine("OARO_AcademicCost_ExceedCeiling".Translate(academicCeiling.Named(KeyLibrary_FormatArgName.Count), 3f.ToStringPercent("F0")));
        }

        KnightChivalryDef academicChivalry = academicDef.chivalry;
        if (academicChivalry is not null)
        {
            float traditionReduction = OrderStationHandler.TraditionsManager.GetAcademicCostReduction(academicChivalry);
            if (traditionReduction > 0f)
            {
                float traditionFactor = 1f - traditionReduction;
                totalFactor *= traditionFactor;
                if (!resultOnly)
                    sb.AppendLine("OARO_AcademicCost_StationTradition".Translate(traditionFactor.ToStringPercent("F0")));
            }

            if (knight.Chivalry == academicChivalry && knight.Chivalry != OARO_ModDefOf.OARO_Oath)
            {
                totalFactor *= 0.75f;
                if (!resultOnly)
                    sb.AppendLine("OARO_AcademicCost_SameChivalry".Translate(0.75f.ToStringPercent("F0")));
            }

            Branch branch = knight.Branch;
            if (academicChivalry.IsSameDefNonNullable(branch?.HonorDef?.chivalry) && academicChivalry.IsSameDefNonNullable(OARO_ModDefOf.OARO_Oath))
            {
                totalFactor *= 0.9f;
                if (!resultOnly)
                    sb.AppendLine("OARO_AcademicCost_HonorChivalry".Translate(0.9f.ToStringPercent("F0")));
            }

            int virtueCount = KnightVirtueUtility.GetVirtueCountOfChivalry(knight, academicChivalry);
            if (virtueCount > 0)
            {
                float virtueFactor = 1f - virtueCount * 0.1f;
                totalFactor *= virtueFactor;
                if (!resultOnly)
                    sb.AppendLine("OARO_AcademicCost_VirtueCount".Translate(virtueCount.Named(KeyLibrary_FormatArgName.Count), virtueFactor.ToStringPercent("F0")));
            }
        }

        if (!resultOnly)
        {
            sb.AppendLine();
            sb.AppendLine("OARO_AcademicCost_TotalFactor".Translate(totalFactor.ToStringPercent("F0")));
            explanation = sb.ToString();
        }

        return totalFactor;
    }

    /// <summary>
    /// 获取课业修习的实际花费
    /// </summary>
    public static float GetActualMeditationPointsNeeded(ResidentKnight knight, KnightAcademicDef academicDef, int targetLevel, bool resultOnly, out string explanation)
    {
        explanation = string.Empty;
        if (knight is null || academicDef is null)
            return 0f;

        float baseCost = GetMeditationPointsNeeded(academicDef, knight.Chivalry, targetLevel);
        float costFactor = GetAcademicCostFactor(knight, academicDef, resultOnly, out string factorExplanation);

        float actualCost = baseCost * costFactor;

        if (!resultOnly)
        {
            StringBuilder sb = new(256);
            sb.AppendLine("OARO_AcademicCost_BaseCost".Translate(baseCost.ToString("F0")));
            sb.AppendLine();
            sb.AppendLine(factorExplanation);
            sb.AppendLine();
            sb.AppendLine("OARO_AcademicCost_ActualCost".Translate(actualCost.ToString("F0")));
            explanation = sb.ToString();
        }

        return actualCost;
    }

    public static AcceptanceReport CanActivateAcademicBySelf(Pawn pawn, KnightAcademicDef academic, bool resultOnly)
    {
        if (pawn is null || academic is null)
            return false;

        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight record))
            return false;

        return CanActivateAcademicBySelf(record, academic, resultOnly);
    }

    public static AcceptanceReport CanActivateAcademicBySelf(ResidentKnight record, KnightAcademicDef academic, bool resultOnly)
    {
        Branch branch = record.Branch;
        if (branch is null)
            return false;

        switch (academic.academicType)
        {
            case KnightAcademicDef.AcademicType.Geneal:
                return true;

            case KnightAcademicDef.AcademicType.Honor:
                return branch.HonorDef?.academicDef == academic;

            case KnightAcademicDef.AcademicType.Traditional:
                if (!branch.IsBranchOfType(Branch.BranchType.Friendly))
                    return false;
                if (record.AcademicHandler.GetTotalAcademicLevelOf(a => a.academicType != KnightAcademicDef.AcademicType.Traditional) < 12)
                    return false;

                foreach (BranchTradition tradition in branch.TraditionHandler.Traditions)
                {
                    if (tradition.Def.academicDef == academic)
                        return true;
                }
                return false;

            default:
                return false;
        }
    }

    public static IEnumerable<KnightAcademicDef> GetAllActivateAcademicsBySelf(ResidentKnight record)
    {
        if (record is null)
            yield break;

        foreach (KnightAcademicDef def in DefDatabase<KnightAcademicDef>.AllDefs)
        {
            if (def.academicType == KnightAcademicDef.AcademicType.Geneal)
                yield return def;
        }

        Branch branch = record.Branch;
        if (branch.HonorDef?.academicDef is not null)
            yield return branch.HonorDef.academicDef;

        if (!branch.IsBranchOfType(Branch.BranchType.Friendly))
            yield break;

        if (record.AcademicHandler.GetTotalAcademicLevelOf(a => a.academicType != KnightAcademicDef.AcademicType.Traditional) < 12)
            yield break;

        HashSet<KnightAcademicDef> traditionAcademics = [];
        foreach (BranchTradition tradition in record.Branch.TraditionHandler.Traditions)
        {
            if (tradition.Def.academicDef is not null && traditionAcademics.Add(tradition.Def.academicDef))
                yield return tradition.Def.academicDef;
        }
    }

    public static IEnumerable<KnightAcademicDef> GetAllPotentialAcademics(ResidentKnight recod)
    {
        if (recod is null)
            yield break;

        foreach (KnightAcademicDef def in DefDatabase<KnightAcademicDef>.AllDefs)
        {
            if (def.academicType == KnightAcademicDef.AcademicType.Geneal)
                yield return def;
        }

        if (recod.Branch.HonorDef?.academicDef is not null)
            yield return recod.Branch.HonorDef.academicDef;

        HashSet<KnightAcademicDef> traditionAcademics = [];
        foreach (BranchTradition tradition in recod.Branch.TraditionHandler.Traditions)
        {
            if (tradition.Def.academicDef is not null && traditionAcademics.Add(tradition.Def.academicDef))
                yield return tradition.Def.academicDef;
        }
    }

    public static float GetDailyTutoringSuccessChance(ResidentKnight teacher, Pawn student, bool resultOnly, out string explain)
    {
        explain = string.Empty;
        if (teacher is null || student is null)
            return 0f;

        StringBuilder sb = resultOnly ? null : new(128);
        float curChance = 0.1f;

        bool hasStudentRecord = ResidentPawnsManager.Instance.TryGetKnightRecord(student, out ResidentKnight studentRecord);
        KnightChivalryDef studentChivalry = studentRecord?.Chivalry;

        if (studentChivalry is null)
        {
            ApplyStepChange(0.7f, "");
        }
        else if (studentChivalry.ResonateChivalriesSet.Contains(teacher.Chivalry))
        {
            ApplyStepChange(1.5f, "");
        }

        int teacherUnlockedCount = teacher.AcademicHandler.TotalAcademicLevel.Value;
        ApplyStepChange(1f + teacherUnlockedCount * 0.01f, "");

        if (hasStudentRecord)
        {
            int studentUnlockedCount = studentRecord.AcademicHandler.TotalAcademicLevel.Value;
            float studentAcademicFactor = studentUnlockedCount switch
            {
                < 5 => 1.5f - studentUnlockedCount * 0.12f,
                < 20 => 1f - (studentUnlockedCount - 5) * 0.02f,
                _ => 0.75f - (studentUnlockedCount - 20) * 0.01f
            };
            studentAcademicFactor = Mathf.Max(0.1f, studentAcademicFactor);
            ApplyStepChange(studentAcademicFactor, "");

            if (studentUnlockedCount >= 50)
                ApplyStepChange(Mathf.Max(0.5f, studentAcademicFactor), "");

            float opinionFactor = 1f + teacher.Pawn.relations.OpinionOf(student) * 0.01f;
            ApplyStepChange(Mathf.Max(0.01f, Mathf.Max(1f + opinionFactor, studentAcademicFactor)), "");
        }

        float teacherRankFactor = teacher.CurRank switch
        {
            ResidentKnightRank.Elite => 1.1f,
            ResidentKnightRank.Honor => 1.25f,
            ResidentKnightRank.Crown => 1.5f,
            _ => 1f
        };
        ApplyStepChange(teacherRankFactor, "");

        float learningRate = student.GetStatValue(StatDefOf.LearningRateFactor);
        curChance *= 0.9f + 0.1f * learningRate;

        return Mathf.Clamp01(curChance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ApplyStepChange(float change, string reason)
        {
            curChance *= change;
            if (!resultOnly)
                sb.AppendLine(reason.Translate(change.ToStringPercentSigned("0.##")).Colorize(change < 1f ? ColorLibrary.RedReadable : Color.green));
        }
    }

    public static IEnumerable<(KnightAcademicDef def, int targetLevel)> GetHigherAcademicsThanB(
        AcademicHandler a,
        AcademicHandler b)
    {
        if (a is null || b is null)
            yield break;

        foreach ((KnightAcademicDef def, int aLevel) in a.Academics)
        {
            int bLevel = b.GetAcademicLevel(def);
            if (bLevel == 0 || aLevel > bLevel)
            {
                yield return (def, bLevel + 1);
            }
        }
    }
}