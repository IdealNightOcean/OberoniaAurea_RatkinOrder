using OberoniaAurea_Frame;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;



public class FundHandler : IExposable, ITickDay, IPostLoadInit, IDrawDevWindow
{
    [Unsaved] public readonly RatkinOrder RatkinOrder;

    private float funds;
    public float Funds => funds;

    private float preDayFunds;

    private float expectedChange;
    public float ExpectedChange => expectedChange;

    private bool hasFortune;
    private bool hasRestoration;

    private int newOrderProtectDaysLeft = 180;

    private List<OrderFundEvent> fundEvents = [];
    public IReadOnlyList<OrderFundEvent> FundEvents => fundEvents;

    public FundHandler(RatkinOrder ratkinOrder, bool initConstruct)
    {
        RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        if (initConstruct)
        {
            funds = Rand.Range(40f, 60f);
            AddFundEvent(OrderFundEventDefOf.OARO_NewOrderSubsidy);
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref funds, "funds", 0f);
        Scribe_Values.Look(ref preDayFunds, "preDayFunds", 0f);
        Scribe_Values.Look(ref expectedChange, "expectedChange", 0f);
        Scribe_Values.Look(ref hasFortune, "hasFortune", defaultValue: false);
        Scribe_Values.Look(ref hasRestoration, "hasRestoration", defaultValue: false);

        Scribe_Values.Look(ref newOrderProtectDaysLeft, "newOrderProtectDaysLeft", 0);

        Scribe_Collections.Look(ref fundEvents, "fundEvents", LookMode.Deep);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"Funds: {funds:F2}");
        listing_Rect.Label($"PreDayFunds: {preDayFunds:F2}");
        listing_Rect.Label($"expectedChange: {expectedChange:F2}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"NewOrderProtectDaysLeft: {newOrderProtectDaysLeft}");
        listing_Rect.Label($"HasFortune: {hasFortune}");
        listing_Rect.Label($"HasRestoration: {hasRestoration}");
        listing_Rect.Gap(6f);
        if (listing_Rect.ButtonText("Fund Events", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(GetFundEventsDetailString()));
        }
    }

    public void TickDay()
    {
        funds = Mathf.Clamp(funds + expectedChange, 0f, 100f);

        preDayFunds = funds;
        RecalculateExpectedChange();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdjustFundsImmediately(float change)
    {
        funds = Mathf.Clamp01(funds + change);
    }

    public void AddFundEvent(OrderFundEventDef def)
    {
        if (def.immediately)
        {
            AdjustFundsImmediately(def.changeRange.RandomInRange);
        }
        else
        {
            if (def.OnceEvent)
            {
                expectedChange += def.changeRange.RandomInRange;
            }
            else
            {
                OrderFundEvent fundEvent = new(def);
                expectedChange += fundEvent.TodayChange;
                fundEvent.DayPassed();
                fundEvents.Add(fundEvent);
            }
        }
    }

    public bool HasFundEventsOfDef(OrderFundEventDef def)
    {
        for (int i = 0; i < fundEvents.Count; i++)
        {
            if (fundEvents[i].Def == def)
            {
                return true;
            }
        }
        return false;
    }

    public void RemoveFirstFundEventsOfDef(OrderFundEventDef def)
    {
        int indexToRemove = -1;
        for (int i = 0; i < fundEvents.Count; i++)
        {
            if (fundEvents[i].Def == def)
            {
                indexToRemove = i;
                break;
            }
        }

        if (indexToRemove > 0)
        {
            fundEvents.RemoveAt(indexToRemove);
        }
    }

    public void RemoveAllFundEventsOfDef(OrderFundEventDef def)
    {
        fundEvents.RemoveAll(e => e.Def == def);
    }

    public void DailySpontaneousChange()
    {
        // float tempChange = Rand.Range(-0.75f, 0.75f) + (newOrderProtectDaysLeft--) > 0 ? Rand.Range(0.1f, 0.5f) : 0f;
        AddFundEvent(OrderFundEventDefOf.OARO_FundDailyChange);

        if (hasFortune)
        {
            hasFortune = RatkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.FundFortune);
        }
        else if (Rand.Chance(0.1f))
        {
            RatkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.FundFortune, cdTicks: 15 * 60000, shouldRemoveWhenExpired: true);
            OrderFundEventDef fortuneEventDef = Rand.Bool ? OrderFundEventDefOf.OARO_FundFortune_Positive : OrderFundEventDefOf.OARO_FundFortune_Negative;
            hasFortune = true;
            AddFundEvent(fortuneEventDef);
        }

        if (hasRestoration)
        {
            if (funds > 0.4f && funds < 0.6f)
            {
                RemoveAllFundEventsOfDef(OrderFundEventDefOf.OARO_FundRestoration_Positive);
                RemoveAllFundEventsOfDef(OrderFundEventDefOf.OARO_FundFortune_Negative);
                hasRestoration = false;
            }
        }
        else
        {
            if (funds < 0.2f)
            {
                hasRestoration = true;
                AddFundEvent(OrderFundEventDefOf.OARO_FundRestoration_Positive);
            }
            else if (funds > 0.8f)
            {
                hasRestoration = true;
                AddFundEvent(OrderFundEventDefOf.OARO_FundFortune_Negative);
            }
        }
    }

    public void RecalculateExpectedChange()
    {
        expectedChange = 0f;
        int newIndex = 0;
        for (int i = 0; i < fundEvents.Count; i++)
        {
            OrderFundEvent fundEvent = fundEvents[i];
            fundEvent.DayPassed();
            expectedChange += expectedChange;

            if (fundEvent.DaysLeft > 0)
            {
                if (i != newIndex)
                {
                    fundEvents[newIndex] = fundEvents[i];
                }
                newIndex++;
            }
        }

        fundEvents.RemoveRange(newIndex, fundEvents.Count - newIndex);
    }

    public void PostLoadInit()
    {
        fundEvents.RemoveAll(e => e.DaysLeft <= 0);
        newOrderProtectDaysLeft = Mathf.Max(newOrderProtectDaysLeft, 0);
    }

    private string GetFundEventsDetailString()
    {
        if (fundEvents.NullOrEmpty())
        {
            return "None";
        }

        StringBuilder sb = new();
        for (int i = 0; i < fundEvents.Count; i++)
        {
            sb.AppendInNewLine($"{i}. {fundEvents[i]}");
        }
        return sb.ToString();
    }
}
