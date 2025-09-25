using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_LostItemsOfTrader : QuestNode
{
    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;

        Faction parentRatkinFaction = slate.Get<Faction>(KeyLibrary_SlateStoreAs.ParentRatkinFaction);
        if (parentRatkinFaction is null)
        {
            FactionValidationParams validationParams = new()
            {
                AllyHostile = false
            };
            parentRatkinFaction = OAFrame_FactionUtility.RandomAvailableFactionOfDef(OARO_ModDefOf.Rakinia_TravelRatkin, validationParams);
        }

        if (parentRatkinFaction is null || Rand.Chance(0.5f))
        {
            NoFurtherAction();
        }
        else
        {
            FollowUpAction(parentRatkinFaction);
        }
    }

    private void NoFurtherAction()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;
    }

    private void FollowUpAction(Faction travelRatkin)
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;
    }
}