using OberoniaAurea.RatkinOrder.DataLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchResident_Deployment : BranchResident
{
    public SkillDef Skill;
    protected BranchResident_Deployment() : base() { }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref Skill, "Skill");
    }

    public override void EndResidency()
    {
        if (pawn is null)
            return;

        float deployDays = Mathf.Max(0f, (totalDeployDays - DeployDaysLeft));

        if (Skill is not null && pawn.skills is not null)
        {
            //  float xpGain = cachedDailyXp * deployDays;
            //  pawn.skills.GetSkill(Skill).Learn(xpGain, direct: true);
        }

        if (branch.EffectTags.HasTag(""))
        {

        }

        if (branch.EffectTags.HasTag(KeyLibrary_EffectTag.IntensiveTrain))
        {
            pawn.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_IntensiveTrain);
        }
    }
}