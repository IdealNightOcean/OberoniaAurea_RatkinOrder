namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_StatModifiesBy_ResidentKnight : KnightVirtueComp_StatModifiesByValue
{
    protected override float GetValueForStat() => ResidentPawnsManager.Instance.KnightsCount;
}
