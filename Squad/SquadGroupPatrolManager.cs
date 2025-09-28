using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadGroupPatrolManager : IExposable, IDrawDevWindow
{
    public enum PatrolType
    {
        Popedom,
        Kingdom,
        Border
    }
    public enum PatrolEndType
    {
        Nothing,
        Normal,
        Friendly,
        Accident,
        Disaster
    }
    public static readonly PatrolEndType[] PatrolEndTypeArr = (PatrolEndType[])Enum.GetValues(typeof(PatrolEndType));

    [Unsaved] private readonly List<(PatrolEndType, float)> patrolEndChances;
    [Unsaved] private int nextEndChancesGetTick = -1;
    public List<(PatrolEndType, float)> PatrolEndChances
    {
        get
        {
            if (Find.TickManager.TicksGame > nextEndChancesGetTick)
            {
                SetPatrolEndChances();
            }
            return patrolEndChances;
        }
    }

    [Unsaved] public readonly SquadManager SquadManager;
    public RatkinOrder RatkinOrder => SquadManager.RatkinOrder;

    private bool isPatrolActived = false;
    private bool isPatrolStarted = false;

    private int tickToNextStage = 180000;
    private int tickToNextCheck = 60000;

    public bool IsPatrolActived => isPatrolActived;
    public bool IsPatrolStarted => isPatrolStarted;

    private int adjustCeiling;
    private int adjustCount;
    private int burdenSquadCount;

    public int AdjustCeiling => adjustCeiling;
    public int AdjustCount => adjustCount;
    public int BurdenSquadCount => burdenSquadCount;

    public PatrolType CurPatrolType;
    public HashSet<Squad> Participants = [];

    private int passedBySquadCount;
    private float curReconnaissanceValue;
    private float endReconnaissanceValue;
    public float NeedReconnaissanceValue
    {
        get
        {
            return CurPatrolType switch
            {
                PatrolType.Popedom => SquadManager.TotalMemberCount * 0.16f * 10f,
                PatrolType.Kingdom => SquadManager.TotalMemberCount * 0.3f * 10f,
                PatrolType.Border => SquadManager.TotalMemberCount * 0.44f * 10f,
                _ => 0f,
            };
        }
    }

    public StringBuilder endResultText = new();

    public SquadGroupPatrolManager(SquadManager squadManager)
    {
        SquadManager = squadManager ?? throw new ArgumentNullException(nameof(squadManager));
        patrolEndChances = new List<(PatrolEndType, float)>(PatrolEndTypeArr.Length)
        {
            (PatrolEndType.Nothing, 1f)
        };

        for (int i = 1; i < PatrolEndTypeArr.Length; i++)
        {
            patrolEndChances.Add((PatrolEndTypeArr[i], 0f));
        }
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_SquadGroupPatrolManager(this));
    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"IsPatrolActived: {isPatrolActived}");
        listing_Rect.Label($"IsPatrolStarted: {isPatrolStarted}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"TickToNextStage: {tickToNextStage}");
        listing_Rect.Label($"TickToNextCheck: {tickToNextCheck}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"AdjustCeiling: {adjustCeiling}");
        listing_Rect.Label($"AdjustCount: {adjustCount}");
        listing_Rect.Label($"BurdenSquadCount: {burdenSquadCount}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"CurPatrolType: {CurPatrolType}");
        listing_Rect.Label($"passedBySquadCount: {passedBySquadCount}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"CurReconnaissanceValue: {curReconnaissanceValue}");
        listing_Rect.Label($"EndReconnaissanceValue: {endReconnaissanceValue}");
    }

    // 只在 isPatrolActived == true 时才会被SquadManager调用
    public void TickLong()
    {
        if ((tickToNextStage -= 1000) == 0)
        {
            if (!isPatrolStarted)
            {
                StartGroupPatrol();
            }
            return;
        }

        if ((tickToNextCheck -= 1000) == 0)
        {
            tickToNextCheck = 60000;
            if (tickToNextStage > 60000
                && passedBySquadCount < Participants.Count
                && RatkinOrder.Relationship >= OrderRelationshipKind.Acquaintance)
            {
                SquadPassBy();
            }
        }
    }

    public bool TryStartPatrolPerp()
    {
        SetCurPatrolType();

        return ChoiceParticipants() && StartPatrolPerp();
    }

    public void ChangeParticipant(IEnumerable<Squad> toAdd, IEnumerable<Squad> toRemove)
    {
        if (!isPatrolActived || isPatrolStarted)
        {
            return;
        }

        if (toRemove is not null)
        {
            foreach (Squad squad in toRemove)
            {
                Participants.Remove(squad);
                if (squad.TaskHandler.CurTask?.Def == SquadTaskDefOf.OARO_Squad_GroupPatrolPerp)
                {
                    squad.TaskHandler.EndCurrentTask(startRest: true);
                }
            }
        }

        if (toAdd is not null)
        {
            foreach (Squad squad in toAdd)
            {
                if (!Participants.Contains(squad) && squad.TaskHandler.TrySwitchToTask(SquadTaskDefOf.OARO_Squad_GroupPatrolPerp))
                {
                    Participants.Add(squad);
                }
            }
        }

        if (Participants.Count == 0)
        {
            Reset();
            return;
        }

    }

    public void Notify_SquadPatrolEnd(Squad squad, float finalReconnaissance, StringBuilder squadResult)
    {
        if (isPatrolActived)
        {
            Participants.Remove(squad);
            endReconnaissanceValue += finalReconnaissance;
            endResultText.AppendLineIfNotEmpty();
            endResultText.Append(squadResult);
            if (Participants.Count == 0)
            {
                GroupPatrolEnd();
            }
        }
    }

    public (float fundGain, float reformProgressGain) GetGroupPatrolEndResult(float reconnaissanceValue)
    {
        if (!isPatrolStarted)
        {
            return (0f, 0f);
        }

        float reconnaissanceRate = Mathf.Clamp(reconnaissanceValue / NeedReconnaissanceValue, 0f, 2f);
        float rewardMulti = CurPatrolType switch
        {
            PatrolType.Popedom => 1f,
            PatrolType.Kingdom => 2f,
            PatrolType.Border => 4f,
            _ => 1f,
        };
        float gainFund = 0f;
        float gainReformProgress = 0f;
        if (reconnaissanceRate < 0.5f)
        {
            gainFund = (reconnaissanceRate - 0.5f) * 0.002f * rewardMulti;
        }
        else if (reconnaissanceRate > 1f)
        {
            gainFund = (reconnaissanceRate - 1f) * 0.0005f * rewardMulti;
            gainReformProgress = (reconnaissanceRate - 1f) * 0.0005f * rewardMulti;
            reconnaissanceRate -= 1f;
        }
        gainReformProgress += 5f * (1f + rewardMulti) * reconnaissanceRate;

        return (gainFund, gainReformProgress);
    }

    private void SetCurPatrolType()
    {
        try
        {
            (PatrolType, int)[] typeChance =
            [
                (PatrolType.Popedom, 300),
                (PatrolType.Kingdom, 200 + (int)(RatkinOrder.FundHandler.Funds / 0.01f * 5f)),
                (PatrolType.Border, 10 + (int)(RatkinOrder.FundHandler.Funds / 0.01f * 6f + RatkinOrder.ReformationManager.ReformationsCount * 10f)),
            ];
            CurPatrolType = typeChance.RandomElementByWeight(r => r.Item2).Item1;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to set group patrol type: " + ex);
            CurPatrolType = PatrolType.Popedom;
        }
    }

    private bool ChoiceParticipants()
    {
        if (!RatkinOrder.ReformationManager.EffectTags.GetTagCount("", out short reformationTags))
        {
            reformationTags = 0;
        }

        float rate = 0.2f + (reformationTags * 0.05f);
        int squadCount = Mathf.FloorToInt(SquadManager.AllSquadsCount * rate);
        if (squadCount <= 0)
        {
            return false;
        }
        IEnumerable<Squad> tempEnumerables = SquadManager.AllSquads.Where(s => s.TaskHandler.CanSwitchToTask(SquadTaskDefOf.OARO_Squad_GroupPatrolPerp, resultOnly: true))
                                                                   .Take(squadCount);

        Participants = [.. tempEnumerables];

        if (Participants is null || Participants.Count == 0)
        {
            Reset();
            return false;
        }
        return true;
    }

    private bool StartPatrolPerp()
    {
        foreach (Squad squad in Participants)
        {
            squad.TaskHandler.TrySwitchToTask(SquadTaskDefOf.OARO_Squad_GroupPatrolPerp);
        }
        Participants.RemoveWhere(s => s.TaskHandler.CurTask?.Def != SquadTaskDefOf.OARO_Squad_GroupPatrolPerp);

        if (Participants.Count == 0)
        {
            Reset();
            return false;
        }

        tickToNextStage = 180000;
        isPatrolActived = true;
        adjustCount = 0;
        adjustCeiling = 0;
        if (RatkinOrder.Relationship == OrderRelationshipKind.Trustworthy)
        {
            adjustCeiling = RatkinOrder.ReformationManager.HasReformation(null) ? 3 : 2;
        }
        else if (RatkinOrder.Relationship == OrderRelationshipKind.Soulmate)
        {
            adjustCeiling = RatkinOrder.ReformationManager.HasReformation(null) ? 6 : 4;
        }

        return true;
    }

    private void RecacheCurReconnaissanceValue()
    {
        curReconnaissanceValue = endReconnaissanceValue;
        foreach (Squad squad in Participants)
        {
            if (squad.TaskHandler.CurTask is SquadTask_GroupPatrol groupPatrol)
            {
                curReconnaissanceValue += groupPatrol.reconnaissanceValue;
            }
        }
    }

    private void StartGroupPatrol()
    {
        tickToNextStage = (int)(SquadTaskDefOf.OARO_Squad_GroupPatrol.taskDurationDays * 60000);
        tickToNextCheck = 60000;
        passedBySquadCount = 0;
        isPatrolStarted = true;

        foreach (Squad squad in Participants)
        {
            if (squad.TaskHandler.CurTask?.Def == SquadTaskDefOf.OARO_Squad_GroupPatrolPerp)
            {
                squad.TaskHandler.FinishCurTask();
            }
        }

        Participants.RemoveWhere(s => s.TaskHandler.CurTask?.Def != SquadTaskDefOf.OARO_Squad_GroupPatrol);
        if (Participants.Count == 0)
        {
            Reset();
            return;
        }

        int overcap = Participants.Count - burdenSquadCount;
        if (overcap > 0)
        {
            RatkinOrder.FundHandler.AdjustFundsImmediately(overcap * 0.05f);
        }
        RecacheCurReconnaissanceValue();
    }

    private void GroupPatrolEnd()
    {
        (float fundGain, float reformProgressGain) = GetGroupPatrolEndResult(endReconnaissanceValue);
        RatkinOrder.FundHandler.AdjustFundsImmediately(fundGain);

        Reset();
    }

    private void SetPatrolEndChances()
    {
        nextEndChancesGetTick = Find.TickManager.TicksGame + 30000;
        float preparedModify = 0.5f;

        patrolEndChances.Clear();
        switch (CurPatrolType)
        {
            case PatrolType.Popedom:
                patrolEndChances.Add((PatrolEndType.Nothing, 0.69f));
                patrolEndChances.Add((PatrolEndType.Normal, 0.15f));
                patrolEndChances.Add((PatrolEndType.Friendly, 0.08f));
                patrolEndChances.Add((PatrolEndType.Accident, 0.07f * preparedModify));
                patrolEndChances.Add((PatrolEndType.Disaster, 0.01f * preparedModify));
                return;
            case PatrolType.Kingdom:
                patrolEndChances.Add((PatrolEndType.Nothing, 0.60f));
                patrolEndChances.Add((PatrolEndType.Normal, 0.15f));
                patrolEndChances.Add((PatrolEndType.Friendly, 0.1f));
                patrolEndChances.Add((PatrolEndType.Accident, 0.12f * preparedModify));
                patrolEndChances.Add((PatrolEndType.Disaster, 0.03f * preparedModify));
                return;
            case PatrolType.Border:
                patrolEndChances.Add((PatrolEndType.Nothing, 0.52f));
                patrolEndChances.Add((PatrolEndType.Normal, 0.16f));
                patrolEndChances.Add((PatrolEndType.Friendly, 0.12f));
                patrolEndChances.Add((PatrolEndType.Accident, 0.14f * preparedModify));
                patrolEndChances.Add((PatrolEndType.Disaster, 0.06f * preparedModify));
                return;
            default:
                patrolEndChances.Add((PatrolEndType.Nothing, 1f));
                for (int i = 1; i < PatrolEndTypeArr.Length; i++)
                {
                    patrolEndChances.Add((PatrolEndTypeArr[i], 0f));
                }
                return;
        }
    }

    private void SquadPassBy()
    {
        List<(Squad, float)> potentialPass = [];
        foreach (Squad squad in Participants)
        {
            if (squad.TaskHandler.CurTask is SquadTask_GroupPatrol groupPatrol && !groupPatrol.hadPassedBy)
            {
                potentialPass.Add((squad, squad.IsBranchSquadOfType(BranchType.Friendly) ? 3f : 1f));
            }
        }

        if (potentialPass.Count == 0)
        {
            return;
        }

        Squad targetSquad = potentialPass.RandomElementByWeight(s => s.Item2).Item1;
        potentialPass = null;

        (targetSquad.TaskHandler.CurTask as SquadTask_GroupPatrol).hadPassedBy = true;
        passedBySquadCount++;

        bool targetFriendly = targetSquad.IsBranchSquadOfType(BranchType.Friendly);
        int relationShipDiff = RatkinOrder.Relationship - OrderRelationshipKind.Acquaintance;
        List<(int, float)> passByTypeList =
        [
            (0, Mathf.Max(0f, 75f - (relationShipDiff > 0 ? relationShipDiff * 5f : 0f) - (targetFriendly ? 50f : 0f))),
            (1, Mathf.Max(0f, 20f + (relationShipDiff > 0 ? relationShipDiff * 5f : 0f) + (targetFriendly ? 30f : 0f))),
            (2, Mathf.Max(5f, 20f + (RatkinOrder.Relationship >= OrderRelationshipKind.Soulmate ?  5f : 0f) + (targetFriendly ? 20f : 0f))),
        ];

        int passByType = passByTypeList.RandomElementByWeight(t => t.Item2).Item1;
        passByTypeList = null;

        switch (passByType)
        {
            case 0:
                Messages.Message("OARO_Message_GroupPatrolPassBy".Translate(targetSquad.Name), MessageTypeDefOf.NeutralEvent, historical: true);
                return;
            case 1:
                Slate slate = new();
                slate.Set(KeyLibrary_SlateStoreAs.RatkinOrder, RatkinOrder);
                slate.Set(KeyLibrary_SlateStoreAs.Squad, targetSquad);
                if (OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Quest_TemporaryEncampment, slate, forced: false))
                {
                    break;
                }
                else
                {
                    goto case 0;
                }
            case 2:
                if (false)
                {
                    break;
                }
                else
                {
                    goto case 0;
                }
            default: goto case 0;
        }
    }

    public void Notify_MyOrderRemoved() { }

    private void Reset()
    {
        adjustCeiling = 0;
        adjustCount = 0;

        isPatrolActived = false;
        isPatrolStarted = false;
        tickToNextStage = 180000;
        tickToNextCheck = 60000;

        CurPatrolType = PatrolType.Popedom;
        burdenSquadCount = 0;

        endReconnaissanceValue = 0f;
        curReconnaissanceValue = 0f;
        endResultText.Clear();

        passedBySquadCount = 0;

        Participants ??= [];
        Participants.Clear();

        patrolEndChances[0] = (PatrolEndType.Nothing, 1f);
        for (int i = 1; i < PatrolEndTypeArr.Length; i++)
        {
            patrolEndChances[i] = (PatrolEndTypeArr[i], 0f);
        }
    }

    public void ExposeData()
    {
        string tempEndResultText = string.Empty;
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            tempEndResultText = endResultText.ToString();
        }

        Scribe_Values.Look(ref adjustCeiling, "adjustCeiling", 0);
        Scribe_Values.Look(ref adjustCount, "adjustCount", 0);

        Scribe_Values.Look(ref isPatrolActived, "isPatrolActived", defaultValue: false);
        Scribe_Values.Look(ref isPatrolStarted, "isPatrolStarted", defaultValue: false);
        Scribe_Values.Look(ref tickToNextStage, "tickToNextStage", 1800000);
        Scribe_Values.Look(ref tickToNextCheck, "tickToStart", 60000);

        Scribe_Values.Look(ref CurPatrolType, "CurPatrolType", PatrolType.Popedom);
        Scribe_Values.Look(ref burdenSquadCount, "burdenSquadCount", 0);
        Scribe_Values.Look(ref tempEndResultText, "tempEndResultText");

        Scribe_Values.Look(ref curReconnaissanceValue, "curReconnaissanceValue", 0f);
        Scribe_Values.Look(ref endReconnaissanceValue, "endReconnaissanceValue", 0f);

        Scribe_Collections.Look(ref Participants, "Participants", LookMode.Reference);


        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            endResultText = new StringBuilder(tempEndResultText);
        }
    }
}
