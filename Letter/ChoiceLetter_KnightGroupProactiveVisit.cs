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
        action = delegate
        {
            OrderInteractionHandler.AroundKnightGroupsManager.TriggerVisitQuest(knightGroup);
            Find.LetterStack.RemoveLetter(this);
        },
        resolveTree = true
    };

    public override void Removed()
    {
        base.Removed();
        knightGroup = null;
    }
}
