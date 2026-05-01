using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class OARO_MapUtility
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

    /// <summary>
    /// 获取合适的玩家基地地图
    /// 优先级：当前地图 > 骑士大厅所在地图 > 其它地图
    /// </summary>
    /// <param name="forQuest">为任务获取</param>
    /// <param name="canBeSpace">能否位于太空</param>
    /// <returns>得到的地图，无符合条件则返回<see langword="null"/></returns>
    public static Map GetRationalPlayerHomeMap(bool forQuest, bool canBeSpace = false)
    {
        Map map = Find.CurrentMap;
        if (map is not null && map.IsPlayerHome && (canBeSpace || !map.Tile.LayerDef.isSpace))
        {
            return map;
        }

        map = OrderStationHandler.Instance.MainOrderCodePedestal?.MapHeld;
        if (map is not null && map.IsPlayerHome && (canBeSpace || !map.Tile.LayerDef.isSpace))
        {
            return map;
        }

        if (forQuest)
        {
            return QuestGen_Get.GetMap(canBeSpace: canBeSpace);
        }
        else
        {
            if (canBeSpace)
            {
                return Find.AnyPlayerHomeMap;
            }
            else
            {
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    if (!maps[i].Tile.LayerDef.isSpace && maps[i].IsPlayerHome)
                    {
                        return maps[i];
                    }
                }

                if (ModsConfig.OdysseyActive)
                {
                    for (int j = 0; j < maps.Count; j++)
                    {
                        if (!maps[j].Tile.LayerDef.isSpace && GravshipUtility.PlayerHasGravEngine(maps[j]))
                        {
                            return maps[j];
                        }
                    }
                }

                return null;
            }
        }
    }
}
