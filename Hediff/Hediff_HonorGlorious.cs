using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_HonorGlorious : Hediff
{
    [Unsaved] private HediffStage curStage;
    public override HediffStage CurStage => curStage;

    private int ticksToReset = -1;
    private float curMeleeDamageFactor = 1.25f;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
        Scribe_Values.Look(ref curMeleeDamageFactor, "curMeleeDamageFactor", 1.25f);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            InitStage();
        }
    }

    public override void PostMake()
    {
        base.PostMake();
        InitStage();
    }

    public override void TickInterval(int delta)
    {
        if (ticksToReset > 0 && (ticksToReset -= delta) <= 0)
        {
            ticksToReset = -1;
            SetMeleeDamageFactor(1.25f);
        }
    }

    public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo targets)
    {
        if (verb.IsMeleeAttack)
        {
            ticksToReset = 1800; //30秒
            SetMeleeDamageFactor(curMeleeDamageFactor + 0.05f);
        }
    }

    private void SetMeleeDamageFactor(float newValue)
    {
        curMeleeDamageFactor = Mathf.Clamp(newValue, 1.25f, 2.25f);
        if (curStage is not null && !curStage.statFactors.NullOrEmpty())
        {
            curStage.statFactors[0].value = curMeleeDamageFactor;
        }
    }

    private void InitStage()
    {
        curStage = new()
        {
            statFactors = [],
            statOffsets = []
        };
        curStage.statFactors.Add(new StatModifier()
        {
            stat = StatDefOf.MeleeDamageFactor,
            value = curMeleeDamageFactor
        });
        curStage.statFactors.Add(new StatModifier()
        {
            stat = StatDefOf.IncomingDamageFactor,
            value = 0.9f
        });
        curStage.statOffsets.Add(new StatModifier()
        {
            stat = StatDefOf.ShootingAccuracyPawn,
            value = 2.0f
        });
        curStage.statOffsets.Add(new StatModifier()
        {
            stat = StatDefOf.MeleeHitChance,
            value = 2.0f
        });
    }
}