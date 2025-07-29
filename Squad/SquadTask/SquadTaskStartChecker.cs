using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadTaskStartChecker
{
    public virtual AcceptanceReport CanStartNow(Squad squad, bool resultOnly = false)
    {
        return true;
    }

    public virtual float RandomlyChosenWeight(Squad squad)
    {
        return 1f;
    }
}
