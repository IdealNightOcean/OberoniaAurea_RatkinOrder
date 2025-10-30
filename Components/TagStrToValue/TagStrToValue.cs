using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class TagStrToValue<T> : IExposable where T : struct, IEquatable<T>
{
    protected T defaultValue;
    protected bool removeWhenDefault; //是否在值为默认值时移除标签
    private LookMode valueLookMode = LookMode.Value;

    protected Dictionary<string, T> tagStrToValue = [];

    public TagStrToValue() { }

    public TagStrToValue(T defaultValue, bool removeWhenDefault, LookMode valueLookMode)
    {
        this.defaultValue = defaultValue;
        this.removeWhenDefault = removeWhenDefault;
        this.valueLookMode = valueLookMode;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTag(string tag)
    {
        return tagStrToValue.ContainsKey(tag);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetTagValue(string tag, out T value)
    {
        return tagStrToValue.TryGetValue(tag, out value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTagValue(string tag, T value, bool addIfMiss)
    {
        if (value.Equals(defaultValue) && removeWhenDefault)
        {
            tagStrToValue.Remove(tag);
        }
        else if (addIfMiss || tagStrToValue.ContainsKey(tag))
        {
            tagStrToValue[tag] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveTag(string tag)
    {
        tagStrToValue.Remove(tag);
    }

    public abstract void OffsetTagValueBy(string tag, T offset, bool addIfMiss);
    public abstract void ScaleTagValueBy(string tag, T scale, bool addIfMiss);

    public void ModifyTagValueBy(string tag, Func<T, T> modifier, bool addIfMiss)
    {
        if (tagStrToValue.TryGetValue(tag, out T newValue))
        {
            newValue = modifier(newValue);
            if (newValue.Equals(defaultValue) && removeWhenDefault)
            {
                tagStrToValue.Remove(tag);
            }
            else
            {
                tagStrToValue[tag] = newValue;

            }
        }
        else if (addIfMiss)
        {
            newValue = modifier(defaultValue);
            if (newValue.Equals(defaultValue) && removeWhenDefault)
            {
                return;
            }
            tagStrToValue[tag] = newValue;
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref valueLookMode, "valueLookMode", defaultValue: LookMode.Value);

        Scribe_Collections.Look(ref tagStrToValue, "tagStrToValue", LookMode.Value, valueLookMode);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            tagStrToValue ??= [];
            tagStrToValue.RemoveAll(kv => string.IsNullOrEmpty(kv.Key));
        }
    }
}
