using NightOcean.Utility;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_KnightAcademicProgress : UIDataDrawerBase<UIData_KnightAcademicWithStage>
{
    private int SelectedStageIndex => AcademicStagesListDrawer.SelectedIndex;

    private UIDataDrawer_KnightAcademicProgressBar_Stage StageDrawer { get; }
    private UIDataDrawer_SelectableList_KnightAcademicStage AcademicStagesListDrawer { get; }


    private bool AcademicStageDrawerDatasInited { get; set; }

    public UIDataDrawer_KnightAcademicProgress()
    {
        DrawSize = new Vector2(1384f, 714f);
        OutlineThickness = 2;

        this.StageDrawer = new();
        this.AcademicStagesListDrawer = new(drawer: StageDrawer, drawDatas: [], parentAcademicData: null)
        {
            RowLimit = 1,
            ColumnLimit = 4,
            HorizontalScroll = true,
            LayoutStrategy = ScrollLayoutStrategy.ViewGivenItemAdapt
        };
    }

    public override void SetDrawData(UIData_KnightAcademicWithStage drawData)
    {
        base.SetDrawData(drawData);
        AcademicStagesListDrawer.ResetSelection();
        AcademicStageDrawerDatasInited = false;
    }

    private void InitAcademicStageDrawerDatas(Rect drawRect)
    {
        if (DrawData is null)
            SetDrawData(UIData_KnightAcademicWithStage.EmptyData);

        DrawData.Refresh();
        AcademicStagesListDrawer.SetDrawDatas(DrawData.StagesDatas);
        AcademicStagesListDrawer.SetParentAcademicData(DrawData);
        AcademicStagesListDrawer.SetDrawSize(drawRect.size);

        AcademicStageDrawerDatasInited = true;
    }

    protected override void DrawInner(Vector2 position)
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
            Log.Message($"{AcademicStagesListDrawer.DrawDatas.Count} | {AcademicStagesListDrawer.Drawer is null} | {AcademicStagesListDrawer.DrawSize}");
        }


        AcademicStagesListDrawer.Draw(innerRect.TopLeftCorner());
    }

}