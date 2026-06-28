using OberoniaAurea_Frame;

namespace OberoniaAurea.RatkinOrder;

public abstract class KnightVirtueComp_GiveSelfHediff_Active : KnightVirtueComp_GiveSelfHediff
{
    public override void PostActive() => this.Pawn.GetOrAddHediff(Props.giveParams);
}