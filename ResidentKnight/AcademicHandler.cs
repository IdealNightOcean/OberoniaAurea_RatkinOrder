using NightOcean;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame.DataLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AcademicHandler : IExposable
{
    public ResidentPawn ResidentPawn { get; }

    private Dictionary<KnightAcademicDef, int> academics = [];
    public IReadOnlyDictionary<KnightAcademicDef, int> Academics => academics;

    public LazyMutable<int> TotalAcademicLevel { get; }

    private float meditationPoints;
    public float MeditationPoints
    {
        get => meditationPoints; set => meditationPoints = Mathf.Max(0f, value);
    }

    internal AcademicHandler(ResidentPawn residentPawn)
    {
        ResidentPawn = residentPawn;
        TotalAcademicLevel = new(refreshFunc: () => academics?.Values.Sum() ?? 0);
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
            return 0;

        if (academics.TryGetValue(academic, out int level))
            return level;

        return 0;
    }

    public AcceptanceReport CanUpgradeAcademic(KnightAcademicDef academicDef,
                                               float pointsOverride = -1f,
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
            float neededPoints = pointsOverride > 0f
                ? pointsOverride
                : AcademicUtility.GetAcademicPointsCost(residentPawn: this.ResidentPawn,
                                                        academicDef: academicDef,
                                                        sourceLevel: academicLevel,
                                                        targetLevel: academicLevel + 1,
                                                        resultOnly: true,
                                                        explanation: out _);
            if (meditationPoints < neededPoints)
            {
                return resultOnly ? false : "OARO_Insufficient_MeditationPoints".Translate(neededPoints.ToString("F0").Named(KeyLibrary_FormatArgName.Count));
            }
        }

        return true;
    }

    public bool UpgradeAcademic(KnightAcademicDef academicDef, int upgrade = 1, bool directly = false)
    {
        int targetLevel = GetAcademicLevel(academicDef) + upgrade;

        if (targetLevel >= academicDef.MaxStageLevel)
            return false;

        if (!academics.TryGetValue(academicDef, out int curLevel))
            curLevel = 0;

        if (curLevel >= targetLevel)
            return false;

        if (!directly)
        {
            float neededPoints = AcademicUtility.GetAcademicPointsCost(residentPawn: this.ResidentPawn,
                                                                       academicDef: academicDef,
                                                                       sourceLevel: curLevel,
                                                                       targetLevel: targetLevel,
                                                                       resultOnly: true,
                                                                       explanation: out _);

            MeditationPoints -= neededPoints;
        }

        SetAcademicLevelDirectly(academicDef, curLevel, targetLevel);

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

    private void SetAcademicLevelDirectly(KnightAcademicDef academicDef, int oldLevel, int newLevel)
    {
        academics[academicDef] = newLevel;
        academicDef.OnAcademicLevelUpgrade(ResidentPawn.Pawn, oldLevel, newLevel);
    }
}
