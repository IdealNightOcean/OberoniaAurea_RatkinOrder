using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Text;
using Verse;
using Verse.Utility;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 机械故障村庄（特化类）
/// </summary>
public sealed class WorldObject_ApplianceRepair : WorldObject_InteractWithFixedCaravan_Nameable
{
    private enum FaultType
    {
        Line,
        Component,
        Linkage
    }

    public override string FixedCaravanName => "OARO_FixedCaravan_ApplianceRepair".Translate();

    private FaultType faultType;
    private FaultType repairType;
    private string FaultLabel => $"OARO_ApplianceFault_{faultType}".Translate();

    private bool hasFoundReason;
    private float successChance;

    public override int TicksNeeded => hasFoundReason ? 30000 : 20000;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref faultType, nameof(faultType), FaultType.Line);
        Scribe_Values.Look(ref repairType, nameof(repairType), FaultType.Line);
        Scribe_Values.Look(ref successChance, nameof(successChance), 0f);
        Scribe_Values.Look(ref hasFoundReason, nameof(hasFoundReason), defaultValue: false);
    }

    public override void PostMake()
    {
        base.PostMake();
        faultType = EnumUtility.GetValues<FaultType>().RandomElement();
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());
        if (hasFoundReason)
        {
            sb.AppendInNewLine("OARO_ApplianceRepair_CurFault".Translate());
            sb.Append(": ");
            sb.Append(FaultLabel);
        }
        return sb.ToString();
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        if (!caravan.PawnsListForReading.Any(p => p.skills is not null && !p.skills.GetSkill(SkillDefOf.Crafting).TotallyDisabled))
        {
            Messages.Message("OAFrame_MissSkillAvailablePawn".Translate(SkillDefOf.Crafting.Named(KeyLibrary_FormatArgName.SKILL)), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        base.Notify_CaravanArrived(caravan);
    }

    public override bool StartWork(Caravan caravan)
    {
        if (hasFoundReason)
        {
            RepairDialog(caravan);
            return true;
        }
        else
        {
            return base.StartWork(caravan);
        }
    }

    protected override void FinishWork()
    {
        (Pawn maxPawn, int _) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Crafting);
        TaggedString taggedString;

        if (hasFoundReason)
        {
            if (Rand.Chance(successChance))
            {
                taggedString = "OARO_ApplianceRepair_RepairSuccess".Translate(maxPawn.Named(KeyLibrary_FormatArgName.PAWN));
                this.SendWorkResolvedSignal();
            }
            else
            {
                taggedString = "OARO_ApplianceRepair_RepairFail".Translate();
            }
        }
        else
        {
            if (Rand.Chance(GetSuccessChance(associatedFixedCaravan.PawnsListForReading)))
            {
                hasFoundReason = true;
                taggedString = "OARO_ApplianceRepair_FindReasonSuccess".Translate(maxPawn.Named(KeyLibrary_FormatArgName.PAWN), FaultLabel.Named(KeyLibrary_FormatArgName.Reason));

            }
            else
            {
                taggedString = "OARO_ApplianceRepair_FindReasonFail".Translate();
            }
        }

        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(taggedString, Faction));
    }

    private float GetSuccessChance(IEnumerable<Pawn> pawns)
    {
        int maxSuccessChance = OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(pawns, SkillDefOf.Crafting);
        return 0.25f + 0.05f * maxSuccessChance;
    }

    protected override void InterruptWork()
    {
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo(
            text: hasFoundReason ? "OARO_ApplianceRepair_Interrupt_HasFoundReason".Translate()
                                 : "OARO_ApplianceRepair_Interrupt_NotFoundReason".Translate(),
            faction: Faction));
    }

    private void RepairDialog(Caravan caravan)
    {
        StringBuilder sb = new("OARO_ApplianceRepair_RepairInfo".Translate());
        sb.AppendInNewLine("OARO_ApplianceRepair_CurFault".Translate());
        sb.Append(": ");
        sb.Append(FaultLabel);

        DiaNode repairNode = new(sb.ToString());

        foreach (FaultType type in EnumUtility.GetValues<FaultType>())
        {
            DiaOption option = new($"OARO_ApplianceRepair_{type}".Translate())
            {
                action = () => RepairStart(type, caravan),
                resolveTree = true
            };
            if (type == FaultType.Component && !CaravanInventoryUtility.HasThings(caravan, ThingDefOf.ComponentIndustrial, 2))
            {
                option.Disable("OAFrame_NeedCountOfThing".Translate(ThingDefOf.ComponentIndustrial.label, 2.ToString()));
            }
            repairNode.options.Add(option);
        }
        repairNode.options.Add(OAFrame_DiaUtility.DefaultPostponeOption);

        Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(repairNode, Faction));
    }

    private void RepairStart(FaultType repairType, Caravan caravan)
    {
        this.repairType = repairType;
        if (repairType == FaultType.Component)
        {
            OAFrame_CaravanUtility.RemoveThingsOfDef(caravan, ThingDefOf.ComponentIndustrial, 2);
        }

        int maxConstructionSkill = OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(caravan.PawnsListForReading, SkillDefOf.Crafting);
        float successChance = maxConstructionSkill * 0.05f;
        successChance *= faultType switch
        {
            FaultType.Line => 0.8f,
            FaultType.Component => 1f,
            FaultType.Linkage => 0.6f,
            _ => 1f
        };

        if (repairType == faultType)
        {
            successChance *= 2f;
        }
        this.successChance = successChance;
        base.StartWork(caravan);
    }
}