using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public partial class JointPatrolManager : IExposable
{
    private const int JointPatrolDurationPrepDays = 3;
    private const int JointPatrolDurationDays = 7;

    private readonly RatkinOrder ratkinOrder;
    private BranchManager BranchManager => ratkinOrder.BranchManager;

    private bool isPatrolStarted = false;
    public bool IsPatrolStarted => isPatrolStarted;
    [Unsaved] private bool isPatrolEnded = false;

    private int tickToNextStage = 180000;
    private int tickToNextCheck = 60000;

    private int burdenSquadCount;
    public int BurdenSquadCount => burdenSquadCount;

    private PatrolLevel patrolLevel;
    public PatrolLevel PatrolLevelValue => patrolLevel;

    private List<JointBranchRecord> participants = [];
    [Unsaved] private HashSet<Branch> participantsHash = [];
    public IReadOnlyList<JointBranchRecord> Participants => participants;

    private List<JointIncidentRecord> incidentRecords = [];
    public IReadOnlyList<JointIncidentRecord> IncidentRecords => incidentRecords;

    [Unsaved] private IReadOnlyDictionary<JointPatrolIncidentDef.IncidentType, List<JointPatrolIncidentDef>> potentialIncidents;
    public IReadOnlyDictionary<JointPatrolIncidentDef.IncidentType, List<JointPatrolIncidentDef>> PotentialIncidents
    {
        get
        {
            potentialIncidents ??= DefDatabase<JointPatrolIncidentDef>.AllDefsListForReading.Where(d => !d.patrolLevelLimits.HasValue || d.patrolLevelLimits.Value == patrolLevel)
                                                                                            .GroupBy(d => d.incidentType)
                                                                                            .ToDictionary(g => g.Key, g => g.ToList());
            return potentialIncidents;
        }
    }

    private float curReconnaissance;
    public float NeedReconnaissanceValue
    {
        get
        {
            return patrolLevel switch
            {
                PatrolLevel.Popedom => BranchManager.TotalKnights * 0.16f * 10f,
                PatrolLevel.Kingdom => BranchManager.TotalKnights * 0.3f * 10f,
                PatrolLevel.Border => BranchManager.TotalKnights * 0.44f * 10f,
                _ => 0f,
            };
        }
    }

    public JointPatrolManager(RatkinOrder ratkinOrder)
    {
        this.ratkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref isPatrolStarted, "isPatrolStarted", defaultValue: false);
        Scribe_Values.Look(ref tickToNextStage, "tickToNextStage", 1800000);
        Scribe_Values.Look(ref tickToNextCheck, "tickToStart", 60000);

        Scribe_Values.Look(ref patrolLevel, "CurPatrolType", PatrolLevel.Popedom);
        Scribe_Values.Look(ref burdenSquadCount, "burdenSquadCount", 0);

        Scribe_Values.Look(ref curReconnaissance, "curReconnaissance", 0f);

        Scribe_Collections.Look(ref participants, "participants", LookMode.Deep);
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_JointPatrolManager(ratkinOrder));
    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"IsPatrolStarted: {isPatrolStarted}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"TickToNextStage: {tickToNextStage}");
        listing_Rect.Label($"TickToNextCheck: {tickToNextCheck}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"BurdenSquadCount: {burdenSquadCount}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"CurPatrolType: {patrolLevel}");
        listing_Rect.Label($"CurReconnaissance: {curReconnaissance}");
    }

    public void TickLong()
    {
        if (isPatrolEnded || !isPatrolStarted)
        {
            return;
        }

        if ((tickToNextCheck -= 1000) <= 0)
        {
            tickToNextCheck = 10000;
            curReconnaissance = 0f;
            for (int i = 0; i < participants.Count; i++)
            {
                participants[i].RecordUpdate();
                curReconnaissance += participants[i].Reconnbissance;
            }
        }
    }

    public void TickDay()
    {
        if (isPatrolEnded)
        {
            return;
        }

        if ((tickToNextStage -= 60000) <= 0)
        {
            if (isPatrolStarted)
            {
                EndJointPatrol();
            }
            else
            {
                StartJointPatrol();
            }
            return;
        }
    }

    public bool IsParticipant(Branch branch) => participantsHash.Contains(branch);

    private void AddParticipant(Branch branch)
    {
        if (participantsHash.Add(branch))
        {
            participants.Add(new JointBranchRecord() { Branch = branch });
            branch.WorkStateDirty = true;
        }
    }

    private void RemoveParticipant(Branch branch)
    {
        if (participantsHash.Remove(branch))
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
        }
    }

    public bool TryStartPatrolPrep()
    {
        /*
        * 选择联巡等级
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
            Log.Error("Failed to set group patrol type: " + ex);
            patrolLevel = PatrolLevel.Popedom;
        }

        /*
        * 选择联巡参与分队
        */
        short reformationTags = ratkinOrder.EffectTags.GetTagCount("");

        float rate = 0.2f + (reformationTags * 0.05f);
        int participantCount = Mathf.FloorToInt(BranchManager.AllBranches.Count * rate);
        //至少应有两个分队参与联合巡逻
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
        * 终止分队当前任务丨移除无法准备联巡的分队
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

        //无任一分队参与联巡则返回false
        if (participants.NullOrEmpty())
        {
            return false;
        }

        /*
        * 开始联巡准备
        */
        tickToNextStage = JointPatrolDurationPrepDays * 60000;

        return true;
    }

    public void ChangeParticipant(IEnumerable<Branch> toAdd, IEnumerable<Branch> toRemove)
    {
        if (isPatrolStarted)
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
        isPatrolStarted = true;

        int overcap = participants.Count - burdenSquadCount;
        if (overcap > 0)
        {
            ratkinOrder.FundHandler.AdjustFundsImmediately(overcap * 0.05f, "OARO_Fund_JointPatrolOvercap".Translate());
        }

        foreach (JointBranchRecord record in participants)
        {
            record.Branch.Supply -= 0.5f;
        }
    }

    private void EndJointPatrol()
    {
        isPatrolEnded = true;
        if (participants.NullOrEmpty())
        {
            ratkinOrder.BranchManager.Notify_JointPatrolEnd();
            return;
        }

        curReconnaissance = 0f;
        StringBuilder endText = new();
        foreach (JointBranchRecord record in participants)
        {
            EndJointPatrol(record, endText);
        }
    }

    private void EndJointPatrol(JointBranchRecord record, StringBuilder endText)
    {
        record.RecordUpdate();
        curReconnaissance += record.Reconnbissance;


    }

    private void PeriodicPatrolIncidentChecker()
    {
        if (!isPatrolStarted)
        {
            return;
        }
        int ticksGame = Find.TickManager.TicksGame;
        for (int i = 0; i < participants.Count; i++)
        {
            if (ticksGame > participants[i].NextIncidentCheckTick)
            {
                participants[i].NextIncidentCheckTick = ticksGame + Rand.Range(2 * 2500, 4 * 2500);
                if (Rand.Chance(0.05f))
                {
                    return;
                }
            }
        }
    }

    private void TryTriggerPatrolIncident(JointBranchRecord record)
    {
        JointPatrolIncidentDef.IncidentType selIncidentType = JointPatrolIncidentDef.GetPotentialIncidentType(record);
        if (!PotentialIncidents.TryGetValue(selIncidentType, out List<JointPatrolIncidentDef> potentialIncidentsOfType))
        {
            return;
        }

        Branch branch = record.Branch;
        JointPatrolIncidentDef selIncident = potentialIncidentsOfType.Where(p => p.CanApply(branch)).RandomElementWithFallback(fallback: null);
        if (selIncident is null)
        {
            return;
        }
        selIncident.ApplyIncident(branch, out string description);
        incidentRecords.Add(new JointIncidentRecord()
        {
            Def = selIncident,
            RelatedBranch = branch,
            Description = description,
            TriggerTick = Find.TickManager.TicksGame
        });
    }

    internal void PostLoadInit()
    {
        if (participants.RemoveAll(r => r.Branch is null) > 0)
        {
            Log.Error($"Some participant branches of {ratkinOrder} were null after loading and have been removed.");
        }

        participantsHash = participants.Select(r => r.Branch).ToHashSet();
    }
}