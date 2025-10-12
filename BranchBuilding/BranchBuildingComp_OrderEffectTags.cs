using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingCompProperties_OrderEffectTags : BranchBuildingCompProperties
{
    public List<string> tags;

    public BranchBuildingCompProperties_OrderEffectTags()
    {
        compClass = typeof(BranchBuildingComp_OrderEffectTags);
    }
}

public class BranchBuildingComp_OrderEffectTags : BranchBuildingComp
{
    private BranchBuildingCompProperties_OrderEffectTags Props => (BranchBuildingCompProperties_OrderEffectTags)props;
    public override void PostPostActive()
    {
        if (!Props.tags.NullOrEmpty())
        {
            TagStrToBoolCountable effectTags = parent.RatkinOrder.EffectTags;
            foreach (string tag in Props.tags)
            {
                effectTags.IncrementTagValue(tag, addIfMiss: true);
            }
        }
    }

    public override void PostPostDeactive()
    {
        if (!Props.tags.NullOrEmpty())
        {
            TagStrToBoolCountable effectTags = parent.RatkinOrder.EffectTags;
            foreach (string tag in Props.tags)
            {
                effectTags.DecrementTagValue(tag);
            }
        }
    }
}