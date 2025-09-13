using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ChoiceLetter_KnightGroupProactiveVisit : ChoiceLetter_RatkinOrder
{
    private AroundKnightGroup knightGroup;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref knightGroup, "knightGroup");
    }

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (ArchivedOnly || knightGroup is null)
            {
                yield return Option_Close;
            }
            else
            {
                yield return Option_Reject;
                yield return Option_Postpone;
            }
        }
    }

    public DiaOption Option_Accept => new("Accept".Translate())
    {
        action = ProactiveVisit,
        resolveTree = true
    };

    private void ProactiveVisit()
    {
        Find.LetterStack.RemoveLetter(this);

        Map map = MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
        if (map is null || !OrderInteractionHandler.AroundKnightGroupsManager.TriggerVisitQuest(knightGroup, map))
        {
            OrderInteractionHandler.AroundKnightGroupsManager.RemoveKnightGroup(knightGroup);
            OrderInteractionUtility.AroundKnightGroupVisitInvalid(knightGroup.Branch, isProactive: true);
        }
    }

    public override void Removed()
    {
        base.Removed();
        knightGroup = null;
    }
}
