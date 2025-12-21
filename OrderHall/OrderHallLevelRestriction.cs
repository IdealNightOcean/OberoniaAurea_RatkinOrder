using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderHallLevelRestriction
{
    public int level;
    public List<ThingDefCountClass> buildings = [];
    public List<string> otherRestrictionDescs = [];

    public IEnumerable<string> GetRestrictionDescs()
    {
        if (!otherRestrictionDescs.NullOrEmpty())
        {
            foreach (string desc in otherRestrictionDescs)
            {
                yield return desc;
            }
        }

        if (!buildings.NullOrEmpty())
        {
            foreach (ThingDefCountClass buildingNeeded in buildings)
            {
                yield return buildingNeeded.LabelCap;
            }
        }
    }
}
