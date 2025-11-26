using Verse;

namespace OberoniaAurea.RatkinOrder;

public interface IPostBranchCombatKnightGenerate
{
    void PostBranchCombatKnightGenerate(Pawn pawn, Branch branch, bool isCommander, bool friendly);
}