using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(IncidentWorker_CaravanDemand), "CanFireNowSub")]
public static class CaravanDemand_CanFireNowSub_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref bool __result, IncidentParms parms)
    {
        if (!__result || parms.target is not Caravan caravan)
        {
            return;
        }

        foreach (Branch branch in BranchUtility.GetAllAffectedBranch(caravan.Tile))
        {
            if (branch.EffectTags.HasTag(KeyLibrary_EffectTag.CaravanPreventLoot))
            {
                __result = false;
                return;
            }
        }
    }
}