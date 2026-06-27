using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_MoodOffsetBy_NonPrimaryIdeoColonist : KnightVirtueComp_MoodOffsetByValue
{
    protected override float GetValueForStat()
    {
        if (!ModsConfig.IdeologyActive || !this.Pawn.Spawned)
            return 0f;

        return this.Pawn.Map.GetComponent<MapComponent_RatkinOrder>()?.NonPrimaryIdeoColonistsCount.GetCachedResult() ?? 0f;
    }
}
