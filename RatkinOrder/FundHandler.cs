using OberoniaAurea_Frame;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class FundHandler(RatkinOrder ratkinOrder) : IExposable
{
    [Unsaved] public readonly RatkinOrder RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));

    private float funds;
    public float Funds => funds;

    private float preDayFunds;

    private float immediatelyChange;
    private float expectedChange;
    public float ImmediatelyChange => immediatelyChange;
    public float ExpectedChange => expectedChange;

    private Dictionary<string, float> immediatelyChangeExplanation = [];
    private Dictionary<string, float> expectedChangeExplanation = [];

    private bool hasFortune;
    private bool hasRestoration;

    private List<OrderFundEvent> fundEvents = [];
    public IReadOnlyList<OrderFundEvent> FundEvents => fundEvents;

    public void PostOrderGenerated()
    {
        funds = Rand.Range(0.4f, 0.6f);
        AddFundEvent(OrderFundEventDefOf.OARO_NewOrderSubsidy);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref funds, "funds", 0f);
        Scribe_Values.Look(ref preDayFunds, "preDayFunds", 0f);

        Scribe_Values.Look(ref hasFortune, "hasFortune", defaultValue: false);
        Scribe_Values.Look(ref hasRestoration, "hasRestoration", defaultValue: false);

        Scribe_Values.Look(ref immediatelyChange, "immediatelyChange", 0f);
        Scribe_Values.Look(ref expectedChange, "expectedChange", 0f);
        Scribe_Collections.Look(ref immediatelyChangeExplanation, "immediatelyChangeExplanation", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref expectedChangeExplanation, "expectedChangeExplanation", LookMode.Value, LookMode.Value);

        Scribe_Collections.Look(ref fundEvents, "fundEvents", LookMode.Deep);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"Funds: {funds:F2}");
        listing_Rect.Label($"PreDayFunds: {preDayFunds:F2}");
        listing_Rect.Label($"expectedChange: {expectedChange:F2}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"HasFortune: {hasFortune}");
        listing_Rect.Label($"HasRestoration: {hasRestoration}");
        listing_Rect.Gap(6f);
        if (listing_Rect.ButtonText("Fund Events", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(GetFundEventsDetailString()));
        }
        listing_Rect.Gap(6f);
        if (listing_Rect.ButtonText("Fund Change Detail", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(GetFundChangeDetail()));
        }
    }

    internal void DailySettlement()
    {
        funds = Mathf.Clamp01(funds + expectedChange);

        preDayFunds = funds;

        immediatelyChange = 0f;
        immediatelyChangeExplanation.Clear();

        RecalculateExpectedChange();
        DailySpontaneousChange();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdjustFundsImmediately(float change, string reason = null)
    {
        funds = Mathf.Clamp01(funds + change);
        immediatelyChange += change;
        if (reason.NullOrEmpty())
        {
            reason = "OARO_Fund_Misc".Translate();
        }

        if (immediatelyChangeExplanation.TryGetValue(reason, out float curChange))
        {
            immediatelyChangeExplanation[reason] = curChange + change;
        }
        else
        {
            immediatelyChangeExplanation[reason] = change;
        }
    }

    private void AdjustFundsExpected(float change, string reason = null)
    {
        expectedChange += change;
        if (reason.NullOrEmpty())
        {
            reason = "OARO_Fund_Misc".Translate();
        }
        if (expectedChangeExplanation.TryGetValue(reason, out float curChange))
        {
            expectedChangeExplanation[reason] = curChange + change;
        }
        else
        {
            expectedChangeExplanation[reason] = change;
        }
    }

    public void AddFundEvent(OrderFundEventDef def)
    {
        if (def.immediately)
        {
            AdjustFundsImmediately(def.changeRange.RandomInRange, def.label);
        }
        else
        {
            if (def.OnceEvent)
            {
                AdjustFundsExpected(def.changeRange.RandomInRange, def.label);
            }
            else
            {
                OrderFundEvent fundEvent = new(def);
                AdjustFundsExpected(fundEvent.TodayChange, def.label);
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

    private void DailySpontaneousChange()
    {
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
                RemoveAllFundEventsOfDef(OrderFundEventDefOf.OARO_FundRestoration_Negative);
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
                AddFundEvent(OrderFundEventDefOf.OARO_FundRestoration_Negative);
            }
        }
    }

    private void RecalculateExpectedChange()
    {
        expectedChange = 0f;
        expectedChangeExplanation.Clear();
        int newIndex = 0;
        for (int i = 0; i < fundEvents.Count; i++)
        {
            OrderFundEvent fundEvent = fundEvents[i];
            fundEvent.DayPassed();
            AdjustFundsExpected(fundEvent.TodayChange, fundEvent.Def?.label);

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

    internal void PostLoadInit()
    {
        fundEvents.RemoveAll(e => e.DaysLeft <= 0);
    }

    public string GetFundChangeDetail()
    {
        StringBuilder sb = new();
        sb.AppendLine("OARO_Fund_ImmediatelyChange".Translate(immediatelyChange.ToStringWithSign("F2")).Colorize(immediatelyChange < 0f ? ColorLibrary.RedReadable : Color.green));
        sb.AppendLine("----");
        if (immediatelyChangeExplanation.Count > 0)
        {
            foreach (KeyValuePair<string, float> kv in immediatelyChangeExplanation)
            {
                sb.AppendLine($"{kv.Key}: {kv.Value.ToStringWithSign("F2")}".Colorize(kv.Value < 0f ? ColorLibrary.RedReadable : Color.green));
            }
        }

        sb.AppendLine();
        sb.AppendLine("-*-*-*-*-*-");
        sb.AppendLine();

        sb.AppendLine("OARO_Fund_ExpectedChange".Translate(expectedChange.ToStringWithSign("F2")).Colorize(expectedChange < 0f ? ColorLibrary.RedReadable : Color.green));
        sb.AppendLine("----");
        if (expectedChangeExplanation.Count > 0)
        {
            foreach (KeyValuePair<string, float> kv in expectedChangeExplanation)
            {
                sb.AppendLine($"{kv.Key}: {kv.Value.ToStringWithSign("F2")}".Colorize(kv.Value < 0f ? ColorLibrary.RedReadable : Color.green));
            }
        }

        return sb.ToString();
    }

    public string GetFundEventsDetailString()
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
