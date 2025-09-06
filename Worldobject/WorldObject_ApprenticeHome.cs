using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_ApprenticeHome : WorldObject_Interactive_Nameable
{
    public override void Notify_CaravanArrived(Caravan caravan)
    {
        base.Notify_CaravanArrived(caravan);
        Find.LetterStack.ReceiveLetter(label: "OARO_Apprentice_NoOnePickUpReasonLabel".Translate(),
                                       text: "OARO_Apprentice_NoOnePickUpReason".Translate(),
                                       LetterDefOf.NegativeEvent,
                                       lookTargets: this);

        QuestUtility.SendQuestTargetSignals(questTags, "Resolved");
        if (!Destroyed)
        {
            Destroy();
        }
    }
}