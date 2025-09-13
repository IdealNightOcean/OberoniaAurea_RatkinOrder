using RimWorld.QuestGen;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class MapUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MapComponent_RatkinOrder GetOrderMapComp(this Map map)
    {
        if (map?.IsPlayerHome ?? false)
        {
            return map.GetComponent<MapComponent_RatkinOrder>();
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Map GetRationalPlayerHomeMap(bool forQuest, bool canBeSpace = false)
    {
        Map map = Find.CurrentMap;
        if (map is not null && map.IsPlayerHome && (canBeSpace || !map.Tile.LayerDef.isSpace))
        {
            return map;
        }

        map = OrderInteractionHandler.MainOrderCodePedestal?.MapHeld;
        if (map is not null && map.IsPlayerHome && (canBeSpace || !map.Tile.LayerDef.isSpace))
        {
            return map;
        }

        return forQuest ? QuestGen_Get.GetMap(canBeSpace: canBeSpace) : Find.AnyPlayerHomeMap;
    }
}
