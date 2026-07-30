using HarmonyLib;
using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute))]
internal static class IncidentWorker_TryExecute_Patch
{
    [HarmonyPostfix]
    public static void Postfix(bool __result, IncidentDef ___def, IncidentParms parms)
    {
        if (!__result || !RatkinOrderSettings.EnableAIContent)
            return;
        if (!Rand.Chance(RatkinOrderSettings.AIIncidentConcernLetterChance))
            return;
        if (!IncidentValidator(___def))
            return;

        Faction parmsFaction = parms.faction;
        List<Branch> allAvailableBranches = BranchUtility.GetAllAvailableBranches(b => b.IsBranchOfType(Branch.BranchType.Friendly) && b.RatkinOrder.Faction != parmsFaction);
        if (allAvailableBranches.NullOrEmpty())
            return;

        Branch targetBranch = allAvailableBranches.RandomElement();
        AIInteractionUtility.SendIncidentConcernLetter(targetBranch, ___def, parms);
    }

    private static bool IncidentValidator(IncidentDef def)
    {
        if (def.category == IncidentCategoryDefOf.ThreatSmall || def.category == IncidentCategoryDefOf.ThreatBig)
            return true;

        if (def.letterDef is null)
            return false;

        if (def.letterDef == LetterDefOf.ThreatSmall || def.letterDef == LetterDefOf.ThreatBig)
            return true;

        return false;
    }
}