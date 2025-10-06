using Verse;

namespace OberoniaAurea.RatkinOrder;

public interface IPostSquadCombatPawnGenerate
{
    void PostSquadCombatPawnGenerate(Pawn pawn, Branch branch, bool isCommander, bool friendly);
}