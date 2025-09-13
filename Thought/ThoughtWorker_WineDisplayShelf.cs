using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ThoughtWorker_WineDisplayShelf : ThoughtWorker
{
    [Unsaved] private SimpleMapCahce<bool> mapCache = new(cacheInterval: 30000, defaultValue: false, onlyPlayerHome: true, Checker);

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (mapCache.GetCachedResult(p.Map))
        {
            return ThoughtState.ActiveAtStage(0);
        }
        return ThoughtState.Inactive;
    }

    private static bool Checker(Map map)
    {
        return map.listerThings.AnyThingWithDef(OARO_ThingDefOf.OARO_WineDisplayShelf);
    }
}
