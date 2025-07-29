using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class TagStrToInt : IExposable, ITagStrToValue<int>
{
    private readonly int DefaultValue;
    public readonly bool RemoveWhenDefault; //是否在值为默认值时移除标签

    private Dictionary<string, int> tagStrToInt = [];

    public TagStrToInt(int defaultValue, bool removeWhenDefault)
    {
        this.DefaultValue = defaultValue;
        this.RemoveWhenDefault = removeWhenDefault;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTag(string tag)
    {
        return tagStrToInt.ContainsKey(tag);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetTagValue(string tag, out int value)
    {
        return tagStrToInt.TryGetValue(tag, out value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTagValue(string tag, int value, bool addIfMiss)
    {
        if (value == DefaultValue && RemoveWhenDefault)
        {
            tagStrToInt.Remove(tag);
        }
        else if (addIfMiss || tagStrToInt.ContainsKey(tag))
        {
            tagStrToInt[tag] = value;
        }
    }

    public void OffsetTagValueBy(string tag, int offset, bool addIfMiss)
    {
        if (tagStrToInt.TryGetValue(tag, out int newValue))
        {
            newValue += offset;
            if (newValue == DefaultValue && RemoveWhenDefault)
            {
                tagStrToInt.Remove(tag);
            }
            else
            {
                tagStrToInt[tag] = newValue;

            }
        }
        else if (addIfMiss)
        {
            newValue = DefaultValue + offset;
            if (newValue == DefaultValue && RemoveWhenDefault)
            {
                return;
            }
            tagStrToInt[tag] = newValue;
        }
    }

    public void ScaleTagValueBy(string tag, int scale, bool addIfMiss)
    {
        if (tagStrToInt.TryGetValue(tag, out int newValue))
        {
            newValue *= scale;
            if (newValue == DefaultValue && RemoveWhenDefault)
            {
                tagStrToInt.Remove(tag);
            }
            else
            {
                tagStrToInt[tag] = newValue;

            }
        }
        else if (addIfMiss)
        {
            newValue = DefaultValue * scale;
            if (newValue == DefaultValue && RemoveWhenDefault)
            {
                return;
            }
            tagStrToInt[tag] = newValue;
        }
    }

    public void ModifyTagValueBy(string tag, Func<int, int> modifier, bool addIfMiss)
    {
        if (tagStrToInt.TryGetValue(tag, out int newValue))
        {
            newValue = modifier(newValue);
            if (newValue == DefaultValue && RemoveWhenDefault)
            {
                tagStrToInt.Remove(tag);
            }
            else
            {
                tagStrToInt[tag] = newValue;

            }
        }
        else if (addIfMiss)
        {
            newValue = modifier(DefaultValue);
            if (newValue == DefaultValue && RemoveWhenDefault)
            {
                return;
            }
            tagStrToInt[tag] = newValue;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveTag(string tag)
    {
        tagStrToInt.Remove(tag);
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref tagStrToInt, "tagStrToInt", LookMode.Value, LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            tagStrToInt ??= [];
            tagStrToInt.RemoveAll(kv => kv.Key.NullOrEmpty());
        }
    }
}
