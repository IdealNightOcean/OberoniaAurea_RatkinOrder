using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderHallRestrictionExtension : DefModExtension
{
    private List<string> forbiddenBuildingTags = [];
    public HashSet<string> ForbiddenBuildingTags = [];

    public List<OrderHallLevelRestriction> hallLevelRestriction = [];

    public int MaxLevel => hallLevelRestriction.Count;

    public OrderHallLevelRestriction GetRestrictionOfLevel(int level)
    {
        if (level < 1 || level > hallLevelRestriction.Count)
        {
            return null;
        }
        return hallLevelRestriction[level - 1];
    }

    public override IEnumerable<string> ConfigErrors()
    {
        ForbiddenBuildingTags = [.. forbiddenBuildingTags];
        hallLevelRestriction.SortBy(r => r.level);
        return base.ConfigErrors();
    }
}