using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GiveHediffInRange_Cyclicity : KnightVirtueComp_GiveHediffInRange, ITickInterval
{
    public override bool HasExtraPawnValiator => false;

    public override void PostActive() => this.Knight.VirtueHandler.RegisterTickIntervalProcessor(this);

    public override void PostRemove() => this.Knight.VirtueHandler.DeregisterTickIntervalProcessor(this);

    public virtual void TickInterval(int delta)
    {
        if (this.Pawn.IsHashIntervalTick(Props.checkInterval, delta))
            GiveHediffInRange();
    }

}
