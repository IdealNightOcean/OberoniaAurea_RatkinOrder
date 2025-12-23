using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ChoiceLetter_KnightGroupProactiveVisit : ChoiceLetter_RatkinOrder
{
    private AroundKnightGroup knightGroup;

    public override bool CanDismissWithRightClick => false;

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
                yield return Option_Accept;
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

        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
        if (map is null || !AroundKnightGroupsManager.Instance.TriggerVisitQuest(knightGroup, map))
        {
            AroundKnightGroupsManager.Instance.RemoveKnightGroup(knightGroup);
            GlobalInteractionUtility.AroundKnightGroupVisitInvalidDialog(knightGroup, isProactive: true);
        }
    }

    public override void Removed()
    {
        base.Removed();
        knightGroup = null;
    }
}
