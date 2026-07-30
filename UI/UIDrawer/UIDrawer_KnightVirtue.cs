using NightOcean.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDrawer_KnightVirtue : UIDrawerBase
{
    public ResidentKnight Knight { get; }
    public KnightVirtue Virtue { get; }

    public UIDrawer_KnightVirtue(ResidentKnight knight, KnightVirtue virtue)
    {
        this.Knight = knight;
        this.Virtue = virtue;
    }

    public override void Draw(Vector2 position)
    {
        Rect boxRect = new(position.x, position.y, DefaultSize.x, DefaultSize.y);
        Rect innerBoxRect = GenUI.ContractedBy(boxRect, 1f);
        Widgets.DrawBoxSolid(boxRect, OARO_ColorLibrary.MediumDarkBackground);

        float verticalLineX = innerBoxRect.xMin + innerBoxRect.width * (1f / 3f);
        float horizontalLine2Y = innerBoxRect.yMin + innerBoxRect.height * (1f / 3f);
        float horizontalLine3Y = innerBoxRect.yMin + innerBoxRect.height * (2f / 3f);

        Rect labelRect = innerBoxRect;
        labelRect.xMax = verticalLineX;

        this.TextStyle = new(GameFont.Medium, TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(labelRect, this.Virtue.Def.LabelCap, this.TextStyle);

        float levelRectHeight = innerBoxRect.height / 3f;
        for (int i = 0; i < this.Virtue.Def.maxLevel; i++)
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
                 width: 1f);

        Widgets.DrawLine(start: new Vector2(verticalLineX, horizontalLine2Y),
                 end: new Vector2(innerBoxRect.xMax, horizontalLine2Y),
                 color: OARO_ColorLibrary.CommonOutline,
                 width: 1f);

        Widgets.DrawLine(start: new Vector2(verticalLineX, horizontalLine3Y),
                 end: new Vector2(innerBoxRect.xMax, horizontalLine3Y),
                 color: OARO_ColorLibrary.CommonOutline,
                 width: 1f);
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

        Rect traitInfoRect = inRect;
        traitInfoRect.xMin = verticalLineX + 1f;

        if (this.Virtue.Level < level)
        {
            this.TextStyle = new(GameFont.Medium, TextAnchor.MiddleCenter);
            OAFrame_Widgets.DrawLabel(traitInfoRect, "OARO_NotUnlocked".Translate(), this.TextStyle);
            return;
        }

        KnightVirtueTraitDef virtueTrait = this.Virtue.GetTraitOfLevel(level);
        if (virtueTrait is not null)
        {
            DarwVirtueTaritInfo(traitInfoRect, virtueTrait);
            return;
        }

        if (level < this.Virtue.Def.traitGroups.Count + 1)
        {
            Rect traitInfoButRect = traitInfoRect.CenterSegment(0.6f, 0.6f);
            if (OARO_Widgets.DefaultTextButton(traitInfoButRect, "OARO_SelectVirtueTrait".Translate()))
            {
                Window_VirtueTaritSelection taritSelectionWin = new(this.Knight, this.Virtue, level);
                Find.WindowStack.Add(taritSelectionWin);
            }
        }
        else
        {
            this.TextStyle = new(OARO_ColorLibrary.DimInactive, GameFont.Medium, TextAnchor.MiddleCenter);
            OAFrame_Widgets.DrawLabel(traitInfoRect, "None".Translate(), this.TextStyle);
        }
    }

    private void DarwVirtueTaritInfo(Rect inRect, KnightVirtueTraitDef virtueTrait)
    {
        Rect iconRect = inRect;
        iconRect.width *= 0.24f;


        Rect descRect = inRect;
        descRect.xMin = iconRect.xMax;
        this.TextStyle = new(GameFont.Small, TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(descRect, virtueTrait.description, this.TextStyle);
    }

    private void DarwVirtueTaritSelection(Rect inRect, int level)
    {
        Rect infoRect = inRect.CenterSegmentOnX(0.85f);

        Rect labelRect = infoRect.CenterSegmentOnX(0.33f);
        this.TextStyle = new(GameFont.Small, TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(labelRect, "OARO_SelectVirtueTrait".Translate(), this.TextStyle);

        IReadOnlyList<KnightVirtueTraitDef> traitOptions = this.Virtue.Def.GetTraitOptionsForLevel(level);
        Rect opt1Rect = infoRect.LeftPart(0.33f);
        KnightVirtueTraitDef optTrait1 = traitOptions.ElementAtOrDefault(0) ?? OARO_ModDefOf.OARO_BaseTrait;
        TooltipHandler.TipRegion(opt1Rect, optTrait1.description);
        if (OARO_Widgets.DefaultTextButton(opt1Rect, string.Empty))
        {
            this.Virtue.TrySelectTraitForLevel(optTrait1, level);
        }

        KnightVirtueTraitDef optTrait2 = traitOptions.ElementAtOrDefault(1) ?? OARO_ModDefOf.OARO_BaseTrait;
        Rect opt2Rect = infoRect.RightPart(0.33f);
        TooltipHandler.TipRegion(opt2Rect, optTrait2.description);
        if (OARO_Widgets.DefaultTextButton(opt2Rect, string.Empty))
        {
            this.Virtue.TrySelectTraitForLevel(optTrait2, level);
        }

    }
}
