using OberoniaAurea_Frame;

namespace OberoniaAurea.RatkinOrder;

public abstract class KnightVirtueComp_GiveHediff_Self_Active : KnightVirtueComp
{
    public KnightVirtueCompProperties_HediffGiver Props => (KnightVirtueCompProperties_HediffGiver)props;
    public override void PostActive() => this.Pawn.GetOrAddHediff(Props.giveParams);

    public override void PostRemove() => this.Pawn.RemoveFirstHediffOfDef(Props.giveParams.HediffToGive);
}