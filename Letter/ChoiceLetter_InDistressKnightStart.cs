using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal class ChoiceLetter_InDistressKnightStart : ChoiceLetter_RatkinOrder
{
    internal string OutSignalAccepted;
    internal string OutSignalRejected;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref OutSignalAccepted, nameof(OutSignalAccepted));
        Scribe_Values.Look(ref OutSignalAccepted, nameof(OutSignalAccepted));
    }

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (quest?.State != QuestState.Ongoing || ArchivedOnly)
            {
                yield return Option_Close;
                yield break;
            }
            yield return AcceptOption;
            yield return RejectOption;
            yield return Option_Postpone;
            if (quest is not null)
            {
                yield return Option_ViewInQuestsTab();
            }
        }
    }

    private DiaOption AcceptOption => new("Accept".Translate())
    {
        action = delegate
        {
            Find.SignalManager.SendSignal(new Signal(OutSignalAccepted));
            Find.LetterStack.RemoveLetter(this);
        },
        resolveTree = true
    };

    private DiaOption RejectOption => new("OAFrame_Reject".Translate())
    {
        action = delegate
        {
            Find.SignalManager.SendSignal(new Signal(OutSignalRejected));
            Find.LetterStack.RemoveLetter(this);
        },
        resolveTree = true
    };

    public override void Removed()
    {
        base.Removed();
        OutSignalAccepted = null;
        OutSignalRejected = null;
    }

}