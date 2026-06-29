using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class CompRapierCombo : CompMeleeAttackCombo
{
    public override bool CanCombo(Thing victim, Pawn caster)
    {
        if (!base.CanCombo(victim, caster) || victim is not Pawn pawn)
        {
            return false;
        }
        return caster.GetSkillLevel(SkillDefOf.Melee) >= pawn.GetSkillLevel(SkillDefOf.Melee);
    }
}
