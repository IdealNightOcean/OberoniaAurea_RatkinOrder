using NightOcean;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AcademicHandler : IExposable
{
    private Dictionary<KnightAcademicDef, int> academics = [];
    public IReadOnlyDictionary<KnightAcademicDef, int> Academics => academics;

    public LazyMutable<int> TotalAcademicLevel { get; }

    private float meditationPoints;
    public float MeditationPoints
    {
        get => meditationPoints; set => meditationPoints = Mathf.Max(0f, value);
    }

    internal AcademicHandler()
    {
        TotalAcademicLevel = new(refreshFunc: () => academics?.Values.Sum() ?? 0);
    }

    public AcademicHandler(ResidentKnight knight) : this()
    {
        try
        {
            /*
            KnightAcademicDef initAcademicDef;
            if (Personality != KnightPersonality.None && OrderDefDataBase.ResidentKnightAcademicGroupByPersonality.TryGetValue(Personality, out List<KnightAcademicDef> potentialAcademics))
            {
                initAcademicDef = potentialAcademics.RandomElement();
            }
            else
            {
                initAcademicDef = DefDatabase<KnightAcademicDef>.AllDefsListForReading
                    .Where(d => d.academicType == KnightAcademicDef.AcademicType.Geneal)
                    .RandomElement();
            }
            UpgradeAcademicLevel(initAcademicDef, usePoints: false);
            */
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                $"initialize {nameof(AcademicHandler)} for a knight.",
                typeName: nameof(AcademicHandler),
                methodName: nameof(AcademicHandler),
                needStackTrace: true);
        }

    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref academics, nameof(academics), LookMode.Def, LookMode.Value);
        Scribe_Values.Look(ref meditationPoints, nameof(meditationPoints), 0f);
    }

    public bool HasAcademic(KnightAcademicDef academic)
    {
        if (academic is null)
        {
            return false;
        }
        return academics.ContainsKey(academic);
    }

    public int GetAcademicLevel(KnightAcademicDef academic)
    {
        if (academic is null)
        {
            return 0;
        }
        if (academics.TryGetValue(academic, out int level))
        {
            return level;
        }
        return 0;
    }

    public AcceptanceReport CanUpgradeAcademic(
        KnightAcademicDef academicDef,
        KnightPersonality personality = KnightPersonality.None,
        bool directly = false,
        bool resultOnly = false)
    {
        if (!academics.TryGetValue(academicDef, out int academicLevel))
        {
            academicLevel = 0;
        }
        if (academicLevel >= academicDef.MaxStageLevel)
        {
            return resultOnly ? false : "OARO_ReachMax_AcademicLevel".Translate();
        }

        if (!directly)
        {
            float neededPoints = AcademicUtility.GetMeditationPointsNeeded(academicDef, personality, academicLevel + 1);
            if (meditationPoints < neededPoints)
            {
                return resultOnly ? false : "OARO_Insufficient_MeditationPoints".Translate(neededPoints.ToString("F0").Named(KeyLibrary_FormatArgName.Count));
            }
        }

        return true;
    }

    public bool UpgradeAcademic(
        KnightAcademicDef academicDef,
        Pawn pawn,
        KnightPersonality personality = KnightPersonality.None,
        bool directly = false)
    {

        return SetAcademicLevel(academicDef: academicDef,
                                pawn: pawn,
                                targetLevel: GetAcademicLevel(academicDef) + 1,
                                personality: personality,
                                directly: directly);
    }

    public bool SetAcademicLevel(
        KnightAcademicDef academicDef,
        Pawn pawn,
        int targetLevel,
        KnightPersonality personality = KnightPersonality.None,
        bool directly = false)
    {
        if (targetLevel >= academicDef.MaxStageLevel)
        {
            return false;
        }
        if (!academics.TryGetValue(academicDef, out int curAcademicLevel))
        {
            curAcademicLevel = 0;
        }

        if (curAcademicLevel >= targetLevel)
        {
            return false;
        }

        if (!directly)
        {
            float neededPoints = 0f;
            for (int i = curAcademicLevel + 1; i <= targetLevel; i++)
            {
                neededPoints += AcademicUtility.GetMeditationPointsNeeded(academicDef, personality, targetLevel);
            }

            MeditationPoints -= neededPoints;
        }

        for (int i = curAcademicLevel + 1; i <= targetLevel; i++)
        {
            SetAcademicLevelDirectly(academicDef, pawn, i);
        }

        return true;
    }

    public int GetTotalAcademicLevelOf(Predicate<KnightAcademicDef> predicate)
    {
        int totalLevel = 0;
        foreach (KeyValuePair<KnightAcademicDef, int> kv in academics)
        {
            if (predicate(kv.Key))
                totalLevel += kv.Value;
        }
        return totalLevel;
    }

    private void SetAcademicLevelDirectly(KnightAcademicDef academicDef, Pawn pawn, int targetLevel)
    {
        academics[academicDef] = targetLevel;
        academicDef.OnAcademicLevelUpgrade(pawn, targetLevel: targetLevel);
    }
}
