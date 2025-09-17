using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class CompEdgeSwordFightBack : CompMeleeFightBack
{
    protected override bool CanFightBack(Pawn instigator)
    {
        int skillDiff = parentPawn.GetSkillLevelOfPawn(SkillDefOf.Melee) - instigator.GetSkillLevelOfPawn(SkillDefOf.Melee);
        if (skillDiff < 0)
        {
            return false;
        }
        return Rand.Chance(Props.baseFightBackChance + skillDiff * 0.05f);
    }
}