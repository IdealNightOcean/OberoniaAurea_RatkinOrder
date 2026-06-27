using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue_Courage_Wariness : KnightVirtue
{
    protected HediffGiveParams giveParams;

    public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
    {
        base.Notify_KilledPawn(victim, dinfo);
        if (!this.Pawn.Spawned)
            return;

        foreach (Pawn p in this.Pawn.Map.mapPawns.FreeColonistsSpawned)
        {
            KnightChivalryUtility.KnightlyTalkStimulate(knight.KnightRecord, p);
        }
    }

    public override void Notify_Stimulate(Pawn recipient)
    {
        base.Notify_Stimulate(recipient);
        giveParams ??= Def.GetModExtension<ModExtension_GiveHediff>()?.giveParams;
        OAFrame_PawnUtility.GetOrAddHediffToPawn(recipient, giveParams);
    }
}
