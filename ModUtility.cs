using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;


[StaticConstructorOnStartup]
public static class ModUtility
{
    public const string RatkinOrderStoreAs = "ratkinOrder";
    public const string Branch = "branch";
    public const string SquadStoreAs = "squad";

    public static bool AnyThingOfDef(Room room, ThingDef thingDef)
    {
        List<Region> regions = room.Regions;
        for (int i = 0; i < regions.Count; i++)
        {
            if (regions[i].ListerThings.AnyThingWithDef(thingDef))
            {
                return true;
            }
        }
        return false;
    }

    public static void DrawRelatedOrderInfo(Rect rect, RatkinOrder order, ref float curY)
    {
        Text.Anchor = TextAnchor.LowerRight;
        curY += 10f;
        FactionRelationKind playerRelationKind = order.Faction.PlayerRelationKind;
        string text = order.Name.CapitalizeFirst() + "\n" + "goodwill".Translate().CapitalizeFirst() + ": " + order.Esteem.ToStringWithSign();
        GUI.color = Color.gray;
        Rect textRect = new(rect.x, curY, rect.width, Text.CalcHeight(text, rect.width));
        Widgets.Label(textRect, text);
        curY += textRect.height;
        GUI.color = playerRelationKind.GetColor();
        Rect rect3 = new(textRect.x, curY - 7f, textRect.width, 25f);
        Widgets.Label(rect3, playerRelationKind.GetLabelCap());
        curY += rect3.height;
        GUI.color = Color.white;
        GenUI.ResetLabelAlign();
    }
}