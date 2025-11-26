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
        GameComponent_RatkinOrder.ClearStaticCache();

        Thought_VisitingKnight.ClearStaticCache();
        ThoughtWorker_BranchChurch.ClearStaticCache();
    }
}