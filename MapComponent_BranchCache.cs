using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MapComponent_RatkinOrder : MapComponent, IBranchRelated
{
    public List<Branch> branchesInRadius;
    public int nextCacheTick = -1;

    public MapComponent_RatkinOrder(Map map) : base(map) { }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        branchesInRadius?.RemoveAll(b => b.RatkinOrder == order);
    }

    public void Notify_BranchDestoryed(Branch branch)
    {
        branchesInRadius?.RemoveAll(b => b == branch);
    }

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame > nextCacheTick)
        {
            nextCacheTick = Find.TickManager.TicksGame + 60000;
            if (map.IsPlayerHome)
            {
                branchesInRadius = BranchUtility.GetAllAffectedBranchSite(map.Tile).ToList();
            }
        }
    }

    public static void OnRatkinOrderRemoved(RatkinOrder order)
    {
        List<Map> maps = Find.Maps;
        for (int i = 0; i < maps.Count; i++)
        {
            if (maps[i].IsPlayerHome)
            {
                maps[i].GetComponent<MapComponent_RatkinOrder>()?.Notify_RatkinOrderRemoved(order);
            }
        }
    }

    public static void OnBranchDestoryed(Branch branch)
    {
        List<Map> maps = Find.Maps;
        for (int i = 0; i < maps.Count; i++)
        {
            if (maps[i].IsPlayerHome)
            {
                maps[i].GetComponent<MapComponent_RatkinOrder>()?.Notify_BranchDestoryed(branch);
            }
        }
    }
}
