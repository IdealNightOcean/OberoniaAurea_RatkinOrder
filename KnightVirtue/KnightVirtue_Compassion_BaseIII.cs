using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 援护·基础III
/// </summary>
public class KnightVirtue_Compassion_BaseIII : KnightVirtueWithComps, ITickInterval
{
    private static readonly List<Pawn> TempInjuredColonists = [];
    private static readonly List<Pawn> TempTargetColonists = new(2);

    public void TickInterval(int delta)
    {
        if (this.Pawn.Spawned && this.Pawn.IsHashIntervalTick(60000, delta))
        {
            List<Pawn> potentialPawns = this.Pawn.Map.mapPawns.FreeColonistsSpawned;
            potentialPawns.Remove(this.Pawn);

            if (ValueCacheManager.Instance.InjuredColonistsCount.GetCachedResult(this.Pawn.Map) > 0)
            {
                TempInjuredColonists.Clear();
                for (int i = potentialPawns.Count - 1; i >= 0; i--)
                {
                    Pawn p = potentialPawns[i];
                    if (!OARO_PawnUtility.IsHealthyPawn(p))
                    {
                        TempInjuredColonists.Add(p);
                        potentialPawns.Remove(p);
                    }
                }
            }

            TempTargetColonists.Clear();
            if (!TempInjuredColonists.NullOrEmpty())
                TempTargetColonists.AddRange(TempInjuredColonists.TakeRandom(2));

            if (TempTargetColonists.Count < 2)
                TempTargetColonists.AddRange(potentialPawns.TakeRandom(2 - TempTargetColonists.Count));

            foreach (Pawn p in TempTargetColonists)
                KnightChivalryUtility.KnightStimulate(knight.KnightRecord, p);

            TempInjuredColonists.Clear();
            TempTargetColonists.Clear();
        }
    }
}
