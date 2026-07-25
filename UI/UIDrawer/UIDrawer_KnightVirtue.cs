using OberoniaAurea_Frame;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDrawer_KnightVirtue : IUIDrawer
{
    public Vector2 InitSize => new(362f, 182f);
    public TextStyle_GameFont GameFontText { get; set; } = TextStyle_GameFont.DefaultStyle;
    public TextStyle_FontSize FontSizeText { get; set; } = TextStyle_FontSize.DefaultStyle;

    public ResidentKnight Knight { get; }
    public KnightVirtue Virtue { get; }

    public UIDrawer_KnightVirtue(ResidentKnight knight, KnightVirtue virtue)
    {
        this.Knight = knight;
        this.Virtue = virtue;
    }

    public void Draw(Vector2 position)
    {
        Rect boxRect = new(position.x, position.y, InitSize.x, InitSize.y);
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

        GameFontText = new(GameFont.Medium, TextAnchor.MiddleCenter);
        GameFontText.DrawLabel(labelRect, this.Virtue.Def.LabelCap);

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
            GameFontText = new(GameFont.Medium, TextAnchor.MiddleCenter);
            GameFontText.DrawLabel(traitInfoRect, "OARO_NotUnlocked".Translate());
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
            GameFontText = new(OARO_ColorLibrary.DimInactive, GameFont.Medium, TextAnchor.MiddleCenter);
            GameFontText.DrawLabel(traitInfoRect, "None".Translate());
        }
    }

    private void DarwVirtueTaritInfo(Rect inRect, KnightVirtueTraitDef virtueTrait)
    {
        Rect iconRect = inRect;
        iconRect.width *= 0.24f;


        Rect descRect = inRect;
        descRect.xMin = iconRect.xMax;
        GameFontText = new(GameFont.Small, TextAnchor.MiddleCenter);
        GameFontText.DrawLabel(descRect, virtueTrait.description);
    }

    private void DarwVirtueTaritSelection(Rect inRect, int level)
    {

    }
}

public class Window_VirtueTaritSelection : OrderWindowBase
{
    public ResidentKnight Knight { get; }
    public KnightVirtue Virtue { get; }
    public int Level { get; }
    public KnightVirtueTraitGroups TraitGroup { get; }

    public Window_VirtueTaritSelection(ResidentKnight knight, KnightVirtue virtue, int level) : base()
    {
        this.Knight = knight;
        this.Virtue = virtue;
        this.Level = level;
        this.TraitGroup = virtue.Def.traitGroups[level - 1];
    }

    public override void DoWindowContents(Rect inRect)
    {
        throw new NotImplementedException();
    }

}