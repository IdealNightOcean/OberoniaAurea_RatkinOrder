using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderHallRestrictionExtension : DefModExtension
{
    private List<string> forbiddenBuildingTags = [];
    public HashSet<string> ForbiddenBuildingTags = [];

    public List<OrderHallBuildingRequirements> buildingRequirements = [];

    public override IEnumerable<string> ConfigErrors()
    {
        ForbiddenBuildingTags = [.. forbiddenBuildingTags];
        buildingRequirements.SortBy(r => r.level);
        return base.ConfigErrors();
    }
}