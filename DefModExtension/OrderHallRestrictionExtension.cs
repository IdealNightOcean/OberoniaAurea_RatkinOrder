using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderHallRestrictionExtension : DefModExtension
{
    public HashSet<string> forbiddenBuildingTags = [];

    public List<OrderHallBuildingRequirements> buildingRequirements = [];

    public override IEnumerable<string> ConfigErrors()
    {
        buildingRequirements.SortBy(r => r.level);
        return base.ConfigErrors();
    }
}