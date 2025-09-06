using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 机械故障村庄（特化类）
/// </summary>
public sealed class WorldObject_ApplianceRepair : WorldObject_InteractWithFixedCaravan_Nameable
{
    private static readonly string[] faultName = ["OARO_ApplianceFault_line", "OARO_ApplianceFault_Component", "OARO_ApplianceFault_Linkage"];
    private static readonly string[] repairName = ["OARO_ApplianceRepair_line", "OARO_ApplianceRepair_Component", "OARO_ApplianceRepair_Linkage"];
    public override string FixedCaravanName => "OARO_FixedCaravan_ApplianceRepair".Translate();

    private int faultType;

    private int repairType;
    public int RepairType { get { return repairType; } set { repairType = Mathf.Clamp(value, 0, 2); } }

    private bool isReasonFound;
    private float successChance;

    public override int TicksNeeded => isReasonFound ? 30000 : 20000;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref faultType, "faultType", 0);
        Scribe_Values.Look(ref repairType, "repairType", 0);
        Scribe_Values.Look(ref successChance, "successChance", 0f);
        Scribe_Values.Look(ref isReasonFound, "isReasonFound", defaultValue: false);
    }

    public override void PostMake()
    {
        base.PostMake();
        faultType = Rand.RangeInclusive(0, 2);
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        if (OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(caravan.PawnsListForReading, SkillDefOf.Construction) < 0)
        {
            Messages.Message("OARO_NoOneCanDo".Translate(SkillDefOf.Construction.label), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        base.Notify_CaravanArrived(caravan);
    }

    public override bool StartWork(Caravan caravan)
    {
        if (isReasonFound)
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
        (Pawn maxPawn, int maxLevel) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Construction);
        TaggedString taggedString;

        if (isReasonFound)
        {
            if (Rand.Chance(successChance))
            {
                taggedString = "OARO_ApplianceRepair_RepairSuccess".Translate(maxPawn);
                SendWorkResolvedSignal();
            }
            else
            {
                taggedString = "OARO_ApplianceRepair_RepairFail".Translate();
            }
        }
        else
        {

            if (Rand.Chance(0.1f))
            {
                isReasonFound = true;
                string reason = faultName[faultType].Translate();
                taggedString = "OARO_ApplianceRepair_FindReasonSuccess".Translate(maxPawn, reason);

            }
            else
            {
                taggedString = "OARO_ApplianceRepair_FindReasonFail".Translate();
            }
        }

        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(taggedString));
    }

    protected override void InterruptWork() { }

    private void RepairDialog(Caravan caravan)
    {
        StringBuilder sb = new("OARO_ApplianceRepair_RepairInfo".Translate());
        sb.AppendInNewLine("OARO_ApplianceRepair_CurFault");
        sb.Append(": ");
        sb.Append(faultName[faultType].Translate());

        DiaNode repairNode = new(sb.ToString());

        DiaOption Option_Line = new(repairName[0])
        {
            action = delegate
            {
                RepairStart(0, caravan);
            },
            resolveTree = true
        };

        DiaOption Option_Component = new(repairName[1])
        {
            action = delegate
            {
                RepairStart(1, caravan);
            },
            resolveTree = true
        };
        if (!CaravanInventoryUtility.HasThings(caravan, ThingDefOf.ComponentIndustrial, 2))
        {
            Option_Component.Disable(null);
        }

        DiaOption Option_Linkage = new(repairName[2])
        {
            action = delegate
            {
                RepairStart(2, caravan);
            },
            resolveTree = true
        };

        repairNode.options.Add(Option_Line);
        repairNode.options.Add(Option_Component);
        repairNode.options.Add(Option_Linkage);

        repairNode.options.Add(new DiaOption("Close".Translate())
        {
            resolveTree = true
        });

        Find.WindowStack.Add(new Dialog_NodeTree(repairNode));
    }

    private void RepairStart(int repairType, Caravan caravan)
    {
        RepairType = repairType;
        successChance = 0f;


        int maxConstructionSkill = OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(caravan.PawnsListForReading, SkillDefOf.Construction);
        if (RepairType == 0)
        {
            if (faultType == 0)
            {
                successChance *= 2f;
            }
            else if (faultType == 1)
            {
                successChance *= 0.5f;
            }
        }
        else if (RepairType == 1)
        {
            OAFrame_CaravanUtility.RemoveThingsOfDef(caravan, ThingDefOf.ComponentIndustrial, 2);
            if (faultType == 1)
            {
                successChance = 1f;
            }
            else if (faultType == 2)
            {
                successChance *= 0.5f;
            }
        }
        else
        {
            if (faultType == 0)
            {
                successChance *= 0.5f;
            }
            else if (faultType == 2)
            {
                successChance *= 2f;
            }
        }

        base.StartWork(caravan);
    }

}