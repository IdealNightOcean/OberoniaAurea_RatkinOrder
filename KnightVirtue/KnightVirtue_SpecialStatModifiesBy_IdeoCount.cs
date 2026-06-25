using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue_SpecialStatModifiesBy_IdeoCount : KnightVirtue_SpecialStatModifiesByValue
{
    protected override float ValueForStat
    {
        get
        {
            if (!ModsConfig.IdeologyActive)
                return 0f;

            int value = Faction.OfPlayer.ideos?.IdeosMinorListForReading.Count ?? 0;
            return value > 0 ? value + 1 : 0;
        }
    }
}
