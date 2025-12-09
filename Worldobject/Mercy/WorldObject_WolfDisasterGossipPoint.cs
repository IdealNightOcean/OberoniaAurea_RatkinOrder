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
        if (caravan.PawnsListForReading.Any(p => !p.skills.GetSkill(SkillDefOf.Animals).TotallyDisabled))
        {
            Messages.Message("OAFrame_MissSkillAvailablePawn".Translate(SkillDefOf.Animals.Named(KeyLibrary_FormatArgName.SKILL)), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        base.Notify_CaravanArrived(caravan);
    }

    protected override void FinishWork()
    {
        (Pawn maxAnimalsPawn, _) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Animals);
        float value = Rand.Value;
        if (value < 0.25f)
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterGossipPoint_Discovered".Translate(maxAnimalsPawn)));
            QuestUtility.SendQuestTargetSignals(questTags, "DiscoveredWolf", this.Named(KeyLibrary_FormatArgName.SUBJECT));
        }
        else if (value < 0.75f)
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterGossipPoint_Succeess".Translate(maxAnimalsPawn)));
            QuestUtility.SendQuestTargetSignals(questTags, "SucceessAdvancePoint", this.Named(KeyLibrary_FormatArgName.SUBJECT));
        }
        else
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterGossipPoint_Reduce".Translate(maxAnimalsPawn)));
            QuestUtility.SendQuestTargetSignals(questTags, "ReducePoint", this.Named(KeyLibrary_FormatArgName.SUBJECT));
        }

        this.SafeDestroy();
    }
    protected override void InterruptWork()
    {
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterGossipPoint_Interrupt".Translate()));
    }
}
