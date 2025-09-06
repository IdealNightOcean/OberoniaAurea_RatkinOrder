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
}
