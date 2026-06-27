namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_StatModifiesBy_WealthBuildings : KnightVirtueComp_StatModifiesByValue
{
    protected override float GetValueForStat()
    {
        if (!this.Pawn.Spawned)
            return 0f;

        return this.Pawn.Map.wealthWatcher.WealthBuildings;
    }
}