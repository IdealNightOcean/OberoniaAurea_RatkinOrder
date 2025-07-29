using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal class ChoiceLetter_TemporaryEncampment : ChoiceLetter
{
    public WorldObject_TemporaryEncampment temporaryEncampment;

    private DiaOption Option_Accept => new("Accept".Translate())
    {
        action = delegate
        {
            Find.LetterStack.RemoveLetter(this);
        },
        resolveTree = true
    };

    private new DiaOption Option_Reject => new("RejectLetter".Translate())
    {
        action = delegate
        {
            temporaryEncampment?.RejectSupplyRequest();
            Find.LetterStack.RemoveLetter(this);
        },
        resolveTree = true
    };

    public void SetWorldObject(WorldObject_TemporaryEncampment worldObject)
    {
        temporaryEncampment = worldObject;
        lookTargets = worldObject;
    }

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (temporaryEncampment is null || !temporaryEncampment.hasSupplyRequest)
            {
                yield return Option_Close;
            }
            else
            {
                if (!ArchivedOnly)
                {
                    yield return Option_Accept;
                    yield return Option_Reject;
                    yield return Option_Postpone;
                }

                if (lookTargets.IsValid())
                {
                    yield return Option_JumpToLocationAndPostpone;
                }
                if (quest != null && !quest.hidden)
                {
                    yield return Option_ViewInQuestsTab("ViewRelatedQuest", postpone: true);
                }
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref temporaryEncampment, "temporaryEncampment");
    }
}