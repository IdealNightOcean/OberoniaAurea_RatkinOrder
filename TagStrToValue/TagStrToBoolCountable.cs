using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class TagStrToBoolCountable : IExposable
{
    private const short DefaultValue = 0;
    private const short MissingValue = -9999; //表示标签不存在的值

    private Dictionary<string, short> tagStrCount;

    public TagStrToBoolCountable()
    {
        tagStrCount = [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTag(string tag)
    {
        return tagStrCount.ContainsKey(tag);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasActiveTag(string tag)
    {
        return tagStrCount.TryGetValue(tag, fallback: MissingValue) > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetTagCount(string tag, out short tagCount)
    {
        return tagStrCount.TryGetValue(tag, out tagCount);
    }

    public void IncrementTagValue(string tag, bool addIfMiss)
    {
        short count = tagStrCount.TryGetValue(tag, fallback: MissingValue);

        if (count == MissingValue)
        {
            if (addIfMiss)
            {
                tagStrCount.Add(tag, value: 1);
            }
        }
        else
        {
            if (++count == DefaultValue)
            {
                tagStrCount.Remove(tag);
            }
            else
            {
                tagStrCount[tag] = count;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementTagsValue(IEnumerable<string> tags, bool addIfMiss)
    {
        if (tags is null)
        {
            return;
        }

        foreach (string tag in tags)
        {
            IncrementTagValue(tag, addIfMiss);
        }
    }

    public void DecrementTagValue(string tag)
    {
        short count = tagStrCount.TryGetValue(tag, fallback: MissingValue);

        if (count != MissingValue)
        {
            if (--count == DefaultValue)
            {
                tagStrCount.Remove(tag);
            }
            else
            {
                tagStrCount[tag] = count;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DecrementTagsValue(IEnumerable<string> tags)
    {
        if (tags is null)
        {
            return;
        }

        foreach (string tag in tags)
        {
            DecrementTagValue(tag);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveTag(string tag)
    {
        tagStrCount.Remove(tag);
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref tagStrCount, "tagStrCount", LookMode.Value, LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            tagStrCount ??= [];
            tagStrCount.RemoveAll(kv => kv.Key.NullOrEmpty() || kv.Value == 0);
        }
    }
}