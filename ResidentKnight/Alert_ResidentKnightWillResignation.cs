using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Alert_ResidentKnightWillResignation : Alert_Critical
{
    private List<Pawn> KnightsApproachingResignation => ResidentPawnsManager.CacheManager?.KnightsApproachingResignation.Value;

    protected override Color BGColor => KnightsApproachingResignation.Count > 0 ? BGColor : Color.clear;

    public Alert_ResidentKnightWillResignation()
    {
        defaultLabel = "OARO_Alert_SomeResidentKnightWillResignation".Translate();
    }

    public override AlertReport GetReport()
    {
        AlertReport alertReport = new()
        {
            active = !KnightsApproachingResignation.NullOrEmpty(),
            culpritsPawns = !KnightsApproachingResignation.NullOrEmpty() ? [.. KnightsApproachingResignation] : null
        };

        return alertReport;
    }

    protected override void OnClick()
    {
        if (OrderStationHandler.Instance.MainOrderCodePedestal?.Map is null)
        {
            return;
        }
        Window_OrderStation stationWin = new(OrderStationHandler.Instance.MainOrderCodePedestal.Map);
        Find.WindowStack.Add(stationWin);
    }

    public override TaggedString GetExplanation()
    {
        TaggedString explanation = "OARO_Alert_SomeResidentKnightWillResignationExp".Translate(GenLabel.ThingsLabel(KnightsApproachingResignation).Named(KeyLibrary_FormatArgName.PawnsInfo));
        if (OrderStationHandler.Instance.MainOrderCodePedestal?.Map is not null)
        {
            explanation += ("\n\n(" + "OARO_ClickToOpenOrderStationWin".Translate() + ")");
        }
        return explanation;
    }
}