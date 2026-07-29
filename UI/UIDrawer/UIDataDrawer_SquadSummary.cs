using NightOcean.Utility;
using OberoniaAurea_Frame;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_SquadSummary : UIDataDrawerBase<UIData_BranchSummary>
{
    public override Vector2 DefaultSize => new(392f, 90f);

    public override void DrawInner(Vector2 position, UIData_BranchSummary drawData)
    {
        Rect boxRect = new(position, DefaultSize);
        Widgets.DrawBoxSolid(boxRect, OARO_ColorLibrary.DimDarkBackground);

        Rect innerBoxRect = RectUtils.ContractedBy(boxRect, 2f);

        float verticalLineX = innerBoxRect.xMin + innerBoxRect.width * 0.6f - 2f;

        Rect leftRect = innerBoxRect;
        leftRect.xMax = verticalLineX;
        DrawLeftRect(leftRect, drawData);

        Rect rightRect = innerBoxRect;
        rightRect.xMin = verticalLineX + 2f;
        DrawRightRect(rightRect, drawData);

        OAFrame_Widgets.DrawLineVertical(new(verticalLineX, innerBoxRect.yMin), innerBoxRect.height, OARO_ColorLibrary.CommonOutline, 2);
        OAFrame_Widgets.DrawBox(boxRect, OARO_ColorLibrary.CommonOutline, thickness: 2);
    }

    /// <remarks>标准大约为(230f, 86f)</remarks>
    public void DrawLeftRect(Rect inRect, UIData_BranchSummary drawData)
    {
        Rect honorColorRect = inRect;
        honorColorRect.width = 5f;

        Rect innerRect = inRect;
        innerRect.xMin = honorColorRect.xMax; //标准大约为(226f, 86f)

        Rect branchIconRect = innerRect.LeftPart(0.45f);//标准大约为(100f, 86f)

        BranchHonorDef honorDef = drawData.Branch.HonorDef;
        if (honorDef is not null)
        {
            GUI.DrawTexture(honorColorRect, honorDef.HonorColorTex);

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

        this.TextStyle = new(guiColor: drawData.IsInAffectedRange ? Color.green : Color.white, font: GameFont.Small, anchor: TextAnchor.UpperLeft);
        OAFrame_Widgets.DrawLabel(innerRect, drawData.Distance.ToString("F0"), this.TextStyle);

        Rect infoRect = innerRect;
        infoRect.xMin = branchIconRect.xMax;

        Rect squadNameRect = infoRect.TopPart(0.35f);
        this.TextStyle = new(guiColor: drawData.Branch.Color, font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
        if (OAFrame_Widgets.DrawLabelEllipses(squadNameRect, drawData.SquadName, this.TextStyle))
        {
            TooltipHandler.TipRegion(squadNameRect, drawData.SquadName);
        }

        Rect stateRect = infoRect;
        stateRect.yMin = squadNameRect.yMax;

        Rect friendlyRect = stateRect.LeftHalf();
        Rect friendlyIconRect = GenUI.ContractedBy(friendlyRect.TopPart(0.7f), 4f);

        Rect friendlyStrRect = friendlyRect.BottomPart(0.3f);
        if (drawData.Branch.IsBranchOfType(BranchType.Friendly))
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
        OARO_UIUtility.DrawBranchStateIcon(workStateIconRect, drawData.Branch, expand: false);

        Rect workStateStrRect = workStateRect.BottomPart(0.3f);
        string workState = drawData.Branch.CurWorkStateDesc;
        this.TextStyle = new(guiColor: drawData.Branch.CurWorkState == WorkStateType.Idle ? Color.white : Color.green,
                             font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
        if (OAFrame_Widgets.DrawLabelEllipses(workStateStrRect, workState, this.TextStyle))
        {
            TooltipHandler.TipRegion(workStateStrRect, workState);
        }
    }

    public void DrawRightRect(Rect inRect, UIData_BranchSummary drawData)
    {
        float partRectHeight = inRect.height * (1f / 3f);
        Rect topRect = Rect.MinMaxRect(xmin: Mathf.Min(inRect.xMin + 24f, inRect.xMax - inRect.width * 0.9f),
                                       ymin: inRect.y,
                                       xmax: inRect.xMax,
                                       ymax: inRect.yMin + partRectHeight);
        this.TextStyle = new(font: GameFont.Small, anchor: TextAnchor.MiddleLeft);
        OAFrame_Widgets.DrawLabel(topRect, "OARO_AllCrewCountShortInfo".Translate(drawData.Squad.AllCrewCountInt), this.TextStyle);

        Rect centerRect = RectUtils.OffsetVertical(topRect, partRectHeight);
        Widgets.DrawBoxSolid(inRect.CenterSegmentOnY((1f / 3f)), OARO_ColorLibrary.MediumDarkBackground);
        OAFrame_Widgets.DrawLabel(centerRect, "OARO_BranchPotencyShortInfo".Translate(drawData.Branch.Potency.ToString("0.##")), this.TextStyle);

        Rect bottomRect = RectUtils.OffsetVertical(centerRect, partRectHeight);
        OAFrame_Widgets.DrawLabel(bottomRect, "OARO_BranchSupplyStateInfo".Translate(drawData.Branch.SupplyState), this.TextStyle);
    }
}

