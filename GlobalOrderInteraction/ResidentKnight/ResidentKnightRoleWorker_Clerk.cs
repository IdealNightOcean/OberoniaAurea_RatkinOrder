using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightRoleWorker_Clerk(ResidentKnightRoleDef def) : ResidentKnightRoleWorker(def)
{
    public int KnightMoodOffset(Pawn rolePawn)
    {
        int offset = 1 + rolePawn.GetSkillLevel(SkillDefOf.Social) / 4;
        return offset > 6 ? 6 : offset;
    }
}