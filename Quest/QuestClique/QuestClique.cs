using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestClique : IExposable
{
    private string key;
    public string Key => key;

    public string Name = string.Empty;
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
        set => willingness = Mathf.Clamp01(value);
    }

    public bool IsActive;
    public bool IsActivatable;
    public bool IsCommunicable;
    public bool IsBribable;
    public BranchBuildingDef PreferredBuilding;
    public int BriberyCost = -1;
    public int TicksToActive = -1;

    private Branch relatedBranch;
    public Branch RelatedBranch => relatedBranch;
    public RatkinOrder RelatedRatkinOrder => relatedBranch?.RatkinOrder;
    public bool IsBranchClique => relatedBranch is not null;
    public bool IsFriendlyBranchClique => relatedBranch is not null && relatedBranch.IsBranchOfType(Branch.BranchType.Friendly);

    public QuestClique() { }
    public QuestClique(string key) { this.key = key; }

    public void InitForBranch(Branch branch, bool initWithBranchPotency = true)
    {
        relatedBranch = branch;

        if (key.NullOrEmpty())
        {
            key = GetBranchCliqueKey(branch);
        }
        if (Name.NullOrEmpty())
        {
            Name = branch.Name;
        }

        if (ActiveDesc.NullOrEmpty())
        {
            ActiveDesc = "OARO_QuestClique_DefaultBranchActive".Translate(relatedBranch.Name);
        }
        if (InactiveDesc.NullOrEmpty())
        {
            InactiveDesc = "OARO_QuestClique_DefaultBranchInactive".Translate(relatedBranch.Name);
        }

        if (initWithBranchPotency)
        {
            Potency = BranchPotencyToCliquePotency(GetBranchPotency(branch));
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref key, "key", "UNKNOWN");

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

    /// <summary>
    /// 获取分队效能
    /// </summary>
    public static float GetBranchPotency(Branch branch)
    {
        float branchPotency = (branch.Squad.AllCrewCount * 10f)
                            * (1f + (branch.FacilityHandler.TotalFacilityLevel + branch.MedalHandler.TotalMedalCount) * 0.04f);

        if (branch.IsBranchOfType(Branch.BranchType.Honor))
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
        if (branch is null)
        {
            return null;
        }
        return "BranchClique_" + branch.GetUniqueLoadID();
    }
}