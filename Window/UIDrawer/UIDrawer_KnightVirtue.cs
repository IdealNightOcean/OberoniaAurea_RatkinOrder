using OberoniaAurea_Frame;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDrawer_KnightVirtue
{
    public ResidentKnight Knight { get; }
    public KnightVirtue Virtue { get; }

    public void Draw(Rect inRect)
    {
        Rect boxRect = new(inRect.x, inRect.y, 362f, 182f);
        Widgets.DrawBoxSolidWithOutline(rect: boxRect,
                                        solidColor: OARO_ColorLibrary.MediumDarkBackground,
                                        outlineColor: OARO_ColorLibrary.CommonOutline);

        Rect innerBoxRect = boxRect.ContractedBy(1f);

        float verticalLineX = innerBoxRect.xMin + innerBoxRect.width * (1f / 3f);
        Widgets.DrawLine(start: new Vector2(verticalLineX, innerBoxRect.yMin),
                         end: new Vector2(verticalLineX, innerBoxRect.yMax),
                         color: OARO_ColorLibrary.CommonOutline,
                         width: 1f);

        float horizontalLine2Y = innerBoxRect.yMin + innerBoxRect.height * (1f / 3f);
        Widgets.DrawLine(start: new Vector2(verticalLineX, horizontalLine2Y),
                         end: new Vector2(innerBoxRect.xMax, horizontalLine2Y),
                         color: OARO_ColorLibrary.CommonOutline,
                         width: 1f);

        float horizontalLine3Y = innerBoxRect.yMin + innerBoxRect.height * (2f / 3f);
        Widgets.DrawLine(start: new Vector2(verticalLineX, horizontalLine3Y),
                         end: new Vector2(innerBoxRect.xMax, horizontalLine3Y),
                         color: OARO_ColorLibrary.CommonOutline,
                         width: 1f);



        Rect labelRect = innerBoxRect;
        labelRect.xMax = verticalLineX;

        OAFrame_UIUtility.DrawLabel(labelRect, this.Virtue.Def.LabelCap, GameFont.Medium, TextAnchor.MiddleCenter);

        float levelRectHeight = innerBoxRect.height / 3f;
        for (int i = 0; i < this.Virtue.Def.maxLevel; i++)
        {
            Rect levelRect = innerBoxRect;
            levelRect.yMin = innerBoxRect.yMin + i * levelRectHeight;
            levelRect.yMax = levelRect.yMin + levelRectHeight;

            Widgets.DrawLine(start: new Vector2(verticalLineX, levelRect.yMax),
                             end: new Vector2(innerBoxRect.xMax, levelRect.yMax),
                             color: OARO_ColorLibrary.CommonOutline,
                             width: 1f);

            DrawVirtueTrait(inRect: levelRect, level: i + 1);
        }
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
            OAFrame_UIUtility.DrawLabel(traitInfoRect, "OARO_NotUnlocked".Translate(), GameFont.Medium, TextAnchor.MiddleCenter);
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
            DarwVirtueTaritSelection(traitInfoRect, level);
        }
        else
        {
            OAFrame_UIUtility.DrawLabel(traitInfoRect, "None".Translate(), OARO_ColorLibrary.DimInactive, GameFont.Medium, TextAnchor.MiddleCenter);
        }
    }

    private void DarwVirtueTaritInfo(Rect inRect, KnightVirtueTraitDef virtueTrait)
    {
        throw new NotImplementedException();
    }

    private void DarwVirtueTaritSelection(Rect inRect, int level)
    {
        KnightVirtueTraitGroups traitGroup = this.Virtue.Def.traitGroups[level - 1];
        throw new NotImplementedException();
    }
}
