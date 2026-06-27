using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GiveHediffInRange_Draft : KnightVirtueComp_GiveHediffInRange, ITickInterval
{
    public override bool HasExtraPawnValiator => false;

    public override void PostActive() => this.Knight.VirtueHandler.RegisterTickIntervalProcessor(this);

    public override void PostRemove() => this.Knight.VirtueHandler.DeregisterTickIntervalProcessor(this);

    public void TickInterval(int delta)
    {
        if (this.Pawn.IsHashIntervalTick(Props.checkInterval, delta))
            if (this.Pawn.Drafted)
                hediffGiver.GiveHediffToRange();
    }
}
