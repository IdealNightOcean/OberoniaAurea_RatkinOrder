using NightOcean;
using OberoniaAurea.RatkinOrder.Utility;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;


public class UIData_KnightAcademic : UIDataBase
{
    public ResidentKnight Knight { get; private set; }
    public KnightAcademicDef Academic { get; private set; }

    public Color Color { get; private set; }

    public int StageLevel { get; private set; }
    public ResidentKnightAcademicStage Stage { get; private set; }

    private AcceptanceReport? canActiveBySelf;
    public bool CanActiveBySelf => canActiveBySelf ??= AcademicUtility.CanActivateAcademicBySelf(Knight, Academic, resultOnly: false);

    private float? costFactor;
    public float CostFactor
    {
        get
        {
            if (!IsDataValid)
                return 1f;
            if (!costFactor.HasValue)
            {
                ResidentKnightStatRequestData_Academic requestData = new(this.Knight, ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor, Academic);
                costFactor = ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.GetStatValue(requestData);
            }

            return costFactor.Value;
        }
    }
    public LazyMutable<string> CostFactorExplanation { get; }


    private float? upgradePointsCost;
    public float UpgradePointsCost
    {
        get
        {
            if (!IsDataValid && !CanActiveBySelf)
                return -1f;

            if (!upgradePointsCost.HasValue)
                upgradePointsCost = AcademicUtility.GetAcademicPointsCost(residentPawn: Knight,
                                                                          academicDef: Academic,
                                                                          sourceLevel: StageLevel,
                                                                          targetLevel: StageLevel + 1,
                                                                          resultOnly: true,
                                                                          explanation: out _);

            return upgradePointsCost.Value;
        }
    }

    public LazyMutable<string> UpgradePointsCostExplanation { get; }

    public UIData_KnightAcademic(ResidentKnight knight, KnightAcademicDef academic)
    {
        Knight = knight;
        Academic = academic;

        CostFactorExplanation = new(refreshFunc: RefreshCostFactorExplanation);
        UpgradePointsCostExplanation = new(refreshFunc: RefreshUpgradePointsCostExplanation);
    }

    protected override UIDataState RefreshInner()
    {
        if (Knight is null || Academic is null)
            return UIDataState.Empty;

        Color = this.Knight.Branch?.HonorDef?.color ?? Academic.chivalry?.color ?? Color.white;
        StageLevel = this.Knight.AcademicHandler.GetAcademicLevel(Academic);
        Stage = Academic.GetStage(StageLevel);
        canActiveBySelf = null;

        costFactor = null;
        CostFactorExplanation.MarkDirty();

        upgradePointsCost = null;
        UpgradePointsCostExplanation.MarkDirty();

        return UIDataState.Ready;
    }


    private string RefreshCostFactorExplanation()
    {
        if (!IsDataValid)
            return string.Empty;

        ResidentKnightStatRequestData_Academic requestData = new(this.Knight, ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor, Academic);

        (string explanation, float? resultNullable) = ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.GetStatModifyExplanation(requestData);

        costFactor = resultNullable ?? ResidentKnightStatDefOf.OARO_AcademicPointsCostFactor.baseValue;

        return explanation;
    }

    private string RefreshUpgradePointsCostExplanation()
    {
        if (!IsDataValid)
            return string.Empty;

        upgradePointsCost = AcademicUtility.GetAcademicPointsCost(residentPawn: Knight,
                                                                  academicDef: Academic,
                                                                  sourceLevel: StageLevel,
                                                                  targetLevel: StageLevel + 1,
                                                                  resultOnly: false,
                                                                  explanation: out string upgradePointsCostExplanation);

        return upgradePointsCostExplanation;
    }
}