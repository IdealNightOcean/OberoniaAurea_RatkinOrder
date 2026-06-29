using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_StatModifiesBy_IdeoCount : KnightVirtueComp_StatModifiesByValue
{
    protected override float GetValueForStat()
    {
        if (!ModsConfig.IdeologyActive)
            return 0f;

        int value = Faction.OfPlayer.ideos?.IdeosMinorListForReading.Count ?? 0;
        return value > 0 ? value + 1 : 0;
    }
}

public class KnightVirtueComp_StatModifiesBy_InjuredColonists : KnightVirtueComp_StatModifiesByValue
{
    protected override float GetValueForStat()
    {
        if (!this.Pawn.Spawned)
            return 0f;

        return ValueCacheManager.Instance.InjuredColonistsCount.GetCachedResult(this.Pawn.Map);
    }

}
