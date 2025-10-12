using HarmonyLib;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Game), nameof(Game.ClearCaches))]
internal static class Game_ClearCaches_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        UniqueIDManager.ClearStaticCache();

        KnightPawnsManager.ClearStaticCache();
        RatkinOrderManager.ClearStaticCache();
        OrderLetterBox.ClearStaticCache();
        GlobalOrderInteractionManager.ClearStaticCache();

        ResidencyWorker_Deployment.ClearStaticCache();

        Thought_VisitingKnight.ClearStaticCache();
        ThoughtWorker_BranchChurch.ClearStaticCache();
    }
}