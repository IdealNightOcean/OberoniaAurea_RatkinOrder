namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_StatModifiesBy_TotalAcademicLevel : KnightVirtueComp_StatModifiesByValue
{
    protected override float GetValueForStat() => Knight.AcademicHandler.TotalAcademicLevel.Value;
}
