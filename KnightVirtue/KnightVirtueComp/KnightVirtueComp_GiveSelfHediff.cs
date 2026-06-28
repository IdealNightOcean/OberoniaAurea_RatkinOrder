using OberoniaAurea_Frame;

namespace OberoniaAurea.RatkinOrder;

public abstract class KnightVirtueComp_GiveSelfHediff : KnightVirtueComp
{
    public KnightVirtueCompProperties_HediffGiver Props => (KnightVirtueCompProperties_HediffGiver)props;

    public override void PostRemove() => this.Pawn.RemoveFirstHediffOfDef(Props.giveParams.HediffToGive);

}
