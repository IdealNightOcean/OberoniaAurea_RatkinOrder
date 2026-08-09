using NightOcean.Utility;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_KnightAcademicProgressBar : UIDataDrawerBase<UIData_KnightAcademicWithStage>
{
    private int SelectedStageLevel => (AcademicStagesListDrawer?.SelectedIndex ?? -1) + 1;

    private UIDataDrawer_KnightAcademicProgressBar_Stage StageDrawer { get; } = new();
    private UIDataDrawer_SelectableList_KnightAcademicStage AcademicStagesListDrawer { get; }


    private bool AcademicStageDrawerDatasInited { get; set; }

    public UIDataDrawer_KnightAcademicProgressBar()
    {
        DrawSize = new Vector2(968f, 198f);
        OutlineThickness = 2;

        this.AcademicStagesListDrawer = new(drawer: StageDrawer, drawDatas: [], parentAcademicData: null, rowLimit: 1, columnLimit: 4);
    }

    public override void SetDrawData(UIData_KnightAcademicWithStage drawData)
    {
        base.SetDrawData(drawData);
        AcademicStageDrawerDatasInited = false;
    }

    private void InitAcademicStageDrawerDatas(Rect drawRect)
    {
        AcademicStagesListDrawer.SetDrawDatas(DrawData.StagesDatas);
        AcademicStagesListDrawer.SetParentAcademicData(DrawData);
        AcademicStagesListDrawer.SetDrawSize(drawRect.size);
        AcademicStageDrawerDatasInited = true;
    }

    public override void DrawInner(Vector2 position)
    {
        Rect boxRect = new(position, DrawSize);
        Widgets.DrawBox(boxRect);

        Rect innerRect = GenUI.ContractedBy(boxRect, OutlineThickness);

        Rect topRect = innerRect.TopHalf();
        Rect bottomRect = innerRect.BottomHalf();

        Rect labelRect = topRect.TopPart(1f / 3f);

        float stagesRectHeight = topRect.height * (2f / 3f);
        StageDrawer.SetDrawSizeByHeight(stagesRectHeight - 2f - 20f);
        Rect stagesRect = new(0f, topRect.yMax - stagesRectHeight, topRect.width * 0.92f, stagesRectHeight);
        stagesRect = GenUI.CenteredOnXIn(stagesRect, topRect);
        DrawStages(stagesRect);

    }

    private void DrawStages(Rect inRect)
    {
        Widgets.DrawBox(inRect);
        Rect innerRect = GenUI.ContractedBy(inRect, 1);

        if (!AcademicStageDrawerDatasInited)
        {
            InitAcademicStageDrawerDatas(inRect);
        }

        AcademicStagesListDrawer.Draw(innerRect.TopLeftCorner());
    }

}