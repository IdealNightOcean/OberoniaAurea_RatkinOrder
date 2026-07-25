namespace OberoniaAurea.RatkinOrder.UI;


public class UIData_KnightAcademic : UIDataBase
{
    public ResidentKnight Knight { get; }
    public KnightAcademicDef Academic { get; }

    public int Level { get; private set; }

    public float CostFactor { get; private set; }

    private string costFactorExplanation;

    public string CostFactorExplanation
    {
        get
        {
            if (costFactorExplanation is null)
            {
                RefreshCostFactorExplanation();
            }
            return costFactorExplanation;
        }
    }

    public UIData_KnightAcademic(ResidentKnight knight, KnightAcademicDef academic)
    {
        Knight = knight;
        Academic = academic;
    }

    protected override void RefreshInner()
    {
        Level = Knight.AcademicHandler.GetAcademicLevel(Academic);
        ResidentKnightStatRequestData_Academic requestData = new(Knight, ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor, Academic);

        CostFactor = ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.GetStatValue(requestData);
        costFactorExplanation = null;
    }


    private void RefreshCostFactorExplanation()
    {
        ResidentKnightStatRequestData_Academic requestData = new(Knight, ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor, Academic);

        (costFactorExplanation, float? costFactorNullabel) = ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.GetStatModifyExplanation(requestData);

        CostFactor = costFactorNullabel ?? ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.baseValue;
    }
}