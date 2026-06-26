using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GlobalEffectTags : KnightVirtueComp
{
    public KnightVirtueCompProperties_EffectTags Props => (KnightVirtueCompProperties_EffectTags)props;

    public override void PostActive()
    {
        base.PostActive();
        if (Props.effectTags.NullOrEmpty())
            return;

        foreach (string tag in Props.effectTags)
            ResidentPawnsManager.EffectTags.OffsetTagValueBy(tag: tag, offset: 1, addIfMiss: true);
    }

    public override void PostRemove()
    {
        base.PostRemove();
        if (Props.effectTags.NullOrEmpty())
            return;

        foreach (string tag in Props.effectTags)
            ResidentPawnsManager.EffectTags.OffsetTagValueBy(tag: tag, offset: -1, addIfMiss: false);
    }
}
