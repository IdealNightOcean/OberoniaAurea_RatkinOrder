using NightOcean.Utility;
using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea_Frame.UI;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_SquadSummary : UIDataDrawerBase<UIData_BranchSummary>
{
    public UIDataDrawer_SquadSummary()
    {
        DrawSize = new(392f, 90f);
        OutlineThickness = 2;
    }

    protected override void DrawInner(Vector2 position)
    {
        Rect boxRect = new(position, DrawSize);
        Widgets.DrawBoxSolid(boxRect, OARO_ColorLibrary.DimDarkBackground);

        Rect innerBoxRect = RectUtils.ContractedBy(boxRect, OutlineThickness);

        float verticalLineX = innerBoxRect.xMin + innerBoxRect.width * 0.6f - OutlineThickness;

        Rect leftRect = innerBoxRect;
        leftRect.xMax = verticalLineX;
        DrawLeftRect(leftRect);

        Rect rightRect = innerBoxRect;
        rightRect.xMin = verticalLineX + OutlineThickness;
        DrawRightRect(rightRect);

        OAFrame_Widgets.DrawLineVertical(new(verticalLineX, innerBoxRect.yMin), innerBoxRect.height, OARO_ColorLibrary.CommonOutline, 2);
        OAFrame_Widgets.DrawBox(boxRect, OARO_ColorLibrary.CommonOutline, thickness: OutlineThickness);
    }

    /// <remarks>标准大约为(230f, 86f)</remarks>
    public void DrawLeftRect(Rect inRect)
    {
        Rect honorColorRect = inRect;
        honorColorRect.width = 5f;

        Rect innerRect = inRect;
        innerRect.xMin = honorColorRect.xMax; //标准大约为(226f, 86f)

        Rect branchIconRect = innerRect.LeftPart(0.45f);//标准大约为(100f, 86f)

        if (DrawData.IsDataValid && DrawData.Branch.HonorDef is not null)
        {
            BranchHonorDef honorDef = DrawData.Branch.HonorDef;
            Widgets.DrawBoxSolid(honorColorRect, honorDef.color);

            Rect honorDecorationRect = RectUtils.ContractedBy(innerRect, 10f);
            GUI.DrawTexture(honorDecorationRect, honorDef.chivalry.medal.honorDecorationTexture.Texture, ScaleMode.ScaleToFit);

            OAFrame_Widgets.DrawTextureWithColor(innerRect, OARO_IconLibrary.HonorBackgroundTex, honorDef.color);

            Rect honorIconRect = branchIconRect.CenterSegment(0.8f, 0.75f);
            GUI.DrawTexture(honorIconRect, honorDef.iconTexture.Texture, ScaleMode.ScaleToFit);
        }
        else
        {
            Rect normalBranchIconRect = branchIconRect.CenterSegment(0.5f, 0.5f);
            GUI.DrawTexture(normalBranchIconRect, OARO_IconLibrary.SmallGeneralBranchIcon, ScaleMode.ScaleToFit);
        }

        this.TextStyle = new(guiColor: DrawData.IsInAffectedRange ? Color.green : Color.white, font: GameFont.Small, anchor: TextAnchor.UpperLeft);
        OAFrame_Widgets.DrawLabel(innerRect, DrawData.Distance.ToString("F0"), this.TextStyle);

        Rect infoRect = innerRect;
        infoRect.xMin = branchIconRect.xMax;

        Rect squadNameRect = infoRect.TopPart(0.35f);
        if (DrawData.IsDataValid)
        {
            this.TextStyle = new(guiColor: DrawData.Branch.Color, font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
            if (OAFrame_Widgets.DrawLabelEllipses(squadNameRect, DrawData.SquadName, this.TextStyle))
            {
                TooltipHandler.TipRegion(squadNameRect, DrawData.SquadName);
            }
        }
        else
        {
            this.TextStyle = new(font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
            OAFrame_Widgets.DrawLabelEllipses(squadNameRect, "---", this.TextStyle);
        }


        Rect stateRect = infoRect;
        stateRect.yMin = squadNameRect.yMax;

        Rect friendlyRect = stateRect.LeftHalf();
        Rect friendlyIconRect = GenUI.ContractedBy(friendlyRect.TopPart(0.7f), 4f);

        Rect friendlyStrRect = friendlyRect.BottomPart(0.3f);
        if (DrawData.IsDataValid && DrawData.Branch.IsBranchOfType(BranchType.Friendly))
        {
            GUI.DrawTexture(friendlyIconRect, OARO_IconLibrary.SmallFriendlyIcon, ScaleMode.ScaleToFit);
            this.TextStyle = new(guiColor: Color.green, font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
            OAFrame_Widgets.DrawLabel(friendlyStrRect, "OARO_Friendly".Translate(), this.TextStyle);
        }
        else
        {
            GUI.DrawTexture(friendlyIconRect, OARO_IconLibrary.SmallStrangeIcon, ScaleMode.ScaleToFit);
            this.TextStyle = new(font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
            OAFrame_Widgets.DrawLabel(friendlyStrRect, "OARO_Strange".Translate(), this.TextStyle);
        }

        Rect workStateRect = stateRect.RightHalf();
        Rect workStateIconRect = GenUI.ContractedBy(workStateRect.TopPart(0.7f), 4f);
        OARO_UIUtility.DrawBranchStateIcon(workStateIconRect, DrawData.Branch, expand: false);

        Rect workStateStrRect = workStateRect.BottomPart(0.3f);
        string workState;
        if (DrawData.IsDataValid)
        {
            workState = DrawData.Branch.CurWorkStateDesc;
            this.TextStyle = new(guiColor: DrawData.Branch.CurWorkState == WorkStateType.Idle ? Color.white : Color.green,
                                 font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
        }
        else
        {
            workState = "OARO_BranchWorkState_Idle".Translate();
            this.TextStyle = new(font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
        }

        if (OAFrame_Widgets.DrawLabelEllipses(workStateStrRect, workState, this.TextStyle))
        {
            TooltipHandler.TipRegion(workStateStrRect, workState);
        }
    }

    public void DrawRightRect(Rect inRect)
    {
        float partRectHeight = inRect.height * (1f / 3f);
        Rect topRect = Rect.MinMaxRect(xmin: Mathf.Min(inRect.xMin + 24f, inRect.xMax - inRect.width * 0.9f),
                                       ymin: inRect.y,
                                       xmax: inRect.xMax,
                                       ymax: inRect.yMin + partRectHeight);
        this.TextStyle = new(font: GameFont.Small, anchor: TextAnchor.MiddleLeft);
        OAFrame_Widgets.DrawLabel(topRect, "OARO_AllCrewCountShortInfo".Translate(DrawData.Squad.AllCrewCountInt), this.TextStyle);

        Rect centerRect = RectUtils.OffsetVertical(topRect, partRectHeight);
        Widgets.DrawBoxSolid(inRect.CenterSegmentOnY((1f / 3f)), OARO_ColorLibrary.MediumDarkBackground);
        OAFrame_Widgets.DrawLabel(centerRect, "OARO_BranchPotencyShortInfo".Translate(DrawData.Branch.Potency.ToString("0.##")), this.TextStyle);

        Rect bottomRect = RectUtils.OffsetVertical(centerRect, partRectHeight);
        OAFrame_Widgets.DrawLabel(bottomRect, "OARO_BranchSupplyStateInfo".Translate(DrawData.Branch.SupplyState), this.TextStyle);
    }
}

