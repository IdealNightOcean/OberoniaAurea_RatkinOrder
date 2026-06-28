using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class KnightVirtueComp_GiveHediff_StimulateRecipient : KnightVirtueComp
{
    public KnightVirtueCompProperties_HediffGiver Props => (KnightVirtueCompProperties_HediffGiver)props;

    public override void Notify_Stimulate(Pawn recipient) => recipient.GetOrAddHediff(Props.giveParams);
}
