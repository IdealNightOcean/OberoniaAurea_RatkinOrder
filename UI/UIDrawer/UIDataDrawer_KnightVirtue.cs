using NightOcean.Utility;
using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame.UI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_KnightVirtue : UIDataDrawerBase<UIData_KnightVirtue>
{
    public UIDataDrawer_KnightVirtue()
    {
        DrawSize = new(260f, 94f);
        OutlineThickness = 1;
    }

    public override void DrawInner(Vector2 position)
    {
        Rect boxRect = new(position, DrawSize);
        Rect innerBoxRect = GenUI.ContractedBy(boxRect, OutlineThickness);
        Widgets.DrawBoxSolid(boxRect, OARO_ColorLibrary.MediumDarkBackground);

        float verticalLineX = innerBoxRect.xMin + innerBoxRect.width * (1f / 3f);
        float horizontalLine2Y = innerBoxRect.yMin + innerBoxRect.height * (1f / 3f);
        float horizontalLine3Y = innerBoxRect.yMin + innerBoxRect.height * (2f / 3f);

        Rect labelRect = innerBoxRect;
        labelRect.xMax = verticalLineX;

        TextStyle = new(GameFont.Medium, TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(labelRect, DrawData.VirtueDef.LabelCap, TextStyle);

        float levelRectHeight = innerBoxRect.height / 3f;
        for (int i = 0; i < DrawData.Virtue.Def.MaxLevel; i++)
        {
            Rect levelRect = innerBoxRect;
            levelRect.yMin = innerBoxRect.yMin + i * levelRectHeight;
            levelRect.yMax = levelRect.yMin + levelRectHeight;

            DrawVirtueTrait(inRect: levelRect, level: i + 1);

            OAFrame_Widgets.DrawLineHorizontal(new Vector2(levelRect.xMin, levelRect.yMax), levelRect.width, OARO_ColorLibrary.CommonOutline);
        }

        OAFrame_Widgets.DrawBox(boxRect, OARO_ColorLibrary.CommonOutline, thickness: 1);

        Widgets.DrawLine(start: new Vector2(verticalLineX, innerBoxRect.yMin),
                 end: new Vector2(verticalLineX, innerBoxRect.yMax),
                 color: OARO_ColorLibrary.CommonOutline,
                 width: OutlineThickness);

        Widgets.DrawLine(start: new Vector2(verticalLineX, horizontalLine2Y),
                 end: new Vector2(innerBoxRect.xMax, horizontalLine2Y),
                 color: OARO_ColorLibrary.CommonOutline,
                 width: OutlineThickness);

        Widgets.DrawLine(start: new Vector2(verticalLineX, horizontalLine3Y),
                 end: new Vector2(innerBoxRect.xMax, horizontalLine3Y),
                 color: OARO_ColorLibrary.CommonOutline,
                 width: OutlineThickness);
    }

    private void DrawVirtueTrait(Rect inRect, int level)
    {
        float verticalLineX = inRect.xMin + inRect.width * 0.15f;
        Widgets.DrawLine(start: new Vector2(verticalLineX, inRect.yMin),
                         end: new Vector2(verticalLineX, inRect.yMax),
                         color: OARO_ColorLibrary.CommonOutline,
                         width: 1f);

        Rect levelLabeleRect = inRect;
        levelLabeleRect.xMax = verticalLineX;
        TextStyle = new(GameFont.Medium, TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(levelLabeleRect, RomanNumeralHelper.ToRoman(level), TextStyle);

        Rect traitInfoRect = inRect;
        traitInfoRect.xMin = verticalLineX + 1f;

        if (DrawData.Virtue.Level < level)
        {
            TextStyle = new(GameFont.Medium, TextAnchor.MiddleCenter);
            OAFrame_Widgets.DrawLabel(traitInfoRect, "OARO_NotUnlocked".Translate(), TextStyle);
            return;
        }

        KnightVirtueTraitDef virtueTrait = DrawData.Virtue.GetTraitOfLevel(level);
        if (virtueTrait is not null)
        {
            DrawVirtueTaritInfo(traitInfoRect, virtueTrait);
            return;
        }

        if (level <= DrawData.VirtueDef.MaxLevel)
        {
            if (DrawData.Virtue.Def.GetTraitOptionsForLevel(level).Count <= 2)
            {
                DrawVirtueTaritSelection(traitInfoRect, level);
            }
            else
            {
                Rect traitInfoButRect = traitInfoRect.CenterSegment(0.6f, 0.6f);
                if (OARO_Widgets.DefaultTextButton(traitInfoButRect, "OARO_KnightVirtue_SelectTrait".Translate()))
                {
                    Window_VirtueTaritSelection taritSelectionWin = new(DrawData, level);
                    Find.WindowStack.Add(taritSelectionWin);
                }
            }
        }
        else
        {
            TextStyle = new(OARO_ColorLibrary.DimInactive, GameFont.Medium, TextAnchor.MiddleCenter);
            OAFrame_Widgets.DrawLabel(traitInfoRect, "None".Translate(), TextStyle);
        }
    }

    private void DrawVirtueTaritInfo(Rect inRect, KnightVirtueTraitDef virtueTrait)
    {
        Rect iconRect = inRect;
        iconRect.width *= 0.24f;


        Rect descRect = inRect;
        descRect.xMin = iconRect.xMax;
        TextStyle = new(GameFont.Small, TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(descRect, virtueTrait.description, TextStyle);
    }

    private void DrawVirtueTaritSelection(Rect inRect, int level)
    {
        Rect infoRect = inRect.CenterSegmentOnX(0.85f);

        Rect labelRect = infoRect.CenterSegmentOnX(0.33f);
        TextStyle = new(GameFont.Small, TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(labelRect, "OARO_KnightVirtue_SelectTrait".Translate(), TextStyle);

        IReadOnlyList<KnightVirtueTraitDef> traitOptions = DrawData.VirtueDef.GetTraitOptionsForLevel(level);
        Rect opt1Rect = infoRect.LeftPart(0.33f);
        KnightVirtueTraitDef optTrait1 = traitOptions.ElementAtOrDefault(0) ?? OARO_ModDefOf.OARO_BaseTrait;
        TooltipHandler.TipRegion(opt1Rect, optTrait1.description);
        if (OARO_Widgets.DefaultTextButton(opt1Rect, string.Empty))
        {
            DrawData.Virtue.TrySelectTraitForLevel(optTrait1, level);
        }

        KnightVirtueTraitDef optTrait2 = traitOptions.ElementAtOrDefault(1) ?? OARO_ModDefOf.OARO_BaseTrait;
        Rect opt2Rect = infoRect.RightPart(0.33f);
        TooltipHandler.TipRegion(opt2Rect, optTrait2.description);
        if (OARO_Widgets.DefaultTextButton(opt2Rect, string.Empty))
        {
            DrawData.Virtue.TrySelectTraitForLevel(optTrait2, level);
        }

    }
}
