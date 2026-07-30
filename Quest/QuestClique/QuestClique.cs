using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestClique : IExposable
{
    public QuestPart_CliquesManager CliquesManager { get; set; }
    public Quest Quest => CliquesManager?.quest;

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
            if (focusedTaskChivalry.IsSameDefNonNullable(relatedBranch.TaskHandler.FocusedTaskChivalry))
            {
                finalPotency *= 1.1f;
            }
            if (focusedTaskChivalry.IsSameDefNonNullable(relatedBranch.HonorDef?.chivalry))
            {
                finalPotency *= 1.1f;
            }
            return Mathf.Clamp(finalPotency, -1f, 1f);
        }
        set => potency = Mathf.Clamp(value, -1f, 1f);
    }
    public float Willingness => willingness;

    public bool IsActive;
    public int ticksToInactive = -1;
    private int TicksToInactive => ticksToInactive;

    public bool IsCommunicable;
    private int lastCommunicateTick = -1;

    public bool IsBribable;
    public int BriberyCost = -1;

    public BranchBuildingDef PreferredBuilding;

    private Branch relatedBranch;
    public Branch RelatedBranch => relatedBranch;
    public RatkinOrder RelatedRatkinOrder => relatedBranch?.RatkinOrder;
    public bool IsBranchClique => relatedBranch is not null;
    public bool IsFriendlyBranchClique => relatedBranch is not null && relatedBranch.IsBranchOfType(Branch.BranchType.Friendly);
    private KnightChivalryDef focusedTaskChivalry;
    public KnightChivalryDef FocusedTaskChivalry
    {
        get => IsBranchClique ? focusedTaskChivalry : null;
        set => focusedTaskChivalry = value;
    }

    public QuestClique() { }
    public QuestClique(string key) { this.key = key; }

    public void InitForBranch(Branch branch)
    {
        relatedBranch = branch;

        if (String.IsNullOrEmpty(key))
        {
            key = GetBranchCliqueKey(branch);
        }
        if (String.IsNullOrEmpty(Name))
        {
            Name = branch.Name;
        }

        if (String.IsNullOrEmpty(ActiveDesc))
        {
            ActiveDesc = "OARO_QuestClique_DefaultBranchActive".Translate(relatedBranch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName));
        }
        if (String.IsNullOrEmpty(InactiveDesc))
        {
            InactiveDesc = "OARO_QuestClique_DefaultBranchInactive".Translate(relatedBranch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName));
        }
    }

    public void AdjustCliqueWillingness(float change, bool showMessage = true)
    {
        float trueChange = willingness;
        willingness = Mathf.Clamp01(willingness + change);
        trueChange = willingness - trueChange;
        if (showMessage)
        {
            if (trueChange > 0f)
            {
                Messages.Message(
                    text: "OARO_CliqueWillingness_Increase".Translate(
                        Name.Named(OARO_KeyLibrary_FormatArgName.CliqueName),
                        trueChange.ToStringPercent("0.##").Named(KeyLibrary_FormatArgName.Change)),
                    def: MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message(
                    text: "OARO_CliqueWillingness_Decrease".Translate(
                        Name.Named(OARO_KeyLibrary_FormatArgName.CliqueName),
                        (-trueChange).ToStringPercent("0.##").Named(KeyLibrary_FormatArgName.Change)),
                    def: MessageTypeDefOf.NegativeEvent);
            }
        }
    }

    public AcceptanceReport CanCommunicable(bool resultOnly)
    {
        if (IsActive)
        {
            return resultOnly ? false : "OARO_Clique_HasActive".Translate();
        }
        if (!IsCommunicable)
        {
            return resultOnly ? false : "OARO_Clique_NotCommunicable".Translate();
        }
        if (lastCommunicateTick > 0 && Find.TickManager.TicksGame < lastCommunicateTick + 60000)
        {
            int communicateCoolingTicksLeft = lastCommunicateTick + 60000 - Find.TickManager.TicksGame;
            return resultOnly ? false : "WaitTime".Translate(communicateCoolingTicksLeft.ToStringTicksToPeriod());
        }

        return true;
    }

    public AcceptanceReport CanActiveNow(bool directly, int mapRecommendationCount = -1, bool resultOnly = false)
    {
        if (IsActive)
        {
            return resultOnly ? false : "OARO_Clique_HasActive".Translate(Name.Named(OARO_KeyLibrary_FormatArgName.CliqueName));
        }

        if (directly)
        {
            return true;
        }

        if (ticksToInactive > 0)
        {
            return resultOnly ? false : "OARO_Clique_PrepareActivation".Translate(Name.Named(OARO_KeyLibrary_FormatArgName.CliqueName));
        }

        if (IsBranchClique)
        {
            if (IsFriendlyBranchClique)
            {
                mapRecommendationCount = mapRecommendationCount >= 0 ? mapRecommendationCount : RecommendationUtility.CurRecommendationCount(OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true));
                if (mapRecommendationCount < 1)
                {
                    return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(1.Named(KeyLibrary_FormatArgName.Count));
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (RelatedBranch.Supply < 0.25f)
                {
                    return resultOnly ? false : "OARO_Insufficient_SquadSupply".Translate(0.25f.ToStringPercent());
                }
                if (willingness < 0.6f)
                {
                    return resultOnly ? false : "OARO_Insufficient_CliqueWillingness".Translate(Name.Named(OARO_KeyLibrary_FormatArgName.CliqueName),
                                                                                                0.6f.ToStringPercent("0.##").Named(KeyLibrary_FormatArgName.Chance));
                }

                return true;
            }
        }
        else
        {
            if (willingness < 0.9f)
            {
                return resultOnly ? false : "OARO_Insufficient_CliqueWillingness".Translate(Name.Named(OARO_KeyLibrary_FormatArgName.CliqueName),
                                                                                            0.9f.ToStringPercent("0.##").Named(KeyLibrary_FormatArgName.Chance));
            }

            return true;
        }
    }

    public AcceptanceReport CanBribable(Map map, bool resultOnly)
    {
        if (IsActive)
        {
            return resultOnly ? false : "OARO_Clique_HasActive".Translate(Name.Named(OARO_KeyLibrary_FormatArgName.CliqueName));
        }
        if (!IsBribable)
        {
            return resultOnly ? false : "OARO_Clique_NotBribable".Translate(Name.Named(OARO_KeyLibrary_FormatArgName.CliqueName));
        }


        if (map is null)
        {
            return resultOnly ? false : "OARO_NeedAMap".Translate();
        }

        if (BriberyCost > 0 && !map.HasEnoughThingsOfDef(ThingDefOf.Silver, BriberyCost))
        {
            return resultOnly ? false : "OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.LabelCap, BriberyCost);
        }

        return true;
    }

    public bool TryActive(bool directly = false, Map map = null, int activeDelayTicks = -1)
    {
        if (!IsActive)
        {
            Log.Error($"[OARO] 尝试激活已激活的派别 {Name} ({key})。");
            return false;
        }

        if (directly)
        {
            Active();
            return true;
        }

        int delayTicks = activeDelayTicks;
        //非友好分队派别激活参与有2~4天默认延迟
        if (delayTicks < 0 && IsBranchClique && !RelatedBranch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            delayTicks = Rand.RangeInclusive(120000, 240000);
        }

        if (delayTicks > 0)
        {
            ticksToInactive = Mathf.Min(ticksToInactive, delayTicks);
        }
        else
        {
            if (IsBranchClique)
            {
                RelatedBranch.Supply -= 0.25f;
                //邀请友好分部派别参与消耗1推荐信
                if (RelatedBranch.IsBranchOfType(Branch.BranchType.Friendly))
                {
                    RecommendationUtility.UseRecommendationOfPlayer(OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true), 1);
                }
            }
            Active();
        }

        return true;

        void Active()
        {
            IsActive = true;
            ticksToInactive = -1;
            if (CliquesManager is not null)
            {
                CliquesManager.TotalPotency.MarkDirty();
                Find.SignalManager.SendSignal(new Signal(QuestPart_CliquesManager.SignalCliqueActived(Quest), this.Named(KeyLibrary_FormatArgName.SUBJECT)));
            }
        }
    }

    public void Deactive()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        if (CliquesManager is not null)
        {
            CliquesManager.TotalPotency.MarkDirty();
            Find.SignalManager.SendSignal(new Signal(QuestPart_CliquesManager.SignalCliqueDeactived(Quest), this.Named(KeyLibrary_FormatArgName.SUBJECT)));
        }
    }

    public void Communicate(Branch branch, Map map = null)
    {
        if (!IsCommunicable)
        {
            Log.Error($"[OARO] 尝试与不可通讯的派别 {Name} ({key}) 通讯。");
            return;
        }
        float willingnessGain = Rand.Range(0.05f, 0.15f);
        lastCommunicateTick = Find.TickManager.TicksGame;
        string text;
        if (PreferredBuilding is not null && branch.BuildingHandler.HasBuilding(PreferredBuilding))
        {
            willingnessGain += 0.15f;
            text = "OARO_Clique_CommunicateInfoWithPrefer".Translate(
                Name.Named(OARO_KeyLibrary_FormatArgName.CliqueName),
                willingnessGain.ToStringPercent().Named(KeyLibrary_FormatArgName.Change),
                PreferredBuilding.Named("BUILDING"));
        }
        else
        {
            text = "OARO_Clique_CommunicateInfo".Translate(
                Name.Named(OARO_KeyLibrary_FormatArgName.CliqueName),
                willingnessGain.ToStringPercent().Named(KeyLibrary_FormatArgName.Change));
        }

        AdjustCliqueWillingness(willingnessGain);
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(text.Translate()));

        if (CanActiveNow(directly: false, resultOnly: true))
        {
            TryActive(directly: false, map: map);
        }
    }

    public void Bribery(Map map)
    {
        map.DestroyThingsOfDef(ThingDefOf.Silver, BriberyCost);
        AdjustCliqueWillingness(1f - Willingness + 0.1f);

        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_Clique_BribeInfo".Translate(Name.Named(OARO_KeyLibrary_FormatArgName.CliqueName))));

        if (CanActiveNow(directly: false, resultOnly: true))
        {
            TryActive(directly: false);
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
        Scribe_Values.Look(ref ticksToInactive, nameof(ticksToInactive), -1);

        Scribe_Values.Look(ref IsCommunicable, nameof(IsCommunicable), defaultValue: false);
        Scribe_Values.Look(ref lastCommunicateTick, nameof(lastCommunicateTick), -1);

        Scribe_Values.Look(ref IsBribable, nameof(IsBribable), defaultValue: false);
        Scribe_Values.Look(ref BriberyCost, nameof(BriberyCost), -1);

        Scribe_References.Look(ref relatedBranch, nameof(relatedBranch));
        Scribe_Defs.Look(ref focusedTaskChivalry, nameof(focusedTaskChivalry));
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