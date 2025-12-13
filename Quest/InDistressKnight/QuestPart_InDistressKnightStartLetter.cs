using Verse;

namespace OberoniaAurea.RatkinOrder;

internal class QuestPart_InDistressKnightStartLetter : QuestPart_RimOrderLetter, IOnRatkinOrderRemoved
{
    internal string OutSignalAccepted;
    internal string OutSignalRejected;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref OutSignalAccepted, nameof(OutSignalAccepted));
        Scribe_Values.Look(ref OutSignalAccepted, nameof(OutSignalAccepted));
    }

    public override void Cleanup()
    {
        base.Cleanup();
        OutSignalAccepted = null;
        OutSignalRejected = null;
    }

    protected override void PostGenerateLetter(ChoiceLetter choiceLetter, out bool letterValid)
    {
        base.PostGenerateLetter(choiceLetter, out letterValid);
        if (choiceLetter is ChoiceLetter_InDistressKnightStart startLetter)
        {
            startLetter.RelatedOrder = RelatedOrder;
            startLetter.OutSignalAccepted = OutSignalAccepted;
            startLetter.OutSignalRejected = OutSignalRejected;
            startLetter.quest = quest;
            startLetter.StartTimeout(30000);
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (RelatedOrder == ratkinOrder)
        {
            RelatedOrder = null;
        }
    }
}