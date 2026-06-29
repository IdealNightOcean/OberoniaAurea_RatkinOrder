using OberoniaAurea_Frame;
using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightRoleWorker_Orderly(ResidentKnightRoleDef def) : ResidentKnightRoleWorker(def)
{
    public float MercyQuestChaceFactor(Pawn rolePawn)
    {
        float factor = 1.05f + (rolePawn.GetSkillLevel(SkillDefOf.Social) + rolePawn.GetSkillLevel(SkillDefOf.Intellectual)) * 0.01f;
        return Mathf.Min(1.35f, factor);
    }

    public float ExtraMercyQuestLetterChance(Pawn rolePawn)
    {
        float offset = 0.02f + (rolePawn.GetSkillLevel(SkillDefOf.Social) + rolePawn.GetSkillLevel(SkillDefOf.Intellectual)) * 0.01f;
        return Mathf.Min(0.32f, offset);
    }
}
