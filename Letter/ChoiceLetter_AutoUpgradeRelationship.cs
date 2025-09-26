using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ChoiceLetter_AutoUpgradeRelationship : ChoiceLetter_RatkinOrder
{
    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (ArchivedOnly || relatedOrder is null)
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

    private DiaOption Option_Accept => new("OARO_AutoUpgradeRelationship_Accept".Translate())
    {
        action = AcctptAction,
        resolveTree = true,
    };

    private new DiaOption Option_Reject => new("OARO_AutoUpgradeRelationship_Reject".Translate())
    {
        action = delegate { Find.LetterStack.RemoveLetter(this); },
        resolveTree = true
    };

    private void AcctptAction()
    {
        Find.LetterStack.RemoveLetter(this);
        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
        if (map is not null && RelationshipUtility.TryTriggerRelationshipQuest(relatedOrder, map))
        {
            ModUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo("OARO_AutoUpgradeRelationship_Triggered".Translate(relatedOrder.Name), relatedOrder);
        }
        else
        {
            ModUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo("OARO_AutoUpgradeRelationship_TriggerFailed".Translate(relatedOrder.Name), relatedOrder);
        }
    }
}