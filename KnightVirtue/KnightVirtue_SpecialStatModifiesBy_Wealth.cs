using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue_SpecialStatModifiesBy_Wealth : KnightVirtue_SpecialStatModifiesByValue
{
    protected override float ValueForStat => WealthUtility.PlayerWealth;
}
