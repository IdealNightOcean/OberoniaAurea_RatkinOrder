using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_StatModifiesBy_MercyQuestSucceed_NonPrimaryIdeoColonist : KnightVirtueComp_StatModifiesByValue
{
    protected override float GetValueForStat()
    {
        if (!ModsConfig.IdeologyActive)
            return 0f;
        if (!this.Pawn.Spawned)
            return 0f;

        Ideo primaryIdeo = Faction.OfPlayer.ideos?.PrimaryIdeo;
        if (primaryIdeo is null)
            return 0f;

        int count = 0;
        foreach (Pawn p in this.Pawn.Map.mapPawns.AllHumanlikeSpawned)
        {
            if (p.IsColonist && primaryIdeo != p.ideo?.Ideo)
                count++;
        }
        return count;
    }
}
