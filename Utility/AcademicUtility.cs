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

    public static float GetBaseAcademicPointsCost(KnightAcademicDef academicDef,
                                                  int sourceLevel,
                                                  int targetLevel,
                                                  bool resultOnly = true,
                                                  StringBuilder explanation = null)
    {
        float baseUnitCost = academicDef.academicType == KnightAcademicDef.AcademicType.Honor ? 500f : 250f;

        int levelDiff = targetLevel - sourceLevel;

        float baseNeededPoints = levelDiff * baseUnitCost + (sourceLevel + targetLevel - 1) * levelDiff / 2 * baseUnitCost;


        if (!resultOnly)
        {
            explanation.AppendLine(ResidentKnightStatDefOf.OARO_AcademicPointsCost.GetBaseValueExplanation(baseNeededPoints));
            if (academicDef.academicType == KnightAcademicDef.AcademicType.Honor)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_AcademicPointsCost_BaseUnitCost_Honor".Translate(baseUnitCost.ToString("F0").Named(KeyLibrary_FormatArgName.Value)),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
            else
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_AcademicPointsCost_BaseUnitCost".Translate(baseUnitCost.ToString("F0").Named(KeyLibrary_FormatArgName.Value)),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }

            explanation.AppendLineWithSeparator(
                text: "OARO_AcademicPointsCost_LevelDiff".Translate(levelDiff.Named(KeyLibrary_FormatArgName.Value)),
                separator: KeyLibrary_Misc.SpaceCap4);
        }

        return baseNeededPoints;
    }


    /// <summary>
    /// 获取课业修习的花费
    /// </summary>
    public static float GetMeditationPointsNeeded(ResidentPawn residentPawn,
                                                  KnightAcademicDef academicDef,
                                                  int sourceLevel,
                                                  int targetLevel,
                                                  bool resultOnly,
                                                  out string explanation)
    {
        explanation = string.Empty;
        if (targetLevel < 1 || targetLevel <= sourceLevel)
            return 0f;

        if (targetLevel > academicDef.MaxStageLevel)
            return float.PositiveInfinity;


        if (residentPawn is ResidentKnight knight)
        {
            ResidentKnightStatRequestData_Academic requestData = new(knight: knight,
                                                                     statDef: ResidentKnightStatDefOf.OARO_AcademicPointsCost,
                                                                     academicDef: academicDef,
                                                                     sourceLevel: sourceLevel,
                                                                     targetLevel: targetLevel);

            if (resultOnly)
            {
                return requestData.GetStatValue();
            }
            else
            {
                (StringBuilder explanationBuilder, float? result) = OARO_StatUtility.GetStatModifyExplanation(requestData);
                if (result.HasValue)
                {
                    explanation = explanationBuilder.ToString();
                    return result.Value;
                }
                else
                {
                    explanation = KeyLibrary_Misc.ErrorTipWithColor;
                    return float.PositiveInfinity;
                }
            }
        }
        else
        {
            if (resultOnly)
            {

                return GetBaseAcademicPointsCost(academicDef: academicDef,
                                                 sourceLevel: sourceLevel,
                                                 targetLevel: targetLevel);
            }
            else
            {
                StringBuilder explanationBuilder = new(64);
                float result = GetBaseAcademicPointsCost(academicDef: academicDef,
                                                         sourceLevel: sourceLevel,
                                                         targetLevel: targetLevel,
                                                         resultOnly: false,
                                                         explanation: explanationBuilder);

                explanation = explanationBuilder.ToString();
                return result;
            }
        }
    }

    public static float GetMeditationPointsNeeded(ResidentPawn residentPawn,
                                                  KnightAcademicDef academicDef,
                                                  int targetLevel,
                                                  bool resultOnly,
                                                  out string explanation)
    {
        return GetMeditationPointsNeeded(residentPawn: residentPawn,
                                         academicDef: academicDef,
                                         sourceLevel: targetLevel - 1,
                                         targetLevel: targetLevel,
                                         resultOnly: resultOnly,
                                         explanation: out explanation);
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
            case KnightAcademicDef.AcademicType.Geneal: return true;

            case KnightAcademicDef.AcademicType.Honor: return branch.HonorDef?.academicDef == academic;

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