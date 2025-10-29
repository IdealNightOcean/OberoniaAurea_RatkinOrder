using HarmonyLib;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(PlayDataLoader), nameof(PlayDataLoader.HotReloadDefs))]
public static class HotReloadDefs_Patch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        LongEventHandler.QueueLongEvent(delegate
        {
            DeepProfiler.Start($"{nameof(OrderDefDataBase)}.{nameof(OrderDefDataBase.ClearStaticCache)}()");
            try
            {
                OrderDefDataBase.ClearStaticCache();
            }
            finally
            {
                DeepProfiler.End();
            }

        }, $"Clear {nameof(OberoniaAurea.RatkinOrder)}.{nameof(OrderDefDataBase)}", doAsynchronously: false, null);
    }
}