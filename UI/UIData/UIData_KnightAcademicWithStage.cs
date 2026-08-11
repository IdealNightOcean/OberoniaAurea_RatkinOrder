using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIData_KnightAcademicWithStage : UIData_KnightAcademic
{
    public List<UIData_KnightAcademicStage> StagesDatas { get; } = [];

    public static UIData_KnightAcademicWithStage EmptyData => new(null, null);

    public UIData_KnightAcademicWithStage(ResidentKnight knight, KnightAcademicDef academic) : base(knight, academic) { }

    protected override UIDataState RefreshInner()
    {
        UIDataState dataState = base.RefreshInner();
        if (dataState != UIDataState.Ready)
            return dataState;

        StagesDatas.Clear();
        StagesDatas.Capacity = Academic.MaxStageLevel;

        List<ResidentKnightAcademicStage> academicStages = Academic.academicStages;
        for (int i = 0; i < academicStages.Count; i++)
        {
            StagesDatas.Add(new UIData_KnightAcademicStage(Academic, i + 1));
        }

        return UIDataState.Ready;
    }
}