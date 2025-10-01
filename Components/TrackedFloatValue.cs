using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct SimpleFloatChangeRecord : IExposable
{
    public float change;
    public string explain;

    public SimpleFloatChangeRecord() { }
    public SimpleFloatChangeRecord(float change, string explain)
    {
        this.change = change;
        this.explain = explain;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref change, "change", 0f);
        Scribe_Values.Look(ref explain, "explain", "UNKOWN");
    }
}

public class TrackedFloatValue : IExposable
{
    private float minValue = float.MinValue;
    private float maxValue = float.MaxValue;

    private float curValue;

    private float curRecordChange;
    private List<SimpleFloatChangeRecord> trackedChanges = [];

    public float CurValue => curValue;

    public TrackedFloatValue(float initValue)
    {
        curValue = initValue;
    }

    public TrackedFloatValue(float minValue, float maxValue)
    {
        this.minValue = minValue;
        this.maxValue = maxValue;
    }

    public TrackedFloatValue(float initValue, float minValue, float maxValue) : this(minValue, maxValue)
    {
        curValue = initValue;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref minValue, "minValue", float.MinValue);
        Scribe_Values.Look(ref maxValue, "maxValue", float.MaxValue);

        Scribe_Values.Look(ref curValue, "chacurValuenge", 0f);
        Scribe_Values.Look(ref curRecordChange, "curRecordChange", 0f);
        Scribe_Collections.Look(ref trackedChanges, "trackedChanges", LookMode.Deep);
    }

    public void AdjustValue(float change, string explain)
    {
        float trueChange = curValue;
        curValue = Mathf.Clamp(curValue + change, minValue, maxValue);
        trueChange = curValue - trueChange;
        curRecordChange += trueChange;
        trackedChanges.Add(new SimpleFloatChangeRecord(trueChange, explain));
    }

    public void SetValue(float newValue, string explain)
    {
        float trueChange = curValue;
        curValue = Mathf.Clamp(newValue, minValue, maxValue);
        trueChange = curValue - trueChange;
        curRecordChange += trueChange;
        trackedChanges.Add(new SimpleFloatChangeRecord(trueChange, explain));
    }

    public void SetValueDirectly(float curValue) => this.curValue = curValue;

    public void ClearTrace()
    {
        curRecordChange = 0f;
        trackedChanges.Clear();
    }

    public void Reset()
    {
        curValue = 0f;
        ClearTrace();
    }
}