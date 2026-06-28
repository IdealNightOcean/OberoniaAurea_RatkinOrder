using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_ThoughtSetter_Drafted : KnightVirtueCompTickable
{
    public KnightVirtueCompProperties_ThoughtSetter Props => (KnightVirtueCompProperties_ThoughtSetter)props;

    public override void TickInterval(int delta)
    {
        if (!this.Pawn.Drafted)
            return;

        if (this.Pawn.IsHashIntervalTick(Props.checkInterval, delta))
        {
            this.Pawn.GetOrAddMemory(Props.giveParams);
        }
    }
}







