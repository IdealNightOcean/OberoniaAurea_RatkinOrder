using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTaskStartChecker
{
    public virtual AcceptanceReport CanStartNow(Branch branch, bool resultOnly) => true;

    public virtual float RandomlyChosenWeight(Branch branch) => 1f;
}
