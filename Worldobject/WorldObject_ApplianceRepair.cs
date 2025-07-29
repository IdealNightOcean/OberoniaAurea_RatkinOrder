using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_ApplianceRepair : WorldObject_InteractWithFixedCarvanBase
{
    public override string FixedCaravanName => "OARO_FixedCaravan_ApplianceRepair".Translate();

    private int faultType;
    public int FaultType { get { return faultType; } set { faultType = Mathf.Clamp(value, 0, 2); } }

    private int repairType;
    public int RepairType { get { return repairType; } set { repairType = Mathf.Clamp(value, 0, 2); } }

    protected bool isReasonFound;
    private int maxConstructionSkill = -1;

    public override int TicksNeeded => isReasonFound ? 30000 : 20000;

    public override void PostMake()
    {
        base.PostMake();
        faultType = Rand.RangeInclusive(0, 2);
    }

    public override bool StartWork(Caravan caravan)
    {
        maxConstructionSkill = OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(caravan.PawnsListForReading, SkillDefOf.Construction);
        if (maxConstructionSkill < 0)
        {
            return false;
        }

        if (isReasonFound)
        {
            return true;
        }
        else
        {
            return base.StartWork(caravan);
        }
    }

    protected override void FinishWork()
    {
        TaggedString taggedString;

        if (isReasonFound)
        {
            if (Rand.Chance(0.1f))
            {
                taggedString = "OARO_ApplianceRepair_RepairSuccess".Translate();

            }
            else
            {
                taggedString = "OARO_ApplianceRepair_RepairFail".Translate();

                Destroy();
            }
        }
        else
        {

            if (Rand.Chance(0.1f))
            {
                isReasonFound = true;
                taggedString = "OARO_ApplianceRepair_FindReasonSuccess".Translate()
                               + "\n"
                               + ("OARO_ApplianceRepair_Reason_" + FaultType.ToString()).Translate();

            }
            else
            {
                taggedString = "OARO_ApplianceRepair_FindReasonFail".Translate();
            }
        }

        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(taggedString));
    }

    protected override void InterruptWork() { }

    protected override void Reset()
    {
        base.Reset();
        maxConstructionSkill = -1;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref faultType, "faultType", 0);
        Scribe_Values.Look(ref repairType, "repairType", 0);
        Scribe_Values.Look(ref maxConstructionSkill, "maxConstructionSkill", -1);
        Scribe_Values.Look(ref isReasonFound, "isReasonFound", defaultValue: false);
    }
}