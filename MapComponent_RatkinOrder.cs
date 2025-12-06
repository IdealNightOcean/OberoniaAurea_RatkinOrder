using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MapComponent_RatkinOrder : MapComponent, IOnBranchDestroyed
{
    public List<Branch> BranchesInRadius { get; set; }
    private int NextCacheTick { get; set; } = -1;

    public MapComponent_RatkinOrder(Map map) : base(map) { }

    public override void ExposeData() { }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        BranchesInRadius?.RemoveAll(b => b.RatkinOrder == order);
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        BranchesInRadius?.RemoveAll(b => b == branch);
    }

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame > NextCacheTick)
        {
            NextCacheTick = Find.TickManager.TicksGame + 60000;
            if (map.IsPlayerHome)
            {
                BranchesInRadius = BranchUtility.GetAllAffectedBranch(map.Tile).ToList();
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

    public static void OnBranchDestroyed(Branch branch)
    {
        List<Map> maps = Find.Maps;
        for (int i = 0; i < maps.Count; i++)
        {
            maps[i].GetOrderMapComp()?.Notify_BranchDestroyed(branch);
        }
    }
}