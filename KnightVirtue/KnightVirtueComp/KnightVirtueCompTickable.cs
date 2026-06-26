namespace OberoniaAurea.RatkinOrder;

public abstract class KnightVirtueCompTickable : KnightVirtueComp, ITickInterval
{
    public override void PostActive()
    {
        base.PostActive();
        this.Knight.VirtueHandler.RegisterTickIntervalProcessor(this);
    }

    public override void PostRemove()
    {
        base.PostRemove();
        this.Knight.VirtueHandler.DeregisterTickIntervalProcessor(this);
    }

    public abstract void TickInterval(int delta);
}
