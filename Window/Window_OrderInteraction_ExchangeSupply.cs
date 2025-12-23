using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
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
        MapRecommendationCount = RecommendationUtility.CurRecommendationCount(Map);
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

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(mainInnerX, mainInnerY + 55f, mainInnerWidth, 32f);
        Widgets.Label(reusedRect, OrderInteractionDefOf.OARO_ExchangeSupply.LabelCap);

        reusedRect.xMax -= 172f;
        reusedRect.xMin = reusedRect.xMax - 96f;
        OARO_WindowUtility.DrawRecommendationInfo(reusedRect, MapRecommendationCount, 4f);

        Rect outRect = new(mainInnerX + 172f, mainInnerY + 146f, mainInnerWidth - 172f - 172f + 16f, 488f);
        Rect viewRect = outRect;
        viewRect.width -= 16f;

        int columnCount = 5;

        float entryX = viewRect.xMin;
        float entryY = viewRect.yMin;
        float entryWidth = 100f;
        float entryHeight = 183f;
        float entryXInterval = (viewRect.width - 100f * columnCount) / 4f - 0.5f;
        float entryYInterval = 488f - 183f * 2f - 1f;

        viewRect.height = (ExchangeableSupplies.Count / columnCount + 1) * (entryY + entryYInterval);

        int column = 0;
        foreach (ExchangeableSupply supply in ExchangeableSupplies)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            if ((++column) >= columnCount)
            {
                entryX = viewRect.xMin;
                entryY += (entryHeight + entryYInterval);
                column = 0;
            }
            else
            {
                entryX += (entryWidth + entryXInterval);
            }

            DrawExchangeableSupply(entryRect, supply);
        }

        OARO_WindowUtility.ResetText();
    }

    private void DrawExchangeableSupply(Rect inRect, ExchangeableSupply supply)
    {
        GUI.DrawTexture(inRect, supplyBackground);
        Rect innerRect = inRect.ContractedBy(1f);

        Rect reusedRect = innerRect;
        reusedRect.height = 98f;
        reusedRect = OARO_WindowUtility.CenterRect(reusedRect, 75f, 75f);
        Widgets.ThingIcon(reusedRect, supply.thing, supply.stuff);

        reusedRect = innerRect;
        reusedRect.yMin += 100f;
        reusedRect.yMax = reusedRect.yMin + 54f;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, GenLabel.ThingLabel(supply.thing, supply.stuff, supply.count));

        reusedRect = innerRect;
        reusedRect.yMin += 154f;

        if (OARO_WindowUtility.TextButtonImageDisableable(
            reusedRect,
            $"x {supply.needRecommendation}",
            acceptance: supply.needRecommendation <= MapRecommendationCount,
            supplyButton,
            supplyButton_Down,
            doMouseoverSound: true))
        {
            RecommendationUtility.UseRecommendationOfMap(Map, supply.needRecommendation);
            MapRecommendationCount -= supply.needRecommendation;

            Thing thing = ThingMaker.MakeThing(supply.thing, supply.stuff);
            thing.stackCount = supply.count;
            OAFrame_DropPodUtility.DefaultDropThing([thing], Map, RatkinOrder.Faction);
        }
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/OrderInteraction/ExchangeSupply/OARO_MainBackground");
    private static readonly Texture2D supplyBackground = ContentFinder<Texture2D>.Get("UI/OrderInteraction/ExchangeSupply/OARO_SupplyBackground");

    private static readonly Texture2D supplyButton = ContentFinder<Texture2D>.Get("UI/OrderInteraction/ExchangeSupply/OARO_SupplyButton");
    private static readonly Texture2D supplyButton_Down = ContentFinder<Texture2D>.Get("UI/OrderInteraction/ExchangeSupply/OARO_SupplyButton_Down");
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