using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Alert_ResidentKnightWillResignation : Alert
{
    public Alert_ResidentKnightWillResignation()
    {
        defaultLabel = "OARO_SomeResidentKnightWillResignation".Translate();
    }

    public override AlertReport GetReport() => ResidentKnightsManager.Instance.ShowResignationAlert.Value;

    protected override void OnClick()
    {
        if (OrderHallHandler.Instance.MainOrderCodePedestal?.Map is null)
        {
            return;
        }
        Window_OrderHall hallWin = new(OrderHallHandler.Instance.MainOrderCodePedestal.Map);
        Find.WindowStack.Add(hallWin);
    }

    public override TaggedString GetExplanation()
    {
        TaggedString explanation = "OARO_SomeResidentKnightWillResignationDesc".Translate();
        if (OrderHallHandler.Instance.MainOrderCodePedestal?.Map is not null)
        {
            explanation += ("\n\n(" + "OARO_ClickToOpenOrderHallWin".Translate() + ")");
        }
        return explanation;
    }
}