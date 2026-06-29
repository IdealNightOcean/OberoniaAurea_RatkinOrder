using OberoniaAurea_Frame;
using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GiveHediff_Self_MercyQuestSucceed : KnightVirtueComp
{
    public KnightVirtueCompProperties_HediffGiver Props => (KnightVirtueCompProperties_HediffGiver)props;

    public override void PostActive() => MercyQuestHandler.Instance.PostMercyQuestSucceed.Register(GiveHediff);

    public override void PostRemove() => MercyQuestHandler.Instance.PostMercyQuestSucceed.Deregister(GiveHediff);

    protected void GiveHediff(Quest quest, MercyQuestDef mercyQuestDef) => this.Pawn.GetOrAddHediff(Props.giveParams);
}
