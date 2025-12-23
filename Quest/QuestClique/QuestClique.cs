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
    public string FullActiveDesc => (ActiveDesc + ": " + Potency.ToStringWithSign("0.##")).Colorize(Potency < 0f ? ColorLibrary.RedReadable : Color.green);

    private float potency; // 效能，-1~1
    private float willingness; // 参与意愿，0~1
    private float lastWillingnessChange;

    public float Potency
    {
        get
        {
            if (!IsBranchClique || potency <= 0f)
            {
                return potency;
            }

            float finalPotency = potency;
            if (focusedTaskType == relatedBranch.TaskHandler.FocusedTaskType)
            {
                finalPotency *= 1.1f;
            }
            if (focusedTaskType == relatedBranch.HonorDef?.focusedTaskType)
            {
                finalPotency *= 1.1f;
            }
            return Mathf.Clamp(finalPotency, -1f, 1f);
        }
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
    private BranchTaskType focusedTaskType = BranchTaskType.General;
    public BranchTaskType FocusedTaskType
    {
        get => IsBranchClique ? focusedTaskType : BranchTaskType.General;
        set => focusedTaskType = IsBranchClique ? value : BranchTaskType.General;
    }

    public QuestClique() { }
    public QuestClique(string key) { this.key = key; }

    public void InitForBranch(Branch branch)
    {
        relatedBranch = branch;

        if (string.IsNullOrEmpty(key))
        {
            key = GetBranchCliqueKey(branch);
        }
        if (string.IsNullOrEmpty(Name))
        {
            Name = branch.Name;
        }

        if (string.IsNullOrEmpty(ActiveDesc))
        {
            ActiveDesc = "OARO_QuestClique_DefaultBranchActive".Translate(relatedBranch.Name);
        }
        if (string.IsNullOrEmpty(InactiveDesc))
        {
            InactiveDesc = "OARO_QuestClique_DefaultBranchInactive".Translate(relatedBranch.Name);
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref key, nameof(key), "UNKNOWN");

        Scribe_Values.Look(ref Name, nameof(Name), "UNKNOWN");
        Scribe_Values.Look(ref ActiveDesc, nameof(ActiveDesc), string.Empty);
        Scribe_Values.Look(ref InactiveDesc, nameof(InactiveDesc), string.Empty);

        Scribe_Values.Look(ref potency, nameof(potency), 0f);
        Scribe_Values.Look(ref willingness, nameof(willingness), 0f);
        Scribe_Values.Look(ref lastWillingnessChange, nameof(lastWillingnessChange), 0f);

        Scribe_Values.Look(ref IsActive, nameof(IsActive), defaultValue: false);
        Scribe_Values.Look(ref IsCommunicable, nameof(IsCommunicable), defaultValue: false);
        Scribe_Values.Look(ref IsBribable, nameof(IsBribable), defaultValue: false);
        Scribe_Values.Look(ref BriberyCost, nameof(BriberyCost), -1);
        Scribe_Values.Look(ref TicksToActive, nameof(TicksToActive), -1);

        Scribe_References.Look(ref relatedBranch, nameof(relatedBranch));
        Scribe_Values.Look(ref focusedTaskType, nameof(focusedTaskType), BranchTaskType.General);
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
        if (branch.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            branchPotency *= 1.1f;
        }

        return branchPotency;
    }

    /// <summary>
    /// 将分队效能转换为任务派别效能，最大值100%
    /// </summary>
    public static float BranchPotencyToCliquePotency(float branchPotency)
    {
        return Mathf.Clamp(branchPotency * 0.0007f, 0.05f, 1f);
    }

    public static string GetBranchCliqueKey(Branch branch) => branch is null ? string.Empty : "BranchClique_" + branch.GetUniqueLoadID();
}