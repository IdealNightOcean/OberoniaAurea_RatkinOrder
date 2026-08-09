using UnityEngine;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIData_KnightAcademicStage : UIDataBase
{
    public KnightAcademicDef Academic { get; }
    public int StageLevel { get; private set; }
    public ResidentKnightAcademicStage Stage { get; private set; }

    public UIData_KnightAcademicStage(KnightAcademicDef academic, int stageLevel)
    {
        Academic = academic;
        if (academic is not null)
            StageLevel = Mathf.Clamp(stageLevel, 0, Academic.MaxStageLevel);
    }

    protected override UIDataState RefreshInner()
    {
        if (Academic is null)
            return UIDataState.Empty;

        Stage = Academic.GetStage(StageLevel);

        StageLevel = Mathf.Clamp(StageLevel, 0, Academic.MaxStageLevel);
        return UIDataState.Ready;
    }
}
