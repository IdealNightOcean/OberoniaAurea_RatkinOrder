using OberoniaAurea_Frame;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class DemandPreSetQuestEffectTags : DefModExtension
{
    public List<BranchMedalDef> potentialMedals = [];
    public List<QuestEffectTag> fixedTags;
    public List<QuestEffectTag> randomTags;
    public IntRange randomTagsSelectCount = IntRange.One;

    public List<QuestEffectTag> GetEffectTags()
    {
        List<QuestEffectTag> tags = [];
        if (!fixedTags.NullOrEmpty())
        {
            tags.AddRange(fixedTags);
        }
        if (!randomTags.NullOrEmpty())
        {
            int selCount = Mathf.Min(randomTagsSelectCount.RandomInRange, randomTags.Count);
            if (selCount > 0)
            {
                tags.AddRange(randomTags.TakeRandomElements(selCount));
            }
        }
        return tags;
    }
}