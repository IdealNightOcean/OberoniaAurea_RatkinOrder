using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue_SpecialStatModifiesBy_NonPrimaryIdeoColonist : KnightVirtue_SpecialStatModifiesByValue
{
    protected override float ValueForStat
    {
        get
        {
            if (!ModsConfig.IdeologyActive)
                return 0f;
            if (!knight.Pawn.Spawned)
                return 0f;

            Ideo primaryIdeo = Faction.OfPlayer.ideos?.PrimaryIdeo;
            if (primaryIdeo is null)
                return 0f;

            int count = 0;
            foreach (Pawn p in knight.Pawn.Map.mapPawns.AllHumanlikeSpawned)
            {
                if (p.IsColonist && primaryIdeo != p.ideo?.Ideo)
                    count++;
            }
            return count;
        }
    }
}
