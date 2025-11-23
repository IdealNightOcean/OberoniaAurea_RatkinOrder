using HarmonyLib;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Faction), nameof(Faction.Notify_RelationKindChanged))]
internal class Faction_RelationKindChanged_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Faction __instance, Faction other)
    {
        if (other.IsPlayerSafe())
        {
            RatkinOrderManager.Instance.GetRatkinOrderForFaction(__instance)?.EsteemHandler.Notify_FactionRelationChanged(__instance.PlayerRelationKind);
        }
    }
}