using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ValueCacheManager
{
    public static ValueCacheManager Instance { get; private set; }

    public SimpleMapCahce<int> UnhealthyColonistsCount { get; } = new(cacheInterval: 15000, onlyPlayerHome: false, checker: GetUnhealthyColonistsCountColonistsCount);




    public SimpleMapCahce<int> NonPrimaryIdeoColonistsCount { get; } = new(cacheInterval: 15000, onlyPlayerHome: false, checker: GetUnhealthyColonistsCountColonistsCount);


    public ValueCacheManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;
    }

    public static void ClearStaticCache() => Instance = null;

    private static int GetUnhealthyColonistsCountColonistsCount(Map map)
    {
        int count = 0;
        foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            if (!OARO_PawnUtility.IsHealthyPawn(p))
                count++;

        return count;
    }

    private static int GetNonPrimaryIdeoColonistsCount(Map map)
    {
        if (!ModsConfig.IdeologyActive)
            return 0;
        Ideo primaryIdeo = Faction.OfPlayer.ideos?.PrimaryIdeo;
        if (primaryIdeo is null)
            return 0;

        int count = 0;
        foreach (Pawn p in map.mapPawns.AllHumanlikeSpawned)
        {
            if (p.IsColonist && primaryIdeo != p.ideo?.Ideo)
                count++;
        }
        return count;
    }

}
