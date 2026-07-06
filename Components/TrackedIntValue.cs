using OberoniaAurea_Frame;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct SimpleIntChangeRecord : IExposable
{
    public int change;
    public string explain;

    public SimpleIntChangeRecord() { }
    public SimpleIntChangeRecord(int change, string explain)
    {
        this.change = change;
        this.explain = explain;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref change, nameof(change), 0);
        Scribe_Values.Look(ref explain, nameof(explain), KeyLibrary_Misc.ErrorTip);
    }
}

public class TrackedIntValue : IExposable
{
    private int minValue = int.MinValue;
    private int maxValue = int.MaxValue;

    private int curValue;

    private int curRecordChange;
    private List<SimpleFloatChangeRecord> trackedChanges = [];

    public float CurValue => curValue;

    public TrackedIntValue(int initValue)
    {
        curValue = initValue;
    }

    public TrackedIntValue(int minValue, int maxValue)
    {
        this.minValue = minValue;
        this.maxValue = maxValue;
    }

    public TrackedIntValue(int initValue, int minValue, int maxValue) : this(minValue, maxValue)
    {
        curValue = initValue;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref minValue, "minValue", int.MinValue);
        Scribe_Values.Look(ref maxValue, "maxValue", int.MaxValue);

        Scribe_Values.Look(ref curValue, "chacurValuenge", 0);
        Scribe_Values.Look(ref curRecordChange, "curRecordChange", 0);
        Scribe_Collections.Look(ref trackedChanges, "trackedChanges", LookMode.Deep);
    }

    public void AdjustValue(int change, string explain)
    {
        int trueChange = curValue;
        curValue = Mathf.Clamp(curValue + change, minValue, maxValue);
        trueChange = curValue - trueChange;
        curRecordChange += trueChange;
        trackedChanges.Add(new SimpleFloatChangeRecord(trueChange, explain));
    }

    public void SetValue(int newValue, string explain)
    {
        int trueChange = curValue;
        curValue = Mathf.Clamp(newValue, minValue, maxValue);
        trueChange = curValue - trueChange;
        curRecordChange += trueChange;
        trackedChanges.Add(new SimpleFloatChangeRecord(trueChange, explain));
    }

    public void SetValueDirectly(int curValue) => this.curValue = curValue;

    public void ClearTrace()
    {
        curRecordChange = 0;
        trackedChanges.Clear();
    }

    public void Reset()
    {
        curValue = 0;
        ClearTrace();
    }
}