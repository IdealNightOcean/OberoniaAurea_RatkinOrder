using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_RatkinOrderRelationshipChange : QuestNode
{
    [NoTranslate]
    public SlateRef<string> inSignal;

    public SlateRef<RatkinOrder> ratkinOrder;
    public SlateRef<int> offset;
    public SlateRef<bool> sendLetter;
    [MustTranslate]
    public SlateRef<string> reason;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_RatkinOrderRelationshipChange questPart_RatkinOrderRelationshipChange = new()
        {
            InSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>(OARO_KeyLibrary_SlateStoreAs.inSignal),
            RatkinOrder = ratkinOrder.GetValue(slate) ?? slate.Get<RatkinOrder>(OARO_KeyLibrary_SlateStoreAs.ratkinOrder),
            Offset = offset.GetValue(slate),
            SendLetter = sendLetter.GetValue(slate)
        };
        QuestGen.quest.AddPart(questPart_RatkinOrderRelationshipChange);
    }
}

public class QuestPart_RatkinOrderRelationshipChange : QuestPart, IOnRatkinOrderRemoved
{
    public string InSignal;
    public RatkinOrder RatkinOrder;
    public int Offset;
    public bool SendLetter = true;
    public string Reason;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignal, nameof(InSignal));
        Scribe_References.Look(ref RatkinOrder, nameof(RatkinOrder));
        Scribe_Values.Look(ref Offset, nameof(Offset), 0);
        Scribe_Values.Look(ref SendLetter, nameof(SendLetter), defaultValue: true);
        Scribe_Values.Look(ref Reason, nameof(Reason));
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignal = null;
        RatkinOrder = null;
        Offset = 0;
        SendLetter = true;
        Reason = null;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (RatkinOrder == ratkinOrder)
        {
            RatkinOrder = null;
        }
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == InSignal)
        {
            RatkinOrder?.RelationshipKindOffsetBy(Offset, Reason, SendLetter);
        }
    }
}