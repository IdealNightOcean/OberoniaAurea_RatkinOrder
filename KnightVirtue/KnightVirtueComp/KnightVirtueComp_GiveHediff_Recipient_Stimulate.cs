using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_GiveHediff_Recipient_Stimulate : KnightVirtueComp
{
    public KnightVirtueCompProperties_HediffGiver Props => (KnightVirtueCompProperties_HediffGiver)props;

    public override void Notify_Stimulate(Pawn recipient) => recipient.GetOrAddHediff(Props.giveParams);
}
