using OberoniaAurea_Frame.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;


public class UIDataDrawer_KnightAcademicProgressBar : UIDataDrawerBase<UIData_KnightAcademic>
{
    private int SelectedStage { get; set; } = -1;

    public UIDataDrawer_KnightAcademicProgressBar()
    {
        DrawSize = new Vector2(968f, 198f);
    }

    public override void DrawInner(Vector2 position)
    {
        throw new NotImplementedException();
    }
}



public class UIDataDrawer_SelectableList_KnightAcademicStage : UIDataDrawer_SelectableList<UIData_KnightAcademicStage>
{
    private UIData_KnightAcademic ParentAcademicData { get; }
    private int SelectedStage { get; set; } = -1;


    public UIDataDrawer_SelectableList_KnightAcademicStage(UIDataDrawerBase<UIData_KnightAcademicStage> drawer, IList<UIData_KnightAcademicStage> drawDatas, UIData_KnightAcademic parentAcademicData, int rowLimit = -1, int columnLimit = -1, bool horizontalWarp = false) : base(drawer, drawDatas, rowLimit, columnLimit, horizontalWarp)
    {
        ParentAcademicData = parentAcademicData;
    }


    protected override void DrawEntry(Rect inRect, int dataIndex)
    {
        UIData_KnightAcademicStage drawData = DrawDatas[dataIndex];
        if (drawData is null || !drawData.IsValid)
            return;

        bool isActiveStage = drawData.StageLevel <= ParentAcademicData.StageLevel;
        Color labelColor = isActiveStage ? Color.white : Color.gray;

        Rect innerRect = inRect;
        innerRect.xMax -= 2f;
        float innerX = innerRect.xMin;
        float innerY = innerRect.yMin;
        float innerWidth = innerRect.width;

        Rect reusedRect;

        if (drawData.StageLevel < drawData.Academic.MaxStageLevel)
        {
            reusedRect = inRect;
            reusedRect.xMin = reusedRect.xMax - 2f;
            reusedRect.yMin += 4f;
            reusedRect.yMax -= 4f;
            // GUI.DrawTexture(reusedRect, academicCuttingLine);
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(innerX, innerY + 32f, innerWidth, 20f);
        this.TextStyle = new(guiColor: labelColor, font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(reusedRect, drawData.Stage.label.CapitalizeFirst(), this.TextStyle);

        Text.Anchor = TextAnchor.UpperCenter;
        reusedRect = OARO_UIUtility.CenterRectOnX(innerRect, innerY + 65f, 190f, 45f);
        this.TextStyle = new(guiColor: labelColor, font: GameFont.Small, anchor: TextAnchor.UpperCenter);
        OAFrame_Widgets.DrawLabel(reusedRect, drawData.Stage.shortDescription.CapitalizeFirst(), this.TextStyle);

        reusedRect = OARO_UIUtility.CenterRectOnX(innerRect, innerY + 120f, 30f, 25f);

        Rect selectBoxRect = OARO_UIUtility.CenterRectOnX(innerRect, innerY + 155f, 20f, 20f);

        reusedRect = GenUI.ContractedBy(selectBoxRect, 2f);
        GUI.DrawTexture(reusedRect, BaseContent.BlackTex);
        Rect selectBoxActiveRect = GenUI.ContractedBy(reusedRect, 2f);
        if (isActiveStage)
        {
            Widgets.DrawBoxSolid(selectBoxActiveRect, ParentAcademicData.Color);
        }

        if (drawData.StageLevel > 1)
        {
            Rect leftLineRect = new(inRect.xMin, OARO_UIUtility.CenterMinCoords(selectBoxActiveRect.yMin, selectBoxActiveRect.height, 6f), selectBoxActiveRect.xMin - inRect.xMin, 6f);
            GUI.DrawTexture(leftLineRect, BaseContent.BlackTex);
            if (isActiveStage)
            {
                leftLineRect.yMin += 2f;
                leftLineRect.yMax -= 2f;
                Widgets.DrawBoxSolid(leftLineRect, ParentAcademicData.Color);
            }
        }

        if (drawData.StageLevel < drawData.Academic.MaxStageLevel)
        {
            Rect rightLineRect = new(selectBoxActiveRect.xMax, OARO_UIUtility.CenterMinCoords(selectBoxActiveRect.yMin, selectBoxActiveRect.height, 6f), inRect.xMax - selectBoxActiveRect.xMax, 6f);
            GUI.DrawTexture(rightLineRect, BaseContent.BlackTex);
            if (drawData.StageLevel < ParentAcademicData.StageLevel)
            {
                rightLineRect.yMin += 2f;
                rightLineRect.yMax -= 2f;
                Widgets.DrawBoxSolid(rightLineRect, ParentAcademicData.Color);
            }
        }

        if (Widgets.ButtonInvisible(inRect))
            SelectItem(dataIndex);

        if (SelectedIndex == dataIndex)
        {
            Widgets.DrawBox(inRect);
            Widgets.DrawHighlightSelected(inRect);
        }
        else if (Mouse.IsOver(inRect))
            Widgets.DrawHighlight(inRect);
    }
}
