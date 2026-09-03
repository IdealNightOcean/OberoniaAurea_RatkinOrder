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

    protected override void DrawInner(Vector2 position)
    {
        bool isActive = ParentKnightAcademic is not null && ParentKnightAcademic.IsDataValid && ParentKnightAcademic.StageLevel >= DrawData.StageLevel;

        Rect boxRect = new(position, DrawSize);

        Rect innerBoxRect = GenUI.ContractedBy(boxRect, OutlineThickness);

        Rect topRect = innerBoxRect.TopPart(0.875f);
        Rect bottomRect = innerBoxRect;
        bottomRect.yMin = topRect.yMax;

        Rect validTopRect = topRect;
        validTopRect.xMax -= 2f;

        Rect labelRect = validTopRect.TopPart(0.35f);
        this.TextStyle = new(guiColor: AcademicColor, font: GameFont.Medium, anchor: TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(labelRect, DrawData.IsDataValid ? DrawData.Stage.label : "---", this.TextStyle);

        Rect iconRect = validTopRect.BottomPart(0.2f);
        iconRect = GenUI.ContractedBy(iconRect, 4f);
        GUI.DrawTexture(iconRect, OARO_IconLibrary.Placeholer);

        Rect descRect = validTopRect;
        descRect.yMin = labelRect.yMax;
        descRect.yMax = iconRect.yMin;
        this.TextStyle = new(guiColor: isActive ? Color.green : Color.white, font: GameFont.Medium, anchor: TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(descRect, DrawData.IsDataValid ? DrawData.Stage.shortDescription : "---", this.TextStyle);

        DrawBottomLine(bottomRect, isActive);
    }

    private void DrawBottomLine(Rect inRect, bool isActive)
    {
        Rect validRect = inRect;
        validRect.xMax -= 2f;

        Rect validTopRect = validRect.TopHalf();

        float boxSize = Mathf.Max(20f, validRect.height * 0.5f);
        Rect boxRect = new(0f, 0f, boxSize, boxSize);
        boxRect = boxRect.CenteredIn(validTopRect);
        Widgets.DrawBoxSolid(boxRect, Color.black);

        Rect innerBoxRect = GenUI.ContractedBy(boxRect, 4f);

        if (isActive)
            Widgets.DrawBoxSolid(innerBoxRect, AcademicColor);

        if (DrawData.StageLevel > 1)
        {
            Rect outHorizontalLineRectL = new(inRect.xMin, 0f, innerBoxRect.xMax - inRect.xMin, 6f);
            outHorizontalLineRectL = GenUI.CenteredOnYIn(outHorizontalLineRectL, boxRect);
            Widgets.DrawBoxSolid(outHorizontalLineRectL, Color.black);
            if (isActive)
            {
                Rect innerHorizontalLineRectL = innerBoxRect;
                innerHorizontalLineRectL.yMin += 2f;
                innerHorizontalLineRectL.yMax -= 2f;
                Widgets.DrawBoxSolid(innerHorizontalLineRectL, AcademicColor);
            }
        }

        if (DrawData.IsDataValid && DrawData.StageLevel < DrawData.Academic.MaxStageLevel)
        {
            Rect outHorizontalLineRectR = new(innerBoxRect.xMax, 0f, inRect.xMax - innerBoxRect.xMax, 6f);
            outHorizontalLineRectR = GenUI.CenteredOnYIn(outHorizontalLineRectR, boxRect);
            Widgets.DrawBoxSolid(outHorizontalLineRectR, Color.black);

            if (isActive)
            {
                Rect innerHorizontalLineRectR = innerBoxRect;
                innerHorizontalLineRectR.yMin += 2f;
                innerHorizontalLineRectR.yMax -= 2f;
                Widgets.DrawBoxSolid(innerHorizontalLineRectR, AcademicColor);
            }
        }

        if (Selected)
            Widgets.DrawBox(boxRect, thickness: 2);

    }
}
