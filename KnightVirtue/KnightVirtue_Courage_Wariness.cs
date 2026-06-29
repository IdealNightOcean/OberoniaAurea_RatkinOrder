using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 勇气·戒心
/// </summary>
public class KnightVirtue_Courage_Wariness : KnightVirtueWithComps
{
    public override void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
    {
        base.Notify_KilledPawn(victim, dinfo);
        if (!this.Pawn.Spawned)
            return;

        foreach (Pawn p in this.Pawn.Map.mapPawns.FreeColonistsSpawned)
        {
            KnightChivalryUtility.KnightStimulate(knight.KnightRecord, p);
        }
    }
}