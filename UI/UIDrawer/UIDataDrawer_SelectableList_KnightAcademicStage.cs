using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_SelectableList_KnightAcademicStage : UIDataDrawer_SelectableList<UIData_KnightAcademicStage, UIDataDrawer_KnightAcademicProgressBar_Stage>
{
    private UIData_KnightAcademic ParentAcademicData { get; set; }

    public UIDataDrawer_SelectableList_KnightAcademicStage(UIDataDrawer_KnightAcademicProgressBar_Stage drawer, IList<UIData_KnightAcademicStage> drawDatas, UIData_KnightAcademic parentAcademicData) : base(drawer, drawDatas)
    {
        ParentAcademicData = parentAcademicData;
        Drawer?.SetParentKnightAcademic(ParentAcademicData);
    }

    public override void SetDrawer(UIDataDrawer_KnightAcademicProgressBar_Stage drawer)
    {
        base.SetDrawer(drawer);
        Drawer?.SetParentKnightAcademic(ParentAcademicData);
    }

    public void SetParentAcademicData(UIData_KnightAcademic parentAcademicData)
    {
        ParentAcademicData = parentAcademicData;
        Drawer?.SetParentKnightAcademic(ParentAcademicData);
        ResetSelection();
    }

    protected override void DrawEntry(Rect inRect, int dataIndex)
    {
        Drawer.SetDrawData(DrawDatas[dataIndex], isSelected: dataIndex == SelectedIndex);
        Drawer.Draw(inRect.position);

        if (Widgets.ButtonInvisible(inRect))
            SelectItem(dataIndex);

        if (dataIndex == SelectedIndex)
            Widgets.DrawHighlightSelected(inRect);

        else if (Mouse.IsOver(inRect))
            Widgets.DrawHighlight(inRect);
    }
}
