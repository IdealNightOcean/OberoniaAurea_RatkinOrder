using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightRoleWorker_Sentry(ResidentKnightRoleDef def) : ResidentKnightRoleWorker(def)
{
    public override IEnumerable<StatModifier> RoleStatOffsets(Pawn pawn)
    {
        float offest = 0.05f + pawn.GetSkillLevel(SkillDefOf.Shooting) * 0.01f + pawn.GetSkillLevel(SkillDefOf.Intellectual) * 0.01f;
        yield return new StatModifier()
        {
            stat = StatDefOf.MoveSpeed,
            value = Mathf.Min(offest, 0.35f)
        };
    }
}
