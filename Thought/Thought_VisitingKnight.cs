using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public class Thought_VisitingKnight : Thought_Memory
{
    public override void ThoughtInterval()
    {
        age += 150;
        int index = OrderInteractionHandler.OrderHallLevel;
        SetForcedStage(index > 0 ? index - 1 : 0);
    }
}
