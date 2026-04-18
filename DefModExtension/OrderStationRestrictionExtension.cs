using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderStationRestrictionExtension : DefModExtension
{
    private List<string> forbiddenBuildingTags = [];
    public HashSet<string> ForbiddenBuildingTags = [];

    public List<OrderStationLevelRestriction> stationLevelRestriction = [];

    public int MaxLevel => stationLevelRestriction.Count;

    public OrderStationLevelRestriction GetRestrictionOfLevel(int level)
    {
        if (level < 1 || level > stationLevelRestriction.Count)
        {
            return null;
        }
        return stationLevelRestriction[level - 1];
    }

    public override IEnumerable<string> ConfigErrors()
    {
        ForbiddenBuildingTags = [.. forbiddenBuildingTags];
        stationLevelRestriction.SortBy(r => r.level);
        return base.ConfigErrors();
    }
}