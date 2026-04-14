using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueHandler : IExposable
{
    private readonly ResidentKnight knight;

    public Pawn Pawn => knight.Pawn;


    private List<KnightVirtue> virtues = [];

    public IReadOnlyList<KnightVirtue> Virtues => virtues;
    public int TotalVirtueCount => virtues.Count;
    public int TotalVirtueLevel => virtues.Sum(v => v.Level);
    public int CurMaxVirtueCount
    {
        get
        {
            return VirtueStatValueCache.GetCachedResult() switch
            {
                < 1f => 0,
                < 5f => 2,
                < 12f => 3,
                _ => 4,
            };
        }
    }

    private int curKnightCreedLevel;

    private HediffStageTemplate BuffStageTemplate { get; } = new();
    private SimpleValueCache<float> VirtueStatValueCache { get; }

    public KnightVirtueHandler(ResidentKnight knight)
    {
        this.knight = knight ?? throw new ArgumentNullException(nameof(knight));

        VirtueStatValueCache = new(cacheInterval: 30000,
                                   checker: () => Pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_PawnVirtue));
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref virtues, nameof(virtues), LookMode.Deep);
    }

    public void TickHour()
    {
        CheckKnightCreed();
    }

    public bool TryAddVirtue(KnightVirtueDef virtueDef, int level)
    {
        if (virtues.Count >= CurMaxVirtueCount)
        {
            return false;
        }
        return AddVirtue(virtueDef, level);
    }



    public bool UpgradeVirtue(KnightVirtueDef virtueDef)
    {
        KnightVirtue virtue = GetVirtue(virtueDef);
        if (virtue is null)
        {
            return false;
        }
        if (virtue.Level >= virtueDef.maxLevel)
        {
            return false;
        }
        virtue.Level++;
        VirtuesChanged();
        return true;
    }

    public bool HasVirtue(KnightVirtueDef virtue)
    {
        for (int i = 0; i < virtues.Count; i++)
        {
            if (virtue == virtues[i].Def)
            {
                return true;
            }
        }
        return false;
    }

    public KnightVirtue GetVirtue(KnightVirtueDef virtue)
    {
        for (int i = 0; i < virtues.Count; i++)
        {
            if (virtue == virtues[i].Def)
            {
                return virtues[i];
            }
        }
        return null;
    }


    public bool AbandonVirtue(ResidentKnight record, KnightVirtueDef virtue)
    {
        if (!RemoveVirtue(virtue))
        {
            return false;
        }
        float meditationPointsToReduce = record.AcademicHandler.TotalAcademicLevel.Value * 500f;
        if (virtue.relatedPersonality == record.Personality)
        {
            meditationPointsToReduce *= 2f;
        }

        record.MeditationPoints -= meditationPointsToReduce;

        return true;
    }

    public HediffStage GetNewBuffStage()
    {
        if (!BuffStageTemplate.IsReady)
        {
            RefreshBuffStage();
        }

        return BuffStageTemplate.GetNewHediffStage();
    }

    private bool AddVirtue(KnightVirtueDef virtueDef, int level)
    {
        if (HasVirtue(virtueDef))
        {
            return false;
        }
        virtues.Add(new KnightVirtue(virtueDef, level));
        VirtuesChanged();
        return true;
    }

    private bool RemoveVirtue(KnightVirtueDef virtue)
    {
        for (int i = 0; i < virtues.Count; i++)
        {
            if (virtue == virtues[i].Def)
            {
                virtues.RemoveAt(i);
                VirtuesChanged();
                return true;
            }
        }

        return false;
    }


    private void VirtuesChanged()
    {
        BuffStageTemplate.MarkInvalid();
    }

    private void CheckKnightCreed()
    {
        float virtueStatvalue = VirtueStatValueCache.GetCachedResult();
        int targetKnightCreedLevel = virtueStatvalue switch
        {
            < 15f => 0,
            < 30f => 1,
            _ => 2
        };

        if (curKnightCreedLevel != targetKnightCreedLevel)
        {
            curKnightCreedLevel = targetKnightCreedLevel;
            if (targetKnightCreedLevel <= 0)
            {
                Pawn.RemoveFirstHediffOfDef(OARO_HediffDefOf.OARO_Hediff_KnightCreed);
            }
            else
            {
                Hediff hediff = Pawn.health.GetOrAddHediff(OARO_HediffDefOf.OARO_Hediff_KnightCreed);
                hediff.Severity = targetKnightCreedLevel;
            }
        }
    }

    private void RefreshBuffStage()
    {
        BuffStageTemplate.ResetTemplate();

        VirtueStatValueCache.Reset();
        float virtueStatvalue = VirtueStatValueCache.GetCachedResult();
        foreach (KnightVirtue virtue in virtues)
        {
            foreach (KnightVirtueTraitDef virtueTrait in virtue.SelectedTraits)
            {
                BuffStageTemplate.AddOffsets(virtueTrait.statOffsets);
                BuffStageTemplate.AddOffsets(virtueTrait.statFactors);

                if (virtueTrait.statOffsetsByVirtue is not null)
                {
                    foreach (StatModifierBySeverity statOffsetByVirtue in virtueTrait.statOffsetsByVirtue)
                    {
                        BuffStageTemplate.AddOffset(statOffsetByVirtue.stat, statOffsetByVirtue.valueBySeverity.Evaluate(virtueStatvalue));
                    }
                }

                if (virtueTrait.statFactorsByVirtue is not null)
                {
                    foreach (StatModifierBySeverity statFactorByVirtue in virtueTrait.statFactorsByVirtue)
                    {
                        BuffStageTemplate.AddFactor(statFactorByVirtue.stat, statFactorByVirtue.valueBySeverity.Evaluate(virtueStatvalue));
                    }
                }
            }
        }

        BuffStageTemplate.FinalizeTemplate();
    }

}