using OberoniaAurea_Frame;
using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_ThoughtSetter_MercyQuestSucceed : KnightVirtueComp
{
    public KnightVirtueCompProperties_ThoughtSetter Props => (KnightVirtueCompProperties_ThoughtSetter)props;

    public override void PostActive() => MercyQuestHandler.Instance.PostMercyQuestSucceed.Register(GiveThought);

    public override void PostRemove() => MercyQuestHandler.Instance.PostMercyQuestSucceed.Deregister(GiveThought);

    protected void GiveThought(Quest quest, MercyQuestDef mercyQuestDef) => this.Pawn.GetOrAddMemory(Props.giveParams);
}
