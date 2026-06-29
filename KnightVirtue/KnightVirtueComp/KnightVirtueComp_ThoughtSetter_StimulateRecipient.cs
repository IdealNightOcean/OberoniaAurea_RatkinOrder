using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_ThoughtSetter_StimulateRecipient : KnightVirtueComp
{
    public KnightVirtueCompProperties_ThoughtSetter Props => (KnightVirtueCompProperties_ThoughtSetter)props;

    public override void Notify_Stimulate(Pawn recipient) => recipient.GetOrAddMemory(Props.giveParams);
}
