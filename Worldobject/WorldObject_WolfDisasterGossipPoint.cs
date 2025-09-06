using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 狼灾流言检查点（特化类）
/// </summary>
internal sealed class WorldObject_WolfDisasterGossipPoint : WorldObject_InteractWithFixedCaravanBase
{
    public override int TicksNeeded => 7500;
    public override string FixedCaravanName => "OARO_FixedCaravan_WolfDisasterGossipPoint".Translate();
    public override string FixedCaravanWorkDesc() => "OARO_WolfDisasterGossipPoint_TimeLeft".Translate(ticksRemaining.ToStringTicksToPeriod());

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        if (OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(caravan.PawnsListForReading, SkillDefOf.Animals) < 0)
        {
            Messages.Message("OARO_NoOneCanDo".Translate(SkillDefOf.Animals.label), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        base.Notify_CaravanArrived(caravan);
    }

    protected override void FinishWork()
    {
        (Pawn maxAnimalsPawn, int maxAnimalsSkill) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Animals);
        float value = Rand.Value;
        if (value < 0.25f)
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterGossipPoint_Discovered".Translate(maxAnimalsPawn)));
            QuestUtility.SendQuestTargetSignals(questTags, "DiscoveredWolf", this.Named("SUBJECT"));
        }
        else if (value < 0.75f)
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterGossipPoint_Succeess".Translate(maxAnimalsPawn)));
            QuestUtility.SendQuestTargetSignals(questTags, "SucceessAdvancePoint", this.Named("SUBJECT"));
        }
        else
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterGossipPoint_Reduce".Translate(maxAnimalsPawn)));
            QuestUtility.SendQuestTargetSignals(questTags, "ReducePoint", this.Named("SUBJECT"));
        }

        if (!Destroyed)
        {
            Destroy();
        }
    }
    protected override void InterruptWork()
    {
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterGossipPoint_Interrupt".Translate()));
    }
}
