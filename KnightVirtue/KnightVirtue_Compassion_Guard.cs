using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue_Compassion_Guard : KnightVirtue, ITickInterval
{
    private HediffGiveParams giveParams;

    public void TickInterval(int delta)
    {
        if (!knight.Pawn.Drafted)
            return;

        if (knight.Pawn.IsHashIntervalTick(delta, 120))
        {
            giveParams ??= Def.GetModExtension<ModExtension_GiveHediff>()?.giveParams;
            knight.Pawn.GetOrAddHediff(giveParams);
        }
    }
}
