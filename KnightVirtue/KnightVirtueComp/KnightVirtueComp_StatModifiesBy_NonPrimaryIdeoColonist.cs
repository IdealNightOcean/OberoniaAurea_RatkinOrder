using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_StatModifiesBy_NonPrimaryIdeoColonist : KnightVirtueComp_StatModifiesByValue
{
    protected override float GetValueForStat()
    {
        if (!ModsConfig.IdeologyActive || !this.Pawn.Spawned)
            return 0f;

        return ValueCacheManager.Instance.NonPrimaryIdeoColonistsCount.GetCachedResult(this.Pawn.Map);
    }
}
