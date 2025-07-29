using HarmonyLib;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Game), nameof(Game.ClearCaches))]
public static class Game_ClearCaches_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        UniqueIDManager.ClearStaticCache();

        RatkinOrderManager.ClearStaticCache();
        OrderLetterBox.ClearStaticCache();
        OrderInteractionHandler.ClearStaticCache();

        ResidencyWorker_Deployment.ClearStaticCache();
        ThoughtWorker_BranchChurch.ClearStaticCache();
    }
}