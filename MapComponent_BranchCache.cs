using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MapComponent_BranchCache : MapComponent
{
    public IEnumerable<Branch> branchesInRadius;
    public int nextCacheTick = -1;

    public MapComponent_BranchCache(Map map) : base(map) { }

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame > nextCacheTick && map.IsPlayerHome)
        {
            branchesInRadius = BranchUtility.GetAllAffectedBranchSite(map.Tile);
        }
    }
}
