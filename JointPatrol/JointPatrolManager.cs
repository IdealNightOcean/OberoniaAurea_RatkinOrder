using NightOcean;
using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

using IncidentType = JointPatrolIncidentDef.IncidentType;
using PatrolInteractionType = JointBranchRecord.PatrolInteractionType;

public partial class JointPatrolManager : IExposable, IThingHolder, IPawnRetentionHolder
{
    private const int JointPatrolDurationPrepDays = 3;
    private const int JointPatrolDurationDays = 7;
    //PatrolStateprivatePatrolState PatrolStatestaticPatrolState PatrolStatereadonlyPatrolState PatrolStateBranchTaskTypePatrolState[] PatrolStatepatrolTaskTypePatrolState = [PatrolStateBranchTaskTypePatrolState.PatrolStateCrimeFightingPatrolState, PatrolStateBranchTaskTypePatrolState.PatrolStateStabilityMaintenancePatrolState, PatrolStateBranchTaskTypePatrolState.PatrolStateAssistancePatrolState, PatrolStateBranchTaskTypePatrolState.PatrolStateSupervisionPatrolState];

    private readonly RatkinOrder ratkinOrder;
    private BranchManager BranchManager => ratkinOrder.BranchManager;

    private PatrolState curState;
    public PatrolState CurState => curState;

    private int tickToNextStage = JointPatrolDurationPrepDays * 60000;
    private int tickToNextCheck = 60000;

    private int burdenSquadCount;
    public int BurdenSquadCount => burdenSquadCount;

    private PatrolLevel patrolLevel;
    public PatrolLevel PatrolLevelValue => patrolLevel;

    private List<JointBranchRecord> participants = [];

    [Unsaved] private Dictionary<Branch, JointBranchRecord> participantsDict = [];
    public IReadOnlyDictionary<Branch, JointBranchRecord> ParticipantsDict => participantsDict;
    private List<Pawn> participatingResidentKnights = [];
    private ThingOwner<Pawn> innerContainer;

    private Dictionary<PatrolInteractionType, int> patrolInteractionAcquired = [];

    private List<JointIncidentRecord> incidentRecords = [];
    public IReadOnlyList<JointIncidentRecord> IncidentRecords => incidentRecords;

    [Unsaved] private LazyMutable<IReadOnlyDictionary<BranchTaskType, float>> taskPotencys;
    [Unsaved] private readonly LazyMutable<IReadOnlyDictionary<IncidentType, List<JointPatrolIncidentDef>>> potentialIncidents;

    public float NeededTaskPotency
    {
        get
        {
            return patrolLevel switch
            {
                PatrolLevel.Popedom => BranchManager.TotalKnights * 0.16f * 10f * 0.25f,
                PatrolLevel.Kingdom => BranchManager.TotalKnights * 0.3f * 10f * 0.25f,
                PatrolLevel.Border => BranchManager.TotalKnights * 0.44f * 10f * 0.25f,
                _ => 0f,
            };
        }
    }

    public JointPatrolManager(RatkinOrder ratkinOrder)
    {
        this.ratkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        innerContainer = new ThingOwner<Pawn>(this);

        taskPotencys = new(refreshFunc: () => participants.GroupBy(p => p.FocusedTaskType)
                                                          .ToDictionary(g => g.Key, g => g.Sum(gp => gp.TaskPotency.Value)));


        potentialIncidents = new(refreshFunc: () => DefDatabase<JointPatrolIncidentDef>.AllDefsListForReading.Where(d => !d.patrolLevelLimits.HasValue || d.patrolLevelLimits.Value == patrolLevel)
                                                                                                             .GroupBy(d => d.incidentType)
                                                                                                             .ToDictionary(g => g.Key, g => g.ToList()));

    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref curState, "curState", PatrolState.Invalid);
        Scribe_Values.Look(ref tickToNextStage, "tickToNextStage", 1800000);
        Scribe_Values.Look(ref tickToNextCheck, "tickToStart", 60000);

        Scribe_Values.Look(ref patrolLevel, "CurPatrolType", PatrolLevel.Popedom);
        Scribe_Values.Look(ref burdenSquadCount, "burdenSquadCount", 0);

        Scribe_Collections.Look(ref participants, "participants", LookMode.Deep);
        Scribe_Collections.Look(ref patrolInteractionAcquired, "patrolInteractionAcquired", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref incidentRecords, "incidentRecords", LookMode.Deep);
    }

    private void ClearPatrolData(PatrolState forState)
    {
        burdenSquadCount = 0;

        tickToNextStage = JointPatrolDurationPrepDays * 60000;
        tickToNextCheck = 60000;

        participants.Clear();
        participantsDict.Clear();

        taskPotencys.Reset();
        potentialIncidents.Reset();
        if (forState == PatrolState.Prepare)
        {
            incidentRecords.Clear();
        }
        else
        {
            patrolLevel = default;
        }
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_JointPatrolManager(ratkinOrder));
    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"CurState: {curState}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"TickToNextStage: {tickToNextStage}");
        listing_Rect.Label($"TickToNextCheck: {tickToNextCheck}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"BurdenSquadCount: {burdenSquadCount}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"CurPatrolType: {patrolLevel}");
    }

    public void TickLong()
    {
        if (curState == PatrolState.Invalid)
        {
            return;
        }

        if (curState == PatrolState.Ongoing)
        {
            PeriodicPatrolIncidentChecker();
        }
    }

    public void TickDay()
    {
        if (curState == PatrolState.Invalid)
        {
            return;
        }

        if ((tickToNextStage -= 60000) <= 0)
        {
            if (curState == PatrolState.Ongoing)
            {
                EndJointPatrol();
            }
            else
            {
                StartJointPatrol();
            }
        }
    }

    public bool IsParticipant(Branch branch) => participantsDict.ContainsKey(branch);

    private void AddParticipant(Branch branch)
    {
        if (curState == PatrolState.Invalid)
        {
            return;
        }

        if (!participantsDict.ContainsKey(branch))
        {
            JointBranchRecord record = new() { Branch = branch };
            participants.Add(record);
            participantsDict[branch] = record;
            branch.WorkStateDirty = true;
        }
    }

    private void RemoveParticipant(Branch branch)
    {
        if (curState == PatrolState.Invalid)
        {
            return;
        }

        if (participantsDict.Remove(branch))
        {
            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i].Branch == branch)
                {
                    participants.RemoveAt(i);
                    branch.WorkStateDirty = true;
                    break;
                }
            }

            if (curState == PatrolState.Prepare)
            {
                participatingResidentKnights.RemoveAll(ShouldRemoveResidentKnight);
            }
            else
            {
                List<Pawn> pawnToRemove = innerContainer.InnerListForReading.Where(ShouldRemoveResidentKnight).ToList();
                if (!pawnToRemove.NullOrEmpty())
                {
                    foreach (Pawn p in pawnToRemove)
                    {
                        innerContainer.Remove(p);
                    }
                }
            }
        }

        bool ShouldRemoveResidentKnight(Pawn knight)
        {
            return !ResidentKnightsManager.TryGetKnightRecord(participatingResidentKnights[i], out ResidentKnightRecord residentRecord)
                    || residentRecord.Branch == branch;
        }
    }

    public void BringResidentKnightBackTeam(ResidentKnightRecord record)
    {
        if (curState == PatrolState.Invalid)
        {
            return;
        }

        if (!participantsDict.ContainsKey(record?.Branch))
        {
            return;
        }
        if (!participatingResidentKnights.Contains(record.Knight))
        {
            participatingResidentKnights.Add(record.Knight);
        }
    }

    private bool TryStartPatrolPrep()
    {
        ClearPatrolData(forState: PatrolState.Prepare);
        /*
        * PatrolState选择联巡等级PatrolState
        */
        try
        {
            (PatrolLevel, int)[] typeChance =
            [
                (PatrolLevel.Popedom, 300),
                (PatrolLevel.Kingdom, 200 + (int)(ratkinOrder.Funds / 0.01f * 5f)),
                (PatrolLevel.Border, 10 + (int)(ratkinOrder.Funds / 0.01f * 6f + ratkinOrder.ReformationManager.ReformationsCount * 10f)),
            ];
            patrolLevel = typeChance.RandomElementByWeight(r => r.Item2).Item1;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to set group patrol type: " + ex.Message);
            patrolLevel = PatrolLevel.Popedom;
        }
        potentialIncidents.MarkDirty();

        /*
        * PatrolState选择联巡参与分队PatrolState
        */
        short reformationTags = ratkinOrder.EffectTags.GetTagCount("");

        float rate = 0.2f + (reformationTags * 0.05f);
        int participantCount = Mathf.FloorToInt(BranchManager.AllBranches.Count * rate);
        //PatrolState至少应有两个分队参与联合巡逻PatrolState
        if (participantCount <= 0)
        {
            participantCount = 2;
        }

        IEnumerable<Branch> tempEnumerables = BranchManager.AllBranches.Where(b => b.CanParticipateInJointPatrol())
                                                                       .Take(participantCount);

        foreach (Branch branch in tempEnumerables)
        {
            AddParticipant(branch);
        }

        /*
        * PatrolState终止分队当前任务丨移除无法准备联巡的分队PatrolState
        */
        List<Branch> toRemove = [];
        foreach (JointBranchRecord record in participants)
        {
            try
            {
                if (record.Branch.TaskHandler.HasTask)
                {
                    record.Branch.TaskHandler.EndCurTask(startRest: false);
                }
            }
            catch
            {
                toRemove.Add(record.Branch);
            }
        }

        if (toRemove.Count > 0)
        {
            Log.Error($"Some branches cannot prepare for the Joint Patrol of {ratkinOrder}.");
            foreach (Branch branch in toRemove)
            {
                RemoveParticipant(branch);
            }
        }

        //PatrolState无任一分队参与联巡则返回falsePatrolState
        if (participants.NullOrEmpty())
        {
            return false;
        }

        /*
        * PatrolState开始联巡准备PatrolState
        */
        tickToNextStage = JointPatrolDurationPrepDays * 60000;

        return true;
    }

    public void ChangeParticipant(IEnumerable<Branch> toAdd, IEnumerable<Branch> toRemove)
    {
        if (curState != PatrolState.Prepare)
        {
            Log.Error("Trying to change participant branch when joint patrol has started.");
            return;
        }

        if (toRemove is not null)
        {
            foreach (Branch branch in toRemove)
            {
                RemoveParticipant(branch);
            }
        }

        if (toAdd is not null)
        {
            foreach (Branch branch in toAdd)
            {
                AddParticipant(branch);
            }
        }
    }

    private void StartJointPatrol()
    {
        tickToNextStage = JointPatrolDurationDays * 60000;
        tickToNextCheck = 60000;
        curState = PatrolState.Ongoing;

        int overcap = participants.Count - burdenSquadCount;
        if (overcap > 0)
        {
            ratkinOrder.FundHandler.AdjustFundsImmediately(overcap * 0.05f, "OARO_Fund_JointPatrolOvercap".Translate());
        }

        int ticksGame = Find.TickManager.TicksGame;
        bool hasMilitary = true;
        foreach (JointBranchRecord record in participants)
        {
            record.Branch.Supply -= 0.5f;
            record.TaskPotency.MarkDirty();
            record.NextIncidentCheckTick = ticksGame + Rand.Range(2 * 2500, 4 * 2500);
            if (hasMilitary)
            {
                record.ActiveInteraction(PatrolInteractionType.Military, applyCost: false);
            }
        }
    }

    private void EndJointPatrol()
    {
        if (curState == PatrolState.Invalid)
        {
            return;
        }

        curState = PatrolState.Invalid;

        StringBuilder endText = new();
        foreach (JointBranchRecord record in participants)
        {
            record.NextIncidentCheckTick = int.MaxValue;
            record.TaskPotency.MarkDirty();
        }
        taskPotencys.MarkDirty();
        IReadOnlyDictionary<BranchTaskType, float> endTaskPotencys = taskPotencys.Value;
        IReadOnlyDictionary<BranchTaskType, List<Branch>> taskTypeBranches = participants.GroupBy(p => p.FocusedTaskType)
                                                                                         .ToDictionary(g => g.Key, g => g.Select(p => p.Branch).ToList());
        float neededTaskPotency = NeededTaskPotency;

        List<Branch> taskBranches;


        if (CompletedTaskOfType(BranchTaskType.CrimeFighting))
        {

            if (taskTypeBranches.TryGetValue(BranchTaskType.CrimeFighting, out taskBranches))
            {

            }
        }

        if (CompletedTaskOfType(BranchTaskType.StabilityMaintenance))
        {
            if (taskTypeBranches.TryGetValue(BranchTaskType.StabilityMaintenance, out taskBranches))
            {

            }
        }

        if (CompletedTaskOfType(BranchTaskType.Assistance))
        {
            if (taskTypeBranches.TryGetValue(BranchTaskType.Assistance, out taskBranches))
            {

            }
        }

        if (CompletedTaskOfType(BranchTaskType.Supervision))
        {
            if (taskTypeBranches.TryGetValue(BranchTaskType.Supervision, out taskBranches))
            {

            }
        }

        ratkinOrder.BranchManager.Notify_JointPatrolEnd();

        ClearPatrolData(forState: PatrolState.Invalid);


        bool CompletedTaskOfType(BranchTaskType taskType)
        {
            if (endTaskPotencys.TryGetValue(taskType, out float taskPotency))
            {
                return taskPotency > neededTaskPotency;
            }
            return false;
        }
    }

    private void PeriodicPatrolIncidentChecker()
    {
        int ticksGame = Find.TickManager.TicksGame;
        int triggerCount = 0;
        foreach (JointBranchRecord record in participants)
        {
            record.TaskPotency.MarkDirty();
            if (ticksGame > record.NextIncidentCheckTick)
            {
                record.NextIncidentCheckTick = ticksGame + Rand.Range(2 * 2500, 4 * 2500);
                if (triggerCount < 5 && Rand.Chance(0.05f))
                {
                    TryTriggerPatrolIncident(record);
                    triggerCount++;
                }
            }
        }
        taskPotencys.MarkDirty();
    }

    private void TryTriggerPatrolIncident(JointBranchRecord record)
    {
        try
        {
            IncidentType selIncidentType = JointPatrolIncidentDef.GetPotentialIncidentType(record);
            if (!potentialIncidents.Value.TryGetValue(selIncidentType, out List<JointPatrolIncidentDef> potentialIncidentsOfType))
            {
                return;
            }

            Branch branch = record.Branch;
            JointPatrolIncidentDef selIncident = potentialIncidentsOfType.Where(p => p.CanApply(branch)).RandomElementWithFallback(fallback: null);
            if (selIncident is null)
            {
                return;
            }
            JointIncidentRecord incidentRecord = selIncident.ApplyIncident(record);
            if (incidentRecord is not null)
            {
                incidentRecords.Add(incidentRecord);
            }
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex, "trigger patrol incident", nameof(JointPatrolManager), nameof(TryTriggerPatrolIncident), needStackTrace: true);
        }
    }

    public AcceptanceReport CanActiveParticipantInteraction(JointBranchRecord record, PatrolInteractionType interaction, Map map, bool resultOnly)
    {
        if (curState != PatrolState.Ongoing)
        {
            return false;
        }
        if (patrolInteractionAcquired.TryGetValue(interaction, out int acquiredCount))
        {
            if (acquiredCount > RatkinOrderSettings.MaxAcquiredPatrolInteractionPreType)
            {
                return resultOnly ? false : "OARO_ReachMax_AcquiredPatrolInteractionPreType".Translate();
            }
        }
        return record.CanActiveInteraction(interaction, map, resultOnly);
    }

    public void TryActiveParticipantInteraction(JointBranchRecord record, PatrolInteractionType interaction, Map map)
    {
        if (curState != PatrolState.Ongoing)
        {
            return;
        }
        if (record.ActiveInteraction(interaction, applyCost: true, map))
        {
            if (patrolInteractionAcquired.TryGetValue(interaction, out int acquiredCount))
            {
                patrolInteractionAcquired[interaction] = acquiredCount + 1;
            }
            else
            {
                patrolInteractionAcquired[interaction] = 1;
            }
        }
    }

    internal void PostLoadInit()
    {
        if (curState != PatrolState.Invalid)
        {
            if (participants.RemoveAll(r => r.Branch is null) > 0)
            {
                Log.Error($"Some participant branches of {ratkinOrder} were null after loading and have been removed.");
            }
            participantsDict = participants.GroupBy(r => r.Branch).ToDictionary(g => g.Key, g => g.First());
        }
    }

    public IThingHolder ParentHolder => null;
    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, participatingResidentKnights);
    public ThingOwner GetDirectlyHeldThings() => participatingResidentKnights;

}