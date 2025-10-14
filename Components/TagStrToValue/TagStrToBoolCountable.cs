using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class TagStrToBoolCountable : IExposable
{
    private Dictionary<string, short> tagStrCount = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTag(string tag) => tagStrCount.ContainsKey(tag);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short GetTagCount(string tag)
    {
        if (tagStrCount.TryGetValue(tag, out short tagCount))
        {
            return tagCount;
        }
        return 0;
    }

    public void IncrementTagValue(string tag, bool addIfMiss)
    {
        if (tagStrCount.TryGetValue(tag, out short count))
        {
            tagStrCount[tag] = ++count;
        }
        else if (addIfMiss)
        {
            tagStrCount.Add(tag, value: 1);
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
        if (tagStrCount.TryGetValue(tag, out short count))
        {
            count--;
            if (count <= 0)
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

    public string GetDetailString()
    {
        if (tagStrCount.NullOrEmpty())
        {
            return "None";
        }

        StringBuilder sb = new();
        foreach (KeyValuePair<string, short> kv in tagStrCount)
        {
            sb.AppendWithSeparator($"({kv.Key}, {kv.Value})", "  ");
        }
        return sb.ToString();
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