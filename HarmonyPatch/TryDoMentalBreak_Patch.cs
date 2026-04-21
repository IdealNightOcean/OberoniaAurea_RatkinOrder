using HarmonyLib;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(MentalBreaker), nameof(MentalBreaker.TryDoMentalBreak))]
public class TryDoMentalBreak_Patch
{
    [HarmonyPostfix]
    public static void Postfix(bool __result, Pawn ___pawn, string reason, MentalBreakDef breakDef)
    {
        if (!__result || !Rand.Chance(0.1f))
            return;

        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(___pawn, out ResidentKnight residentKnight))
            return;

        KnightVirtueDef newVirtueDef = KnightVirtueUtility.GetRandomAvailableVirtue(residentKnight);
        if (newVirtueDef is null)
            return;

        int newVirtueLevel = KnightVirtueUtility.GetRandomNewVirtueLevel_MentalBreak(residentKnight);

        residentKnight.KnightVirtueHandler.TryAddVirtue(virtueDef: newVirtueDef,
                                                        level: newVirtueLevel,
                                                        reason: breakDef.LabelCap);
    }
}