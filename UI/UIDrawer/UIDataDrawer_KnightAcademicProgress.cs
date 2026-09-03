using NightOcean.Utility;
using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea_Frame.UI;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_KnightAcademicProgress : UIDataDrawerBase<UIData_KnightAcademicWithStage>
{
    private int SelectedStageIndex => AcademicStagesListDrawer.SelectedIndex;
    private UIData_KnightAcademicStage SelectedStage => AcademicStagesListDrawer.SelectedItem;

    private UIDataDrawer_KnightAcademicProgressBar_Stage StageDrawer { get; }
    private UIDataDrawer_SelectableList_KnightAcademicStage AcademicStagesListDrawer { get; }


    private bool AcademicStageDrawerDatasInited { get; set; }

    public UIDataDrawer_KnightAcademicProgress()
    {
        DrawSize = new Vector2(1384f, 736f);
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
        OAFrame_Widgets.DrawBox(boxRect, OARO_ColorLibrary.CommonOutline);

        Rect innerRect = GenUI.ContractedBy(boxRect, OutlineThickness);

        Rect topRect = innerRect.TopHalf();
        Rect bottomRect = innerRect.BottomHalf();

        Rect labelRect = topRect.TopPart(1f / 3f);
        this.TextStyle = new(guiColor: DrawData.Color, fontSize: 40, anchor: TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(labelRect, DrawData.IsDataValid ? DrawData.Academic.LabelCap : "---", this.TextStyle);

        float stagesRectHeight = topRect.height * (2f / 3f);
        StageDrawer.SetDrawSizeByHeight(stagesRectHeight - 2f - 20f);
        Rect stagesRect = new(0f, topRect.yMax - stagesRectHeight, topRect.width * 0.92f, stagesRectHeight);
        stagesRect = GenUI.CenteredOnXIn(stagesRect, topRect);
        DrawStages(stagesRect);


        DrawBottom(bottomRect);

    }

    private void DrawStages(Rect inRect)
    {
        Widgets.DrawBox(inRect);
        Rect innerRect = GenUI.ContractedBy(inRect, 1);

        if (!AcademicStageDrawerDatasInited)
        {
            InitAcademicStageDrawerDatas(inRect);
        }

        AcademicStagesListDrawer.Draw(innerRect.position);
    }

    private void DrawBottom(Rect inRect)
    {
        Rect validRect = inRect.CenterSegment(0.85f);
        UIData_KnightAcademicStage selectedStage = SelectedStage;
        bool isStageValid = selectedStage is not null && selectedStage.IsDataValid;

        Rect descRect = validRect.LeftHalf();
        Rect descLabelRect = descRect.TopPart((1f / 3f));
        Rect descInfoRect = descRect;
        descInfoRect.yMin = descLabelRect.yMax;

        this.TextStyle = new(guiColor: DrawData.Color, fontSize: 30, anchor: TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(descLabelRect, isStageValid ? selectedStage.Stage.label : "---", this.TextStyle);

        this.TextStyle = new(font: GameFont.Medium, anchor: TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(descInfoRect, isStageValid ? selectedStage.Stage.shortDescription : "---", this.TextStyle);

        Rect unlockRect = validRect.RightHalf();
        Rect unlockPointRect = unlockRect.TopPart((1f / 3f));
        Rect unlockButtonRect = unlockRect;
        unlockButtonRect.yMin = unlockPointRect.yMax;

        if (DrawData.IsDataValid && isStageValid)
        {
            if (selectedStage.StageLevel <= DrawData.StageLevel)
            {
                this.TextStyle = new(font: GameFont.Medium, anchor: TextAnchor.MiddleCenter);
                OAFrame_Widgets.DrawLabel(unlockButtonRect, "OARO_Unlocked".Translate(), this.TextStyle);
            }
            else if (selectedStage.StageLevel == DrawData.StageLevel + 1)
            {
                this.TextStyle = new(fontSize: 30, anchor: TextAnchor.MiddleCenter);
                OAFrame_Widgets.DrawLabel(unlockPointRect, DrawData.UpgradePointsCost.ToString("F0"), this.TextStyle);
                TooltipHandler.TipRegion(unlockPointRect, () => DrawData?.UpgradePointsCostExplanation.Value ?? string.Empty, 325832412);
            }
            else
            {
                this.TextStyle = new(font: GameFont.Medium, anchor: TextAnchor.MiddleCenter);
                OAFrame_Widgets.DrawLabel(unlockButtonRect, "OARO_NeedPreAcademicLevel".Translate(), this.TextStyle);
            }
        }
    }

}