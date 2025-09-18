using Verse;

namespace OberoniaAurea.RatkinOrder;

public interface IPostSquadCombatPawnGenerate
{
    void PostSquadCombatPawnGenerate(Pawn pawn, Squad squad, bool isCommander, bool friendly);
}