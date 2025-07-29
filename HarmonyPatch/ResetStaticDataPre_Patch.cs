using HarmonyLib;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Game), "ClearCaches")]
public static class Game_ClearCaches_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        ResidencyWorker_Deployment.ClearStaticCache();
        ThoughtWorker_BranchChurch.ClearStaticCache();
    }
}