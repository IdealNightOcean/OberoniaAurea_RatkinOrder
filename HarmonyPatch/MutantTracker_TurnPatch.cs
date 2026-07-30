using HarmonyLib;
using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Pawn_MutantTracker), nameof(Pawn_MutantTracker.Turn))]
internal class MutantTracker_TurnPatch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn ___pawn)
    {
        if (!___pawn.Faction.IsPlayerSafe() || !ResidentPawnsManager.Instance.IsResidentKnight(___pawn))
            return;

        ResidentPawnsManager.Instance.DeregisterKnight(___pawn, ResidentKnightRemovalReason.Player);
        if (KnightPawnsManager.Instance.TryGetKnightRecord(___pawn, out KnightRecord record))
        {
            RatkinOrder ratkinOrder = record.RatkinOrder;
            if (ratkinOrder is null)
            {
                return;
            }
            ratkinOrder.EsteemHandler.AdjustEsteem(-15, byPlayer: true, reason: "OARO_TurnKnightToMutant".Translate());
            ratkinOrder.RelationshipKindOffsetBy(-1, reason: "OARO_TurnKnightToMutant".Translate(), sendLetter: true);

            ratkinOrder.Faction?.TryAffectGoodwillWith(Faction.OfPlayer, -20);

            record.Branch?.SetFriendly(active: false);
        }
    }
}
