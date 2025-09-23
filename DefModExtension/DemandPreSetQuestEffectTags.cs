using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class DemandPreSetQuestEffectTags : DefModExtension
{
    public List<string> fixedTags;
    public List<string> randomTags;
    public IntRange randomTagsSelectCount = IntRange.One;

    public List<string> GetEffectTags()
    {
        List<string> tags = [];
        if (!fixedTags.NullOrEmpty())
        {
            tags.AddRange(fixedTags);
        }
        if (!randomTags.NullOrEmpty())
        {
            int selCount = Mathf.Min(randomTagsSelectCount.RandomInRange, randomTags.Count);
            if (selCount > 0)
            {
                List<string> selTags = randomTags.TakeRandomDistinct(selCount);
                tags.AddRange(selTags);
            }
        }
        return tags;
    }
}