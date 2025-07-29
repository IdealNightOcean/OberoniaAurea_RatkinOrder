using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_WolfDisasterPoint : WorldObject_InteractWithFixedCaravan_Village
{
    private static readonly SimpleCurve successCurve = new([new CurvePoint(4, 0), new CurvePoint(15, 1)]);

    public override int TicksNeeded => 7500;
    public override string FixedCaravanName => "OARO_FixedCaravan_WolfDisasterPoint".Translate();
    public override string FixedCaravanWorkDesc() => "OARO_WolfDisasterPoint_TimeLeft".Translate(ticksRemaining.ToStringTicksToPeriod());

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
        foreach (Pawn pawn in associatedFixedCaravan.PawnsListForReading)
        {
            pawn.skills?.Learn(SkillDefOf.Animals, 1000f);
        }

        if (maxAnimalsSkill >= 15)
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterPoint_Discovered".Translate(maxAnimalsPawn)));
            QuestUtility.SendQuestTargetSignals(questTags, "DiscoveredWolf", this.Named("SUBJECT"));

            if (!Destroyed)
            {
                Destroy();
            }
        }
        else
        {
            float successChance = successCurve.Evaluate(maxAnimalsSkill);
            if (Rand.Chance(successChance))
            {
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterPoint_Succeess".Translate(maxAnimalsPawn)));
                QuestUtility.SendQuestTargetSignals(questTags, "SucceessAdvancePoint", this.Named("SUBJECT"));

                if (!Destroyed)
                {
                    Destroy();
                }
            }
            else
            {
                if (Rand.Bool)
                {
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterPoint_Fail".Translate()));
                    QuestUtility.SendQuestTargetSignals(questTags, "FailAdvancePoint", this.Named("SUBJECT"));
                    if (!Destroyed)
                    {
                        Destroy();
                    }
                }
                else
                {
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterPoint_FailWithNew".Translate()));
                }
            }
        }
    }

    protected override void InterruptWork()
    {
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_WolfDisasterPoint_Interrupt".Translate()));
    }
}
