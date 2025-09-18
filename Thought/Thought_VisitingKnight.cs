using OberoniaAurea_Frame;
using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public class Thought_VisitingKnight : Thought_Memory
{
    private static readonly SimpleValueCache<int> valueCache = new(cacheInterval: 2500, defaultValue: 0, GetMoodOffset);

    public override void ThoughtInterval()
    {
        age += 150;
        int index = GlobalOrderInteractionManager.OrderHallLevel;
        SetForcedStage(index > 0 ? index - 1 : 0);

    }
    public override float MoodOffset()
    {
        moodOffset = valueCache.GetCachedResult();
        return base.MoodOffset();
    }

    private static int GetMoodOffset()
    {
        ResidentKnight residentKnight = GlobalOrderInteractionManager.ResidentKnightsManager.GetResidentKnightOfRole(OARO_ModDefOf.OARO_Clerk);
        if (residentKnight is null)
        {
            return 0;
        }
        return (residentKnight.RoleDef.RoleWorker as ResidentKnightRoleWorker_Clerk)?.KnightMoodOffset(residentKnight.Pawn) ?? 0;
    }

    public static void ClearStaticCache()
    {
        valueCache.Reset();
    }
}