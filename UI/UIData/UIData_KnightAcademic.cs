using NightOcean;

namespace OberoniaAurea.RatkinOrder.UI;


public class UIData_KnightAcademic : UIDataBase
{
    public ResidentKnight Knight { get; }
    public KnightAcademicDef Academic { get; }

    public int Level { get; private set; }

    public float CostFactor { get; private set; }

    public LazyMutable<string> CostFactorExplanation { get; }

    public UIData_KnightAcademic(ResidentKnight knight, KnightAcademicDef academic)
    {
        Knight = knight;
        Academic = academic;

        CostFactorExplanation = new(refreshFunc: RefreshCostFactorExplanation);
    }

    protected override void RefreshInner()
    {
        Level = Knight.AcademicHandler.GetAcademicLevel(Academic);
        ResidentKnightStatRequestData_Academic requestData = new(Knight, ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor, Academic);

        CostFactor = ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.GetStatValue(requestData);
        CostFactorExplanation.Reset();
    }


    private string RefreshCostFactorExplanation()
    {
        ResidentKnightStatRequestData_Academic requestData = new(Knight, ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor, Academic);

        (string explanation, float? resultNullabel) = ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.GetStatModifyExplanation(requestData);

        CostFactor = resultNullabel ?? ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.baseValue;

        return explanation;
    }
}