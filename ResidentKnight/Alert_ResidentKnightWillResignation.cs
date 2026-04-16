using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Alert_ResidentKnightWillResignation : Alert_Critical
{
    private static readonly List<Pawn> knightsApproachingResignation = new(4);
    private static int nextUpdateTick = -1;
    private static List<Pawn> KnightsApproachingResignation
    {
        get
        {
            if (Find.TickManager.TicksGame > nextUpdateTick)
            {
                RefreshKnightsApproachingResignation();
            }
            return knightsApproachingResignation;
        }
    }

    protected override Color BGColor => KnightsApproachingResignation.Count > 0 ? BGColor : Color.clear;

    public Alert_ResidentKnightWillResignation()
    {
        defaultLabel = "OARO_SomeResidentKnightWillResignation".Translate();
    }

    public static void ClearStaticCache()
    {
        knightsApproachingResignation.Clear();
        nextUpdateTick = -1;
    }

    public static void MarkDirty() => nextUpdateTick = -1;

    public override AlertReport GetReport()
    {
        AlertReport alertReport = new()
        {
            active = KnightsApproachingResignation.Count > 0,
            culpritsPawns = KnightsApproachingResignation
        };

        return alertReport;
    }

    protected override void OnClick()
    {
        if (OrderStationHandler.Instance.MainOrderCodePedestal?.Map is null)
        {
            return;
        }
        Window_OrderHall hallWin = new(OrderStationHandler.Instance.MainOrderCodePedestal.Map);
        Find.WindowStack.Add(hallWin);
    }

    public override TaggedString GetExplanation()
    {
        TaggedString explanation = "OARO_SomeResidentKnightWillResignationDesc".Translate(GenLabel.ThingsLabel(KnightsApproachingResignation).Named(KeyLibrary_FormatArgName.PawnsInfo));
        if (OrderStationHandler.Instance.MainOrderCodePedestal?.Map is not null)
        {
            explanation += ("\n\n(" + "OARO_ClickToOpenOrderHallWin".Translate() + ")");
        }
        return explanation;
    }

    private static void RefreshKnightsApproachingResignation()
    {
        nextUpdateTick = Find.TickManager.TicksGame + 10000;
        knightsApproachingResignation.Clear();

        IReadOnlyList<ResidentKnight> residentKnights = ResidentPawnsManager.Instance.ResidentKnights;
        if (residentKnights.Count <= 0)
            return;

        int ticksGame = Find.TickManager.TicksGame;
        foreach (ResidentKnight record in residentKnights)
        {
            if (record.ResignationTick > 0)
            {
                float resignationDays = Mathf.Max(0f, (record.ResignationTick - ticksGame) / 60000f);
                if (resignationDays < 15f)
                {
                    knightsApproachingResignation.Add(record.Pawn);
                }
            }
        }
    }
}