namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue_SpecialStatModifiesBy_TotalAcademicLevel : KnightVirtue_SpecialStatModifiesByValue
{
    protected override float ValueForStat => knight.AcademicHandler.TotalAcademicLevel.Value;
}
