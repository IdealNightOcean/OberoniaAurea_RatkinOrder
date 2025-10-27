using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Thought_VisitingKnight : Thought_Memory
{
    private static readonly SimpleValueCache<int> valueCache = new(cacheInterval: 2500, defaultValue: 0, GetMoodOffset);

    public override void ThoughtInterval()
    {
        age += 150;
        int index = OrderHallHandler.OrderHallLevel;
        SetForcedStage(index > 0 ? index - 1 : 0);

    }
    public override float MoodOffset()
    {
        moodOffset = valueCache.GetCachedResult();
        return base.MoodOffset();
    }

    private static int GetMoodOffset()
    {
        if (ResidentKnightsManager.TryGetKnightOfRole(OARO_ModDefOf.OARO_Clerk, out Pawn knight))
        {
            return (OARO_ModDefOf.OARO_Clerk.RoleWorker as ResidentKnightRoleWorker_Clerk)?.KnightMoodOffset(knight) ?? 0;
        }
        return 0;
    }

    public static void ClearStaticCache()
    {
        valueCache.Reset();
    }
}