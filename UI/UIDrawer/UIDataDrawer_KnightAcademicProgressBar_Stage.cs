using NightOcean.Utility;
using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea_Frame.UI;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_KnightAcademicProgressBar_Stage : UIDataDrawerBase<UIData_KnightAcademicStage>
{
    public bool Selected { get; protected set; }

    public UIData_KnightAcademic ParentKnightAcademic { get; protected set; }
    protected Color AcademicColor { get; set; } = Color.white;

    public UIDataDrawer_KnightAcademicProgressBar_Stage()
    {
        DrawSize = new Vector2(342f, 240f);
        OutlineThickness = 0;
    }

    public void SetDrawData(UIData_KnightAcademicStage drawData, bool isSelected)
    {
        SetDrawData(drawData);
        Selected = isSelected;
    }

    public void SetParentKnightAcademic(UIData_KnightAcademic parentKnightAcademic)
    {
        ParentKnightAcademic = parentKnightAcademic;
        AcademicColor = ParentKnightAcademic?.Color ?? Color.white;

        Selected = false;
    }

    public override void DrawInner(Vector2 position)
    {
        bool isActive = ParentKnightAcademic is not null && ParentKnightAcademic.IsDataValid && ParentKnightAcademic.StageLevel >= DrawData.StageLevel;

        Rect boxRect = new(position, DrawSize);

        Rect innerBoxRect = GenUI.ContractedBy(boxRect, OutlineThickness);

        Rect topRect = innerBoxRect.BottomPart(0.875f);
        Rect bottomRect = innerBoxRect;
        bottomRect.yMin = topRect.yMax;

        if (DrawData.IsDataValid && DrawData.StageLevel < DrawData.Academic.MaxStageLevel)
        {
            Rect verticalDividingLine = topRect;
            verticalDividingLine.xMin = topRect.xMax - 2f;


        }

        Rect validTopRect = topRect;
        validTopRect.xMax -= 2f;

        Rect labelRect = validTopRect.TopPart(0.35f);
        this.TextStyle = new(guiColor: AcademicColor, font: GameFont.Medium, anchor: TextAnchor.LowerCenter);
        OAFrame_Widgets.DrawLabel(labelRect, DrawData.IsDataValid ? "" : DrawData.Academic.LabelCap, this.TextStyle);

        Rect iconRect = validTopRect.BottomPart(0.2f);
        iconRect = GenUI.ContractedBy(iconRect, 4f);
        GUI.DrawTexture(iconRect, OARO_IconLibrary.Placeholer);

        Rect descRect = validTopRect;
        descRect.yMin = topRect.yMax;
        descRect.yMax = bottomRect.yMin;
        this.TextStyle = new(guiColor: isActive ? Color.green : Color.white, font: GameFont.Medium, anchor: TextAnchor.LowerCenter);
        OAFrame_Widgets.DrawLabel(labelRect, DrawData.IsDataValid ? "" : DrawData.Stage.shortDescription, this.TextStyle);

        DrawBottomLine(bottomRect, isActive);
    }

    private void DrawBottomLine(Rect inRect, bool isActive)
    {
        Rect validRect = inRect;
        validRect.xMax -= 2f;

        Rect validTopRect = validRect.TopHalf();

        float boxSize = validRect.height * 0.5f;
        Rect boxRect = new(0f, 0f, boxSize, boxSize);
        boxRect = boxRect.CenteredIn(validTopRect);
        Widgets.DrawBoxSolid(boxRect, Color.black);

        Rect innerBoxRect = GenUI.ContractedBy(boxRect, 4f);

        if (isActive)
            Widgets.DrawBoxSolid(innerBoxRect, AcademicColor);

        if (DrawData.IsDataValid && DrawData.StageLevel < DrawData.Academic.MaxStageLevel)
        {
            Rect outHorizontalLineRect = new(innerBoxRect.xMax, 0f, validRect.xMax - innerBoxRect.xMax, 6f);
            outHorizontalLineRect = GenUI.CenteredOnYIn(outHorizontalLineRect, boxRect);
            Widgets.DrawBoxSolid(outHorizontalLineRect, Color.black);

            if (isActive)
            {
                Rect innerHorizontalLineRect = innerBoxRect;
                innerHorizontalLineRect.yMin += 2f;
                innerHorizontalLineRect.yMax -= 2f;
                Widgets.DrawBoxSolid(innerHorizontalLineRect, AcademicColor);
            }
        }

        if (Selected)
            Widgets.DrawBox(boxRect, thickness: 2);

    }
}
