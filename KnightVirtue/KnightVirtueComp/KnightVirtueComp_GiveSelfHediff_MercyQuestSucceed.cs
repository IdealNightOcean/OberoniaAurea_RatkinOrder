using OberoniaAurea_Frame;
using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GiveSelfHediff_MercyQuestSucceed : KnightVirtueComp_GiveSelfHediff
{
    public override void PostActive() => MercyQuestHandler.Instance.PostMercyQuestSucceed.Register(GiveHediff);

    public override void PostRemove()
    {
        base.PostRemove();
        MercyQuestHandler.Instance.PostMercyQuestSucceed.Deregister(GiveHediff);
    }

    protected void GiveHediff(Quest quest, MercyQuestDef mercyQuestDef) => this.Pawn.GetOrAddHediff(Props.giveParams);
}
