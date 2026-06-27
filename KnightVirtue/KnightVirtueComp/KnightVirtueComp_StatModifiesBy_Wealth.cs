using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_StatModifiesBy_Wealth : KnightVirtueComp_StatModifiesByValue
{
    protected override float GetValueForStat() => WealthUtility.PlayerWealth;
}