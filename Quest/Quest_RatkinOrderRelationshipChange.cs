using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_RatkinOrderRelationshipChange : QuestNode
{
    [NoTranslate]
    public SlateRef<string> inSiganl;

    public SlateRef<RatkinOrder> ratkinOrder;
    public SlateRef<int> offset;
    public SlateRef<bool> sendLetter;
    [MustTranslate]
    public SlateRef<string> reason;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_RatkinOrderRelationshipChange questPart_RatkinOrderRelationshipChange = new()
        {
            InSiganl = QuestGenUtility.HardcodedSignalWithQuestID(inSiganl.GetValue(slate)) ?? slate.Get<string>("inSiganl"),
            RatkinOrder = ratkinOrder.GetValue(slate) ?? slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrder),
            Offset = offset.GetValue(slate),
            SendLetter = sendLetter.GetValue(slate)
        };
        QuestGen.quest.AddPart(questPart_RatkinOrderRelationshipChange);
    }
}

public class QuestPart_RatkinOrderRelationshipChange : QuestPart, IOnRatkinOrderRemoved
{
    public string InSiganl;
    public RatkinOrder RatkinOrder;
    public int Offset;
    public bool SendLetter = true;
    public string Reason;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSiganl, "InSiganl");
        Scribe_References.Look(ref RatkinOrder, "RatkinOrder");
        Scribe_Values.Look(ref Offset, "Offset", 0);
        Scribe_Values.Look(ref SendLetter, "SendLetter", defaultValue: true);
        Scribe_Values.Look(ref Reason, "Reason");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSiganl = null;
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
        if (signal.tag == InSiganl)
        {
            RatkinOrder?.RelationshipKindOffsetBy(Offset, Reason, SendLetter);
        }
    }
}