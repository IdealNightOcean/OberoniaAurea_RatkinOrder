using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_EsteemChange : QuestNode
{
    public SlateRef<string> inSignal;
    public SlateRef<RatkinOrder> order;
    public SlateRef<float> esteemChange;

    protected override bool TestRunInt(Slate slate)
    {
        return order.GetValue(slate) is not null;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        if (esteemChange.GetValue(slate) != 0f)
        {
            QuestPart_EsteemChange questPart_EsteemChangeAll = new()
            {
                inSignal = inSignal.GetValue(slate) ?? QuestGen.slate.Get<string>("inSignal"),
                order = order.GetValue(slate),
                esteemChange = esteemChange.GetValue(slate)
            };

            QuestGen.quest.AddPart(questPart_EsteemChangeAll);
        }
    }
}

public class QuestPart_EsteemChange : QuestPart
{
    public string inSignal;
    public RatkinOrder order;
    public float esteemChange;
    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag == inSignal)
        {
            if (order is not null)
            {
                order.EsteemHandler.Esteem += esteemChange;
            }
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        inSignal = string.Empty;
        order = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, "inSignal");
        Scribe_References.Look(ref order, "order");
        Scribe_Values.Look(ref esteemChange, "esteemChange", 0f);
    }
}
