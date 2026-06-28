
using OberoniaAurea_Frame;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueCompProperties_ThoughtSetter : KnightVirtueCompProperties
{
    public MemoryGiveParams giveParams;
    public int checkInterval = 60;

    public KnightVirtueCompProperties_ThoughtSetter()
    {
        compClass = typeof(KnightVirtueComp_ThoughtSetter);
    }
}


public class KnightVirtueComp_ThoughtSetter : KnightVirtueComp
{
    public KnightVirtueCompProperties_ThoughtSetter Props => (KnightVirtueCompProperties_ThoughtSetter)props;

    public override void PostActive() => this.Pawn.GetOrAddMemory(Props.giveParams);

    public override void PostRemove() => this.Pawn.RemoveAllMemoriesOfDef(Props.giveParams.MemoryToGive);
}
