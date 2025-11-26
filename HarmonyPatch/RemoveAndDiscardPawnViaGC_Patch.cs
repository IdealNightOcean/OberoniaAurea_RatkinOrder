using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(WorldPawns), nameof(WorldPawns.RemoveAndDiscardPawnViaGC))]
internal static class RemoveAndDiscardPawnViaGC_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn p)
    {
        if (p.CanBeKnight())
        {
            KnightPawnsManager.Instance.DeregisterKnight(p);
        }
    }
}