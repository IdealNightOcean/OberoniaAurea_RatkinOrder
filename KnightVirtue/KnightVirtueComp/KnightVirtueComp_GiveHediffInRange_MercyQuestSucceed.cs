using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GiveHediffInRange_MercyQuestSucceed : KnightVirtueComp_GiveHediffInRange
{
    public override bool HasExtraPawnValiator => false;

    public override void PostActive() => MercyQuestHandler.Instance.PostMercyQuestSucceed.Register(GiveHediffInRange);

    public override void PostRemove() => MercyQuestHandler.Instance.PostMercyQuestSucceed.Deregister(GiveHediffInRange);

    protected void GiveHediffInRange(Quest quest, MercyQuestDef mercyQuestDef) => GiveHediffInRange();
}
