using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestClique : IExposable
{
    public string Name = "UNKNOWN";
    public float Potency;

    public string PotencyDesc = string.Empty;
    public string FullPotencyDesc => (PotencyDesc + ": " + Potency.ToString("F2")).Colorize(Potency > 0f ? Color.green : ColorLibrary.RedReadable);

    private float willingness;
    public float Willingness
    {
        get => willingness;
        set => willingness = Mathf.Clamp01(value);
    }

    public bool IsActive;
    public int TicksToActive = -1;

    private Branch relatedBranch;
    public Branch RelatedBranch => relatedBranch;
    public RatkinOrder RelatedRatkinOrder => relatedBranch?.RatkinOrder;
    public bool IsBranchClique => relatedBranch is not null;
    public bool IsFriendlyBranchClique => relatedBranch is not null && relatedBranch.IsBranchOfType(BranchType.Friendly);

    public QuestClique() { }

    public QuestClique(Branch branch)
    {
        InitForBranch(branch);
    }

    public void InitForBranch(Branch branch)
    {
        relatedBranch = branch;
        Name = branch.Name;
        Potency = BranchPotencyToCliquePotency(GetBranchPotency(branch));
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref Name, "Name", "UNKNOWN");
        Scribe_Values.Look(ref Potency, "Potency", 0f);
        Scribe_Values.Look(ref PotencyDesc, "PotencyDesc", string.Empty);
        Scribe_Values.Look(ref willingness, "willingness", 0f);

        Scribe_Values.Look(ref IsActive, "IsActive", defaultValue: false);
        Scribe_References.Look(ref relatedBranch, "relatedBranch");
    }

    /// <summary>
    /// 获取分队效能
    /// </summary>
    public static float GetBranchPotency(Branch branch)
    {
        float branchPotency = (branch.Squad.SquadStat.MemberCount + branch.Squad.SquadStat.CommanderCount) * 10f
                            * (1f + (branch.FacilityHandler.TotalFacilityLevel + branch.Squad.SquadStat.TotalMedalCount) * 0.04f);

        if (branch.IsBranchOfType(BranchType.Honor))
        {
            branchPotency *= 1.25f;
        }
        if (branch.RatkinOrder.ReformationManager.HasReformation(null))
        {
            branchPotency *= 1.1f;
        }

        return branchPotency;
    }

    /// <summary>
    /// 将分队效能转换为任务派别效能，最大值50%
    /// </summary>
    public static float BranchPotencyToCliquePotency(float branchPotency)
    {
        return Mathf.Clamp(branchPotency, 0f, 0.5f);
    }

    public static string GetBranchCliqueKey(Branch branch)
    {
        return "BranchClique_" + branch.GetUniqueLoadID();
    }
}