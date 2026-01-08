using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Alert_ResidentKnightWillResignation : Alert_Critical
{
    protected override Color BGColor => ResidentKnightsManager.Instance.MinResignationDays.Value < 5f ? BGColor : Color.clear;

    public Alert_ResidentKnightWillResignation()
    {
        defaultLabel = "OARO_SomeResidentKnightWillResignation".Translate();
    }

    public override AlertReport GetReport()
    {
        float minResignationDays = ResidentKnightsManager.Instance.MinResignationDays.Value;
        return minResignationDays >= 0f && minResignationDays <= 15f;
    }

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