using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class TagStrToFloat : IExposable, ITagStrToValue<float>
{
    private readonly float DefaultValue;
    public readonly bool RemoveWhenDefault; //是否在值为默认值时移除标签

    private Dictionary<string, float> tagStrToFloat = [];

    public TagStrToFloat(float defaultValue, bool removeWhenDefault)
    {
        this.DefaultValue = defaultValue;
        this.RemoveWhenDefault = removeWhenDefault;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTag(string tag)
    {
        return tagStrToFloat.ContainsKey(tag);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetTagValue(string tag, out float value)
    {
        return tagStrToFloat.TryGetValue(tag, out value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTagValue(string tag, float value, bool addIfMiss)
    {
        if (value == DefaultValue && RemoveWhenDefault)
        {
            tagStrToFloat.Remove(tag);
        }
        else if (addIfMiss || tagStrToFloat.ContainsKey(tag))
        {
            tagStrToFloat[tag] = value;
        }
    }

    public void OffsetTagValueBy(string tag, float offset, bool addIfMiss)
    {
        if (tagStrToFloat.TryGetValue(tag, out float newValue))
        {
            newValue += offset;
            if (newValue == DefaultValue && RemoveWhenDefault)
            {
                tagStrToFloat.Remove(tag);
            }
            else
            {
                tagStrToFloat[tag] = newValue;

            }
        }
        else if (addIfMiss)
        {
            newValue = DefaultValue + offset;
            if (newValue == DefaultValue && RemoveWhenDefault)
            {
                return;
            }
            tagStrToFloat[tag] = newValue;
        }
    }

    public void ScaleTagValueBy(string tag, float scale, bool addIfMiss)
    {
        if (tagStrToFloat.TryGetValue(tag, out float newValue))
        {
            newValue *= scale;
            if (newValue == DefaultValue && RemoveWhenDefault)
            {
                tagStrToFloat.Remove(tag);
            }
            else
            {
                tagStrToFloat[tag] = newValue;

            }
        }
        else if (addIfMiss)
        {
            newValue = DefaultValue * scale;
            if (newValue == DefaultValue && RemoveWhenDefault)
            {
                return;
            }
            tagStrToFloat[tag] = newValue;
        }
    }

    public void ModifyTagValueBy(string tag, Func<float, float> modifier, bool addIfMiss)
    {
        if (tagStrToFloat.TryGetValue(tag, out float newValue))
        {
            newValue = modifier(newValue);
            if (newValue == DefaultValue && RemoveWhenDefault)
            {
                tagStrToFloat.Remove(tag);
            }
            else
            {
                tagStrToFloat[tag] = newValue;

            }
        }
        else if (addIfMiss)
        {
            newValue = modifier(DefaultValue);
            if (newValue == DefaultValue && RemoveWhenDefault)
            {
                return;
            }
            tagStrToFloat[tag] = newValue;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveTag(string tag)
    {
        tagStrToFloat.Remove(tag);
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref tagStrToFloat, "tagStrToFloat", LookMode.Value, LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            tagStrToFloat ??= [];
            tagStrToFloat.RemoveAll(kv => kv.Key.NullOrEmpty());
        }
    }
}
