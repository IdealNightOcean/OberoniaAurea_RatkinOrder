using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.FundEvent;

namespace OberoniaAurea.RatkinOrder;

public struct FundEvent : IExposable
{
    public enum FundEvenType
    {
        Misc,
        Fortune,
        Restoration,
        Sponsor,
        Quest,
        OrderEvent
    }

    public float change;
    public int daysLeft;
    public FundEvenType type;

    public FundEvent()
    {
        change = 0;
        daysLeft = 1;
        type = FundEvenType.Misc;
    }

    public FundEvent(float change, int durationDays, FundEvenType type)
    {
        this.change = change;
        this.daysLeft = durationDays;
        this.type = type;
    }

    public void DayPassed()
    {
        daysLeft--;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref change, "change", 0f);
        Scribe_Values.Look(ref daysLeft, "daysLeft", 1);
        Scribe_Values.Look(ref type, "daysLtypeeft", FundEvenType.Misc);
    }
}

public class FundHandler : IExposable, ITickDay, IPostLoadInit
{
    [Unsaved] public readonly RatkinOrder RatkinOrder;

    private float funds;
    public float Funds => funds;

    private float preDayFunds;

    private float expectedChange;
    public float ExpectedChange => expectedChange;

    private int newOrderProtectDaysLeft = 180;
    private int fortuneDaysLeft;
    private int restorationDaysLeft;
    private bool HasFortune => fortuneDaysLeft > 0;
    private bool HasRestoration => restorationDaysLeft > 0;

    public List<FundEvent> fundEvents = [];

    public FundHandler(RatkinOrder ratkinOrder)
    {
        this.RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        funds = Rand.Range(0.4f, 0.6f);
    }

    public void TickDay()
    {
        funds = Mathf.Clamp01(funds + expectedChange);

        preDayFunds = funds;
        RecalculateExpectedChange();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdjustFundsImmediately(float change)
    {
        funds = Mathf.Clamp01(funds + change);
    }

    public void AddFundEvent(float change, int durationDays, FundEvenType type)
    {
        if (change == 0f || durationDays <= 0)
        {
            return;
        }
        expectedChange += change;
        durationDays--;

        if (durationDays > 0)
        {
            fundEvents.Add(new FundEvent(change, durationDays, type));
        }
    }

    public void RemoveAllFundEventsOfType(FundEvenType type)
    {
        fundEvents.RemoveAll(e => e.type == type);
    }

    public void DailySpontaneousChange()
    {
        float tempChange = Rand.Range(-0.0075f, 0.0075f) + (newOrderProtectDaysLeft--) > 0 ? Rand.Range(0.001f, 0.005f) : 0f;
        AddFundEvent(tempChange, 1, FundEvenType.Misc);

        if (HasFortune)
        {
            fortuneDaysLeft--;
        }
        else if (Rand.Chance(0.1f))
        {
            tempChange = Rand.Bool ? 0.0075f : -0.0075f;
            fortuneDaysLeft = 15;
            AddFundEvent(0.005f, fortuneDaysLeft, FundEvenType.Fortune);
            fortuneDaysLeft--;
        }

        if (HasRestoration)
        {
            restorationDaysLeft--;
            if (funds > 0.4f && funds < 0.6f)
            {
                restorationDaysLeft = -1;
                RemoveAllFundEventsOfType(FundEvenType.Restoration);
            }
        }
        else
        {
            if (funds < 0.2f)
            {
                restorationDaysLeft = 20;
                AddFundEvent(0.005f, restorationDaysLeft, FundEvenType.Restoration);
                restorationDaysLeft--;
            }
            else if (funds > 0.8f)
            {
                restorationDaysLeft = 20;
                AddFundEvent(-0.005f, restorationDaysLeft, FundEvenType.Restoration);
                restorationDaysLeft--;
            }
        }
    }

    public void RecalculateExpectedChange()
    {
        expectedChange = 0f;

        FundEvent tmpEvent;
        for (int i = fundEvents.Count - 1; i >= 0; i--)
        {
            tmpEvent = fundEvents[i];

            expectedChange += tmpEvent.change;
            tmpEvent.daysLeft--;
            fundEvents[i] = tmpEvent;
        }
        fundEvents.RemoveAll(e => e.daysLeft <= 0);
    }


    public void PostLoadInit()
    {
        fundEvents.RemoveAll(e => e.daysLeft <= 0);
        newOrderProtectDaysLeft = Mathf.Max(newOrderProtectDaysLeft, 0);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref funds, "funds", 0f);
        Scribe_Values.Look(ref preDayFunds, "preDayFunds", 0f);
        Scribe_Values.Look(ref expectedChange, "expectedChange", 0f);
    }
}
