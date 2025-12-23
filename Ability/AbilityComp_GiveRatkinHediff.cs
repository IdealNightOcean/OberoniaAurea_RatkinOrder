using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class CompProperties_AbilityGiveKnightsOfMapHediff : CompProperties_AbilityEffectWithDuration
{
    public HediffDef hediffDef;

    public CompProperties_AbilityGiveKnightsOfMapHediff()
    {
        compClass = typeof(AbilityComp_GiveKnightsOfMapHediff);
    }
}

public class AbilityComp_GiveKnightsOfMapHediff : CompAbilityEffect_WithDuration
{
    private new CompProperties_AbilityGiveKnightsOfMapHediff Props => (CompProperties_AbilityGiveKnightsOfMapHediff)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        Pawn caster = parent.pawn;
        Faction casterFaction = caster.Faction;
        int durationSecondsOverride = GetDurationSeconds(caster).SecondsToTicks();
        foreach (Pawn p in caster.MapHeld.mapPawns.AllHumanlikeSpawned)
        {
            if (p.HostileTo(casterFaction))
            {
                continue;
            }

            ApplyInner(p, caster, durationSecondsOverride);
        }
    }

    protected void ApplyInner(Pawn target, Pawn caster, int durationSecondsOverride = -1)
    {
        Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, target);
        if (hediff is null)
        {
            return;
        }
        if (target.CanBeKnight() && KnightPawnsManager.Instance.IsKnight(target))
        {
            hediff.Severity = 2f;
        }
        else
        {
            hediff.Severity = 1f;
        }

        HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
        if (hediffComp_Disappears is not null && durationSecondsOverride > 0)
        {
            hediffComp_Disappears.ticksToDisappear = durationSecondsOverride;
        }

        HediffComp_Link hediffComp_Link = hediff.TryGetComp<HediffComp_Link>();
        if (hediffComp_Link is not null)
        {
            hediffComp_Link.other = caster;
            hediffComp_Link.drawConnection = (target != caster);
        }
        target.health.AddHediff(hediff);
    }

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        return parent.pawn.Spawned;
    }
}