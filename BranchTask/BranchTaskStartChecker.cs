using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTaskStartChecker
{
    public virtual AcceptanceReport CanStartNow(Branch branch, bool resultOnly)
    {
        return true;
    }

    public virtual float RandomlyChosenWeight(Branch branch)
    {
        return 1f;
    }
}
