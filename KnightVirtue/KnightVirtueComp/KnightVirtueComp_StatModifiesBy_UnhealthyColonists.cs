namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_StatModifiesBy_UnhealthyColonists : KnightVirtueComp_StatModifiesByValue
{
    protected override float GetValueForStat()
    {
        if (!this.Pawn.Spawned)
            return 0f;

        return ValueCacheManager.Instance.UnhealthyColonistsCount.GetCachedResult(this.Pawn.Map);
    }

}
