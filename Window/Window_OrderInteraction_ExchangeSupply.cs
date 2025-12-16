using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Window_OrderInteraction_ExchangeSupply : OrderWindowBase
{
    public override Vector2 InitialSize => new(1402f, 827f);

    private RatkinOrder RatkinOrder { get; }
    private Map Map { get; }

    private int MapRecommendationCount { get; set; }
    public Action PostCloseAction { get; set; }

    private IReadOnlyList<ExchangeableSupply> ExchangeableSupplies { get; }

    public Window_OrderInteraction_ExchangeSupply(RatkinOrder ratkinOrder, Map map)
    {
        ExchangeableSupplies = OrderInteractionDefOf.OARO_ExchangeSupply.GetModExtension<ExchangeableSupply_Extension>()?.Supplies ?? throw new NullReferenceException(nameof(ExchangeableSupplies));

        RatkinOrder = ratkinOrder;
        Map = map;
        MapRecommendationCount = RecommendationUtility.CurRecommendationOfMap(RatkinOrder, Map);
    }

    public override void PostClose()
    {
        base.PostClose();
        PostCloseAction?.Invoke();
        PostCloseAction = null;
    }



    public override void DoWindowContents(Rect inRect)
    {
        GUI.DrawTexture(inRect, mainBackground);
        Rect mainRect = OARO_WindowUtility.CenterRect(inRect, 1308f, 733f);

        Rect mainInnerRect = mainRect.ContractedBy(2f);
        if (OARO_WindowUtility.DrawCloseX(mainInnerRect))
        {
            Close();
            return;
        }

        float mainInnerX = mainInnerRect.xMin;
        float mainInnerY = mainInnerRect.yMin;
        float mainInnerWidth = mainInnerRect.width;

        Rect reusedRect = new(mainInnerX, mainInnerY + 55f, mainInnerWidth, 40f);
        Widgets.Label(reusedRect, OrderInteractionDefOf.OARO_ExchangeSupply.LabelCap);

        Rect outRect = new(mainInnerX + 172f, mainInnerY + 146f, mainInnerWidth - 172f - 172f + 16f, 488f);
        Rect viewRect = outRect;
        viewRect.width -= 16f;


    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_MainBackground");

}

public class ExchangeableSupply_Extension : DefModExtension
{
    protected List<ExchangeableSupply> supplies;
    public IReadOnlyList<ExchangeableSupply> Supplies => supplies;
}


public class ExchangeableSupply
{
    public ThingDef thing;
    public int count;

    public int needRecommendation;
    public ThingDef stuff;
}