using Verse;

namespace OberoniaAurea.RatkinOrder;

public interface IPostCombatantGenerate
{
    void PostCombatantGenerate(Pawn pawn, KnightRecord knightRecord);
}