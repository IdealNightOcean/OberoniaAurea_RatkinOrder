using Verse;

namespace OberoniaAurea.RatkinOrder;

public interface IPostSquadCombatPawnGenerate
{
    public void PostSquadCombatPawnGenerate(Pawn pawn, Squad squad, bool isCommander, bool friendly);
}
