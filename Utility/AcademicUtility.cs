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

    public static float GetMeditationPointsNeeded(KnightAcademicDef academicDef, KnightPersonality personality, int targetLevel)
    {
        if (targetLevel < 1)
        {
            return 0f;
        }
        if (targetLevel > academicDef.MaxStageLevel)
        {
            return float.MaxValue;
        }

        float baseUnitCost = academicDef.academicType == KnightAcademicDef.AcademicType.Honor ? 500f : 250f;
        float neededPoints = baseUnitCost + (targetLevel - 1) * baseUnitCost;
        if ((academicDef.personality & personality) != 0)
        {
            neededPoints /= 2;
        }
        return neededPoints;
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
                yield return record.Branch.HonorDef.academicDef;
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
                yield return recod.Branch.HonorDef.academicDef;
        }
    }

    public static float GetDailyTutoringSuccessChance(ResidentKnight teacher, Pawn student, bool resultOnly, out string explain)
    {
        explain = string.Empty;
        if (teacher is null || student is null)
            return 0f;

        StringBuilder sb = resultOnly ? null : new(128);
        float curChance = 0.1f;

        // Get student record once and cache personality
        bool hasStudentRecord = ResidentPawnsManager.Instance.TryGetKnightRecord(student, out ResidentKnight studentRecord);
        KnightPersonality studentPersonality = hasStudentRecord ? studentRecord.Personality : KnightPersonality.None;

        // Apply personality factor
        if (studentPersonality == KnightPersonality.None)
        {
            ApplyStepChange(0.7f, "");
        }
        else if (KnightPersonalityExtension.IsResonatePersonality(teacher.Personality, studentPersonality))
        {
            ApplyStepChange(1.5f, "");
        }

        // Teacher experience factor
        int teacherUnlockedCount = teacher.AcademicHandler.TotalAcademicLevel.Value;
        ApplyStepChange(1f + teacherUnlockedCount * 0.01f, "");

        // Student academic level factors
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

            // Opinion factor
            float opinionFactor = 1f + teacher.Pawn.relations.OpinionOf(student) * 0.01f;
            ApplyStepChange(Mathf.Max(0.01f, Mathf.Max(1f + opinionFactor, studentAcademicFactor)), "");
        }

        // Teacher rank factor
        float teacherRankFactor = teacher.CurRank switch
        {
            ResidentKnightRank.Elite => 1.1f,
            ResidentKnightRank.Honor => 1.25f,
            ResidentKnightRank.Crown => 1.5f,
            _ => 1f
        };
        ApplyStepChange(teacherRankFactor, "");

        // Learning rate factor
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