using UnityEngine;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIData_KnightAcademicStage : UIDataBase
{
    public KnightAcademicDef Academic { get; }
    public int StageLevel { get; private set; }
    public ResidentKnightAcademicStage Stage { get; private set; }

    public override bool IsValid => Academic is not null && StageLevel >= 0;

    public UIData_KnightAcademicStage(KnightAcademicDef academic, int stageLevel)
    {
        Academic = academic;
        StageLevel = Mathf.Clamp(stageLevel, 0, Academic.MaxStageLevel);
    }

    protected override void RefreshInner()
    {
        Stage = Academic.GetStage(StageLevel);
    }
}
