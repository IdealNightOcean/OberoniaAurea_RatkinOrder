using NightOcean.Utility;
using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame.DataLibrary;
using OberoniaAurea_Frame.UI;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_KnightVirtueProgressBar : UIDataDrawerBase<UIData_KnightVirtue>
{
    private Vector2 scrollPosition_TraitBar;
    private Vector2 scrollPosition_StarGroup;

    public UIDataDrawer_KnightVirtueProgressBar()
    {
        DefaultSize = new(600f, 70f);
        OutlineThickness = 0;
    }

    public override void DrawInner(Vector2 position)
    {
        Rect boxRect = new(position, DefaultSize);

        Rect virtueSummaryRect = boxRect;
        virtueSummaryRect.width *= 0.25f;
        DrawVirtueSummary(virtueSummaryRect);

        Rect traitBarRect = boxRect;
        traitBarRect.xMin = virtueSummaryRect.xMax;

        float traitItemRectWidth = traitBarRect.width * (1f / 3f) - 0.1f;
        int traitItemCount = DrawDataValid ? DrawData.VirtueDef.MaxLevel : 3;

        Rect traitBarViewRect = traitBarRect;
        traitBarViewRect.width = traitItemCount * traitItemRectWidth;
        Widgets.BeginScrollView(traitBarRect, ref scrollPosition_TraitBar, traitBarViewRect, showScrollbars: false);
        Rect traitItemRect = new(traitBarViewRect.xMin, traitBarViewRect.yMin, traitItemRectWidth, traitBarViewRect.height);
        for (int i = 1; i <= traitItemCount; i++)
        {
            DrawVirtueTrait(traitItemRect, i);
            traitItemRect.OffsetHorizontal(traitItemRectWidth);
        }
        Widgets.EndScrollView();
    }

    private void DrawVirtueSummary(Rect inRect)
    {
        OAFrame_Widgets.DrawBox(inRect, OARO_ColorLibrary.DeepInactive, thickness: 2);

        Rect innerRect = GenUI.ContractedBy(inRect, 2f);

        Rect topRect = innerRect.TopPart(0.55f);
        Rect labelRect = topRect.CenterSegmentOnX(0.8f);
        this.TextStyle = new(font: GameFont.Medium, anchor: TextAnchor.MiddleRight);
        OAFrame_Widgets.DrawLabel(labelRect, DrawDataValid ? DrawData.VirtueDef.LabelCap : "OARO_KnightVirtue_Unkown".Translate(), this.TextStyle);
        if (DrawDataValid)
        {
            this.TextStyle = new(font: GameFont.Small, anchor: TextAnchor.UpperLeft);
            OAFrame_Widgets.DrawLabel(labelRect, RomanNumeralHelper.ToRoman(DrawData.Virtue.Level), TextStyle);
        }

        OAFrame_Widgets.DrawLineHorizontal(new(innerRect.xMin + 4f, topRect.yMax), innerRect.width - 8f, OARO_ColorLibrary.DivideLine);

        Rect bottomRect = innerRect.BottomPart(0.45f);
        Rect starGroupRect = bottomRect.CenterSegmentOnX(0.8f);
        starGroupRect = starGroupRect.RightHalf();

        float starSize = Mathf.Min(starGroupRect.height, (starGroupRect.width - 8f) * 0.2f);
        OARO_UIUtility.DrawStarGroup(outRect: starGroupRect,
                                     starSize: new(starSize, starSize),
                                     interval: 2f,
                                     totalStarNum: DrawDataValid ? DrawData.VirtueDef.MaxLevel : 0,
                                     activeStarNum: DrawDataValid ? DrawData.Virtue.Level : 0,
                                     scrollPosition: ref scrollPosition_StarGroup);
    }

    private void DrawVirtueTrait(Rect inRect, int level)
    {
        Rect horizontalLineRect = inRect.BottomPartPixels(8f);
        OAFrame_Widgets.DrawLineHorizontal(horizontalLineRect.TopRightCorner(), horizontalLineRect.width, OARO_ColorLibrary.DeepInactive, thickness: 2);
        OAFrame_Widgets.DrawLineHorizontal(new(horizontalLineRect.xMin, horizontalLineRect.yMax - 2f), horizontalLineRect.width, OARO_ColorLibrary.DeepInactive, thickness: 2);

        bool hasUnlocked = DrawDataValid && level <= DrawData.Virtue.Level;
        Color traitColor = hasUnlocked ? DrawData.VirtueDef.chivalry.color : Color.black;

        Rect horizontalLineInnerRect = GenUI.ContractedBy(horizontalLineRect, 2f);
        Widgets.DrawBoxSolid(horizontalLineInnerRect, traitColor);

        Rect traitIconBoxRect = new(inRect.x, inRect.y, inRect.width * 0.625f, inRect.height * 0.6f);
        traitIconBoxRect = GenUI.CenteredOnXIn(traitIconBoxRect, inRect);
        OAFrame_Widgets.DrawBox(traitIconBoxRect, Color.black);

        Rect traitIconInnerBoxRect = GenUI.ContractedBy(traitIconBoxRect, 1f);
        Rect traitIconRect = new(0f, 0f, traitIconInnerBoxRect.height - 4f, traitIconInnerBoxRect.height - 4f);
        traitIconRect = traitIconRect.CenteredIn(traitIconInnerBoxRect);

        if (hasUnlocked)
        {
            KnightVirtueTraitDef trait = DrawData.Virtue.GetTraitOfLevel(level);
            OAFrame_Widgets.DrawTextureWithColor(traitIconInnerBoxRect, OARO_IconLibrary.Placeholer, traitColor);
            GUI.DrawTexture(traitIconRect, OARO_IconLibrary.Placeholer, ScaleMode.ScaleToFit);
            TooltipHandler.TipRegion(traitIconInnerBoxRect, () => DrawData?.Virtue?.GetTraitOfLevel(level)?.description ?? KeyLibrary_Misc.ErrorTipWithColor, uniqueId: 324257570);
        }
        else if (DrawDataValid && level == DrawData.Virtue.Level + 1)
        {
            GUI.DrawTexture(traitIconRect, OARO_IconLibrary.PlusSign, ScaleMode.ScaleToFit);

            Rect tipCycleRect = new(0f, 0f, 10f, 10f)
            {
                center = traitIconInnerBoxRect.TopRightCorner()
            };
            OAFrame_Widgets.DrawTextureWithColor(tipCycleRect, OARO_IconLibrary.Placeholer, Color.yellow, ScaleMode.ScaleToFit);
        }
        else
        {
            this.TextStyle = new(font: GameFont.Medium, anchor: TextAnchor.MiddleCenter);
            OAFrame_Widgets.DrawLabel(traitIconInnerBoxRect, "OARO_NotUnlocked".Translate(), this.TextStyle);
        }

        Rect verticalLineRect = new(0f, traitIconBoxRect.yMax, 6f, horizontalLineInnerRect.yMin - traitIconBoxRect.yMax);
        verticalLineRect = GenUI.CenteredOnXIn(verticalLineRect, inRect);

        OAFrame_Widgets.DrawLineVertical(verticalLineRect.TopRightCorner(), verticalLineRect.height, Color.black);
        OAFrame_Widgets.DrawLineVertical(new(verticalLineRect.xMax - 2f, verticalLineRect.yMin), verticalLineRect.height, Color.black);
        Rect verticalLineInnerRect = GenUI.ContractedBy(verticalLineRect, 2f);
        Widgets.DrawBoxSolid(verticalLineInnerRect, traitColor);
    }
}
