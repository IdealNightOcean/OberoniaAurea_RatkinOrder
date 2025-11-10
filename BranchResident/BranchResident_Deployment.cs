using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchResident_Deployment : BranchResident
{
    public override int Priority => 500;

    private SkillDef skill;

    public SkillDef Skill => Skill;
    protected BranchResident_Deployment() : base() { }
    public BranchResident_Deployment(Pawn resident, int totalDeployDays, SkillDef skillDef) : base(resident, totalDeployDays)
    {
        skill = skillDef;
    }
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref skill, "skill");
    }

    public override void EndResidency(Branch branch)
    {
        if (resident is null)
        {
            return;
        }
        float deployDays = Mathf.Max(0f, (totalDeployDays - DeployDaysLeft));

        if (skill is not null && resident.skills is not null)
        {
            //  float xpGain = cachedDailyXp * deployDays;
            //  resident.skills.GetSkill(Skill).Learn(xpGain, direct: true);
        }

        if (branch.EffectTags.HasTag(""))
        {

        }

        if (branch.EffectTags.HasTag(KeyLibrary_EffectTag.IntensiveTrain))
        {
            resident.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_IntensiveTrain);
        }
    }
}