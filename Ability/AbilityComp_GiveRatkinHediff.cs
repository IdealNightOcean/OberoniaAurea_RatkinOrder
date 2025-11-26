using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AbilityComp_GiveKnightsOfMapHediff : CompAbilityEffect_WithDuration
{
    private new CompProperties_AbilityGiveHediff Props => (CompProperties_AbilityGiveHediff)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        IReadOnlyList<Pawn> pawns = parent.pawn.MapHeld.mapPawns.AllPawnsSpawned;

        for (int i = 0; i < pawns.Count; i++)
        {
            if (KnightPawnsManager.Instance.IsKnight(pawns[i]))
            {
                ApplyInner(pawns[i], parent.pawn);
            }
        }
    }

    protected void ApplyInner(Pawn target, Pawn caster)
    {
        if (target is null)
        {
            return;
        }
        if (TryResist(target))
        {
            MoteMaker.ThrowText(target.DrawPos, target.Map, "Resisted".Translate());
            return;
        }
        if (Props.replaceExisting)
        {
            Hediff firstHediffOfDef = target.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            if (firstHediffOfDef is not null)
            {
                target.health.RemoveHediff(firstHediffOfDef);
            }
        }
        Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, target, Props.onlyBrain ? target.health.hediffSet.GetBrain() : null);
        HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
        if (hediffComp_Disappears is not null)
        {
            hediffComp_Disappears.ticksToDisappear = GetDurationSeconds(target).SecondsToTicks();
        }
        if (Props.severity >= 0f)
        {
            hediff.Severity = Props.severity;
        }
        HediffComp_Link hediffComp_Link = hediff.TryGetComp<HediffComp_Link>();
        if (hediffComp_Link is not null)
        {
            hediffComp_Link.other = caster;
            hediffComp_Link.drawConnection = target == parent.pawn;
        }
        target.health.AddHediff(hediff);
    }

    protected bool TryResist(Pawn pawn)
    {
        return pawn.HostileTo(Faction.OfPlayer);
    }

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        return parent.pawn.Faction == Faction.OfPlayer;
    }
}