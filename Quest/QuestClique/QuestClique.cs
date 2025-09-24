using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestClique : IExposable
{
    public string Name = "UNKNOWN";
    public string ActiveDesc = string.Empty;
    public string InactiveDesc = string.Empty;

    public string Description => IsActive ? FullActiveDesc : InactiveDesc;
    public string FullActiveDesc => (ActiveDesc + ": " + Potency.ToStringWithSign("F2")).Colorize(Potency < 0f ? ColorLibrary.RedReadable : Color.green);

    private float potency; // 效能，-1~1
    private float willingness; // 参与意愿，0~1
    private float lastWillingnessChange;

    public float Potency
    {
        get => potency;
        set => potency = Mathf.Clamp(value, -1f, 1f);
    }
    public float Willingness
    {
        get => willingness;
    }
    public float LastWillingnessChange => lastWillingnessChange;

    public bool IsActive;
    public bool IsCommunicable;
    public bool IsBribable;
    public int BriberyCost = -1;
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
        Scribe_Values.Look(ref ActiveDesc, "ActiveDesc", string.Empty);
        Scribe_Values.Look(ref InactiveDesc, "InactiveDesc", string.Empty);

        Scribe_Values.Look(ref potency, "potency", 0f);
        Scribe_Values.Look(ref willingness, "willingness", 0f);
        Scribe_Values.Look(ref lastWillingnessChange, "lastWillingnessChange", 0f);

        Scribe_Values.Look(ref IsActive, "IsActive", defaultValue: false);
        Scribe_Values.Look(ref IsCommunicable, "IsCommunicable", defaultValue: false);
        Scribe_Values.Look(ref IsBribable, "IsBribable", defaultValue: false);
        Scribe_Values.Look(ref BriberyCost, "BriberyCost", -1);
        Scribe_Values.Look(ref TicksToActive, "TicksToActive", -1);

        Scribe_References.Look(ref relatedBranch, "relatedBranch");
    }

    public void AdjustCliqueWillingness(float delta, bool record = true)
    {
        if (delta != 0f)
        {
            willingness = Mathf.Clamp01(willingness + delta);
            if (record)
            { lastWillingnessChange = delta; }
        }
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