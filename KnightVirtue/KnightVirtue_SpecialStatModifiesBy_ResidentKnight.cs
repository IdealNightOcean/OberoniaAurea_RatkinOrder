namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue_SpecialStatModifiesBy_ResidentKnight : KnightVirtue_SpecialStatModifiesByValue
{
    protected override float ValueForStat => ResidentPawnsManager.Instance.KnightsCount;
}
