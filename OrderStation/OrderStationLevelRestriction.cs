using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderStationLevelRestriction
{
    public int level;
    [MustTranslate]
    public List<string> effectDescs = [];

    public int areaFloor = -1;
    public float impressivenessFloor = -1f;
    public List<ThingDefCountClass> buildings = [];
}
