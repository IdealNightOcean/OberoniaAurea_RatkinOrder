using NightOcean;

namespace OberoniaAurea.RatkinOrder.UI;


public class UIData_KnightAcademic : UIDataBase
{
    public ResidentKnight Knight { get; }
    public KnightAcademicDef Academic { get; }
    public override bool IsValid => Knight is not null && Academic is not null;

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
        Level = this.Knight.AcademicHandler.GetAcademicLevel(Academic);
        ResidentKnightStatRequestData_Academic requestData = new(this.Knight, ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor, Academic);
        CostFactor = ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.GetStatValue(requestData);
        CostFactorExplanation.MarkDirty();
    }


    private string RefreshCostFactorExplanation()
    {
        if (!IsValid)
            return string.Empty;

        ResidentKnightStatRequestData_Academic requestData = new(this.Knight, ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor, Academic);

        (string explanation, float? resultNullabel) = ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.GetStatModifyExplanation(requestData);

        CostFactor = resultNullabel ?? ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.baseValue;

        return explanation;
    }
}