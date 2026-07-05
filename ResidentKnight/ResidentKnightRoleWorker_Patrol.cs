using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightRoleWorker_Patrol(ResidentKnightRoleDef def) : ResidentKnightRoleWorker(def)
{
    public override IEnumerable<RimWorld.StatModifier> RoleStatOffsets(Pawn rolePawn)
    {
        float offest = 0.01f + rolePawn.GetSkillLevel(SkillDefOf.Shooting) * 0.003f + rolePawn.GetSkillLevel(SkillDefOf.Intellectual) * 0.003f;
        yield return new RimWorld.StatModifier()
        {
            stat = StatDefOf.PainShockThreshold,
            value = Mathf.Min(offest, 1.1f)
        };
    }

    public override IEnumerable<RimWorld.StatModifier> RoleStatFactors(Pawn rolePawn)
    {
        float factor = 1.02f + rolePawn.GetSkillLevel(SkillDefOf.Shooting) * 0.005f + rolePawn.GetSkillLevel(SkillDefOf.Melee) * 0.005f;
        yield return new RimWorld.StatModifier()
        {
            stat = StatDefOf.MeleeDamageFactor,
            value = Mathf.Min(factor, 1.15f)
        };
    }
}
