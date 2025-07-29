using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_EsteemChangeAllOrders : QuestNode
{
    public SlateRef<string> inSignal;
    public SlateRef<float> esteemChange;
    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        if (esteemChange.GetValue(slate) != 0f)
        {
            QuestPart_EsteemChangeAllOrders questPart_EsteemChangeAll = new()
            {
                inSignal = inSignal.GetValue(slate) ?? QuestGen.slate.Get<string>("inSignal"),
                esteemChange = esteemChange.GetValue(slate)
            };

            QuestGen.quest.AddPart(questPart_EsteemChangeAll);
        }
    }
}

public class QuestPart_EsteemChangeAllOrders : QuestPart
{
    public string inSignal;
    public float esteemChange;

    public QuestPart_EsteemChangeAllOrders() { }
    public QuestPart_EsteemChangeAllOrders(string inSignal, float esteemChange)
    {
        this.inSignal = inSignal;
        this.esteemChange = esteemChange;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag == inSignal)
        {
            RatkinOrderManager.Instance.AllRatkinOrders.ForEach(o => { o.EsteemHandler.Esteem += esteemChange; });
        }
    }
    public override void Cleanup()
    {
        base.Cleanup();
        inSignal = string.Empty;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, "inSignal");
        Scribe_Values.Look(ref esteemChange, "esteemChange", 0f);
    }
}
