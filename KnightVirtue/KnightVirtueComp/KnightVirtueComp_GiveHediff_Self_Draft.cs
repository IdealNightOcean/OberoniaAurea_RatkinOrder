using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GiveHediff_Self_Draft : KnightVirtueComp, ITickInterval
{
    public KnightVirtueCompProperties_HediffGiver Props => (KnightVirtueCompProperties_HediffGiver)props;

    public void TickInterval(int delta)
    {
        if (this.Pawn.Drafted && this.Pawn.IsHashIntervalTick(Props.checkInterval, delta))
        {
            this.Pawn.GetOrAddHediff(Props.giveParams);
        }
    }
}