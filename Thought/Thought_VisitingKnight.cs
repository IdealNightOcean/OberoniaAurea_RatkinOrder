using OberoniaAurea_Frame;
using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public class Thought_VisitingKnight : Thought_Memory
{
    private static SimpleValueCache<int> valueCache = new(cacheInterval: 2500, defaultValue: 0, GetMoodOffset);

    public override void ThoughtInterval()
    {
        age += 150;
        int index = OrderStationHandler.Instance.OrderStationLevel;
        SetForcedStage(index > 0 ? index - 1 : 0);

    }
    public override float MoodOffset()
    {
        moodOffset = valueCache.GetCachedResult();
        return base.MoodOffset();
    }

    private static int GetMoodOffset()
    {
        if (ResidentRoleManager.Instance.TryGetKnightOfRole(OARO_ModDefOf.OARO_Clerk, out ResidentKnight record))
        {
            return (OARO_ModDefOf.OARO_Clerk.RoleWorker as ResidentKnightRoleWorker_Clerk)?.KnightMoodOffset(record.Pawn) ?? 0;
        }
        return 0;
    }

    public static void ClearStaticCache()
    {
        valueCache.Reset();
    }
}