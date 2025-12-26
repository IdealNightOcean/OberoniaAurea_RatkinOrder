using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ChoiceLetter_KnightGroupProactiveVisit : ChoiceLetter_RatkinOrder
{
    public AroundKnightGroup KnightGroup;

    public override bool CanDismissWithRightClick => false;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref KnightGroup, nameof(KnightGroup));
    }

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (ArchivedOnly || KnightGroup is null)
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
        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
        if (!AroundKnightGroupsManager.Instance.TryTriggerVisitQuest(KnightGroup, map, removeWhenInvalid: true))
        {
            AroundKnightGroupsManager.Instance.RemoveKnightGroup(KnightGroup);
            GlobalInteractionUtility.AroundKnightGroupVisitInvalidDialog(KnightGroup, isProactive: true);
        }
        Find.LetterStack.RemoveLetter(this);
    }

    public override void Removed()
    {
        base.Removed();
        KnightGroup = null;
    }
}
