using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public sealed class CompProperties_SuperHeavyHowitzer : CompProperties
{
    public JobDef repairJob;
    public JobDef checkJob;
    public SimpleCurve checkLatentCurve;
    public SimpleCurve doubleRepairCurve;

    public CompProperties_SuperHeavyHowitzer()
    {
        compClass = typeof(CompSuperHeavyHowitzer);
    }
}

public sealed class CompSuperHeavyHowitzer : ThingComp
{
    private CompProperties_SuperHeavyHowitzer Props => (CompProperties_SuperHeavyHowitzer)props;

    private int normalFaultLeft;
    private int extraFaultLeft;

    private int latentFault;
    private int latentFaultChecked;

    public bool Repaired => normalFaultLeft == 0;
    public bool PerfectRepaired => latentFault > 0
                                   && latentFaultChecked == latentFault
                                   && normalFaultLeft == 0
                                   && extraFaultLeft == 0;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref normalFaultLeft, "normalFaultLeft", 0);
        Scribe_Values.Look(ref extraFaultLeft, "extraFaultLeft", 0);

        Scribe_Values.Look(ref latentFault, "latentFault", 0);
        Scribe_Values.Look(ref latentFaultChecked, "latentFaultChecked", 0);
    }

    public void InitFault(int normalFault, int latentFault)
    {
        normalFaultLeft = normalFault;
        this.latentFault = latentFault;
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new();

        sb.AppendInNewLine("OARO_FaultHowitzer_NormalFault".Translate(normalFaultLeft));
        if (latentFaultChecked > 0)
        {
            sb.AppendInNewLine("OARO_FaultHowitzer_ExtraFault".Translate(extraFaultLeft));
        }

        if (Prefs.DevMode)
        {
            sb.AppendInNewLine("--------");
            sb.AppendInNewLine($"latentFault: {latentFault}");
            sb.AppendInNewLine($"latentFaultChecked: {latentFaultChecked}");
            sb.AppendInNewLine($"Repaired: {Repaired}");
            sb.AppendInNewLine($"PerfectRepaired: {PerfectRepaired}");
        }

        return sb.ToString();
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        foreach (FloatMenuOption menuOption in base.CompFloatMenuOptions(selPawn))
        {
            yield return menuOption;
        }
        if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
        {
            yield return new FloatMenuOption("CannotUseReason".Translate(parent.Label) + " (" + "NoPath".Translate().CapitalizeFirst() + ")", null);
            yield break;
        }
        if (normalFaultLeft > 0 || extraFaultLeft > 0)
        {
            if (FindClosestComponent(selPawn) is null)
            {
                yield return new FloatMenuOption("CannotRepair".Translate(parent.Label) + " (" + "NoComponentsToRepair".Translate() + ")", null);
            }
            else
            {
                yield return new FloatMenuOption(label: "RepairThing".Translate(parent.Label),
                                                 action: delegate { RepairJob(selPawn); });
            }
        }

        float checkChance = Props.checkLatentCurve.Evaluate(selPawn.GetSkillLevel(SkillDefOf.Construction));
        yield return new FloatMenuOption(label: "OARO_SuperHeavyHowitzer_Check".Translate(
                                                    parent.Label,
                                                    checkChance.ToStringPercent("0.##").Named(KeyLibrary_FormatArgName.Chance)),
                                         action: delegate { selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(Props.checkJob, parent), JobTag.Misc); });

    }

    private void RepairJob(Pawn selPawn)
    {
        Thing component = FindClosestComponent(selPawn);
        Job job = JobMaker.MakeJob(Props.repairJob, parent, component);
        job.count = 1;
        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }

    private Thing FindClosestComponent(Pawn pawn)
    {
        return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDefOf.ComponentIndustrial), PathEndMode.InteractionCell, TraverseParms.For(pawn, pawn.NormalMaxDanger()), 9999f, t => !t.IsForbidden(pawn) && pawn.CanReserve(t));
    }

    public void RepairHowitzer(Pawn pawn)
    {
        float doubleRepairChance = Props.doubleRepairCurve.Evaluate(pawn.GetSkillLevel(SkillDefOf.Construction));
        int repairCount = Rand.Chance(doubleRepairChance) ? 2 : 1;
        if (normalFaultLeft > 0)
        {
            normalFaultLeft = Mathf.Max(normalFaultLeft - repairCount, 0);
        }
        else if (extraFaultLeft > 0)
        {
            extraFaultLeft = Mathf.Max(extraFaultLeft - repairCount, 0);
        }
        Messages.Message(
            text: "OARO_SuperHeavyHowitzer_Repaired".Translate(parent.LabelCap, pawn.Named(KeyLibrary_FormatArgName.PAWN), repairCount.Named(KeyLibrary_FormatArgName.Count)),
            def: MessageTypeDefOf.PositiveEvent);
    }

    public void CheckHowitzer(Pawn pawn)
    {
        float checkChance = 0.2f + (pawn.skills?.GetSkill(SkillDefOf.Construction).GetLevel() ?? 0f) * 0.035f;
        if (latentFault <= 0 || latentFaultChecked == latentFault)
        {
            if (Rand.Chance(checkChance))
            {
                Messages.Message("OARO_SuperHeavyHowitzer_NoLatentFault".Translate(parent.Label, pawn.Named(KeyLibrary_FormatArgName.PAWN)), MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                Messages.Message("OARO_SuperHeavyHowitzer_NotFindLatentFault".Translate(parent.Label, pawn.Named(KeyLibrary_FormatArgName.PAWN)), MessageTypeDefOf.NeutralEvent);
            }
            return;
        }
        else if (Rand.Chance(checkChance))
        {
            Messages.Message("OARO_SuperHeavyHowitzer_FindLatentFault".Translate(parent.Label, pawn.Named(KeyLibrary_FormatArgName.PAWN)), MessageTypeDefOf.PositiveEvent);
            latentFaultChecked++;
            extraFaultLeft += 3;
        }
        else
        {
            Messages.Message("OARO_SuperHeavyHowitzer_NotFindLatentFault".Translate(parent.Label, pawn.Named(KeyLibrary_FormatArgName.PAWN)), MessageTypeDefOf.NeutralEvent);
        }
    }
}
