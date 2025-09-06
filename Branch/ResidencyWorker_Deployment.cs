using RimWorld;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class ResidencyWorker : IExposable
{
    public virtual int Priority => 1000; //优先级，数值越大越优先
    public abstract void ResidencyStart(Branch branch, Pawn resident, int residencyDays);
    public abstract void ResidencyEnd(Branch branch, Pawn resident, int residencyDays);
    public abstract void ExposeData();

}

public class ResidencyWorker_Deployment : ResidencyWorker
{
    protected static Branch cachedBranch;
    private static float cachedDailyXp;
    private static bool cachedSilverReward;
    private static bool cachedInstinctTrain;

    public override int Priority => 500; //优先级，数值越大越优先
    public SkillDef Skill;

    public override void ExposeData()
    {
        Scribe_Defs.Look(ref Skill, "Skill");
    }

    public override void ResidencyStart(Branch branch, Pawn resident, int residencyDays) { }
    public override void ResidencyEnd(Branch branch, Pawn resident, int residencyDays)
    {
        Recache(branch);
        if (Skill is not null && resident.skills is not null)
        {
            float xpGain = cachedDailyXp * residencyDays;
            resident.skills.GetSkill(Skill).Learn(xpGain, direct: true);
        }
        if (cachedSilverReward)
        {

        }
        if (cachedInstinctTrain)
        {
            resident.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_InstinctTrain);
        }
    }

    public static void Recache(Branch branch)
    {
        if (branch == cachedBranch)
        {
            return;
        }
        cachedBranch = branch;
        cachedDailyXp = BranchStatUtility.GetStatValue(branch, BranchStatDefOf.OARO_DeployeeDailyXp, null, immediateUpdate: true);
        cachedSilverReward = branch.EffectTags.HasActiveTag("InstinctTrain");
        cachedInstinctTrain = branch.EffectTags.HasActiveTag("InstinctTrain");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearStaticCache()
    {
        cachedBranch = null;
        cachedDailyXp = 0f;
        cachedSilverReward = false;
    }
}