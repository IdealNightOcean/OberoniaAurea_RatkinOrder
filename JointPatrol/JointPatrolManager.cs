using NightOcean;
using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

using IncidentType = JointPatrolIncidentDef.IncidentType;
using PatrolInteractionType = JointBranchRecord.PatrolInteractionType;

public partial class JointPatrolManager : IExposable, IThingHolder, IPawnRetentionHolder
{
    private const int JointPatrolDurationPrepDays = 3;
    private const int JointPatrolDurationDays = 7;
    private static readonly FloatRange FirstJointPatrolInterval = new(20f, 40f);
    private static readonly FloatRange JointPatrolInterval = new(40f, 60f);

    private readonly RatkinOrder ratkinOrder;
    private BranchManager BranchManager => ratkinOrder.BranchManager;

    private PatrolState curState;
    public PatrolState CurState => curState;

    private HelpPolicy curHelpPolicy;
    public HelpPolicy CurHelpPolicy => curHelpPolicy;

    private int tickToNextStage = JointPatrolDurationPrepDays * 60000;
    public int TickToNextStage => tickToNextStage;

    private int burdenCount;
    public int BurdenCount => burdenCount;

    private PatrolLevel patrolLevel;
    public PatrolLevel PatrolLevelValue => patrolLevel;

    private List<JointBranchRecord> participants = [];

    [Unsaved] private Dictionary<Branch, JointBranchRecord> participantsDict = [];
    public IReadOnlyDictionary<Branch, JointBranchRecord> ParticipantsDict => participantsDict;

    private List<ResidentKnightRecord> participatingResidentKnights = [];
    public IReadOnlyList<ResidentKnightRecord> ParticipatingResidentKnights => participatingResidentKnights;
    private ThingOwner<Pawn> innerContainer;

    private Dictionary<PatrolInteractionType, int> patrolInteractionAcquired = [];

    private List<JointInteractionRecord> interactionRecords = [];
    public IReadOnlyList<JointInteractionRecord> InteractionRecords => interactionRecords;

    public LazyMutable<IReadOnlyDictionary<BranchTaskType, float>> TaskPotencys { get; }
    private LazyMutable<IReadOnlyDictionary<IncidentType, List<JointPatrolIncidentDef>>> PotentialIncidents { get; }

    private int sacrificeCount;
    public int SacrificeCount => sacrificeCount;

    private int curHelpCount;
    private int nextHelpCheckTick = -1;
    private int HelpCeiling
    {
        get
        {
            return ratkinOrder.Relationship switch
            {
                EsteemHandler.RelationshipKind.Soulmate => 8,
                EsteemHandler.RelationshipKind.Trustworthy => 6,
                EsteemHandler.RelationshipKind.Friendly => 4,
                EsteemHandler.RelationshipKind.Acquaintance => 2,
                _ => 0
            };
        }
    }
    private int HelpCheckInterval
    {
        get
        {
            return ratkinOrder.Relationship switch
            {
                EsteemHandler.RelationshipKind.Soulmate => 15000,
                EsteemHandler.RelationshipKind.Trustworthy => 20000,
                EsteemHandler.RelationshipKind.Friendly => 30000,
                EsteemHandler.RelationshipKind.Acquaintance => 60000,
                _ => 60000
            };
        }
    }
    private float HelpTriggerChance
    {
        get
        {
            return CurHelpPolicy switch
            {
                HelpPolicy.None => 0f,
                HelpPolicy.OnlyFriendly => 0.125f,
                HelpPolicy.All => 0.25f,
                _ => 0f
            };
        }
    }

    private string completionSummary = string.Empty;
    public string CompletionSummary => completionSummary;

    public float NeededTaskPotency
    {
        get
        {
            return patrolLevel switch
            {
                PatrolLevel.Popedom => BranchManager.TotalKnightsCount.Value * 0.16f * 10f * 0.25f,
                PatrolLevel.Kingdom => BranchManager.TotalKnightsCount.Value * 0.3f * 10f * 0.25f,
                PatrolLevel.Border => BranchManager.TotalKnightsCount.Value * 0.44f * 10f * 0.25f,
                _ => 0f,
            };
        }
    }

    public JointPatrolManager(RatkinOrder ratkinOrder)
    {
        this.ratkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        TaskPotencys = new(refreshFunc: delegate
        {
            if (curState != PatrolState.Ongoing)
            {
                return new Dictionary<BranchTaskType, float>();
            }
            return participants.GroupBy(p => p.FocusedTaskType).ToDictionary(g => g.Key, g => g.Sum(gp => gp.TaskPotency.Value));
        });

        PotentialIncidents = new(refreshFunc: delegate
        {
            if (curState != PatrolState.Ongoing)
            {
                return new Dictionary<IncidentType, List<JointPatrolIncidentDef>>();
            }
            return DefDatabase<JointPatrolIncidentDef>.AllDefsListForReading.Where(d => !d.patrolLevelLimits.HasValue || d.patrolLevelLimits.Value == patrolLevel)
                                                                            .GroupBy(d => d.incidentType)
                                                                            .ToDictionary(g => g.Key, g => g.ToList());
        });

        innerContainer = new ThingOwner<Pawn>(this)
        {
            removeContentsIfDestroyed = true,
            contentsLookMode = LookMode.Deep
        };
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref curState, nameof(curState), PatrolState.Invalid);

        Scribe_Values.Look(ref tickToNextStage, nameof(tickToNextStage), 1800000);

        Scribe_Values.Look(ref patrolLevel, nameof(patrolLevel), PatrolLevel.Popedom);
        Scribe_Values.Look(ref burdenCount, nameof(burdenCount), 0);
        Scribe_Values.Look(ref sacrificeCount, nameof(sacrificeCount), 0);

        Scribe_Values.Look(ref curHelpPolicy, nameof(curHelpPolicy), HelpPolicy.None);
        Scribe_Values.Look(ref curHelpCount, nameof(curHelpCount), 0);
        Scribe_Values.Look(ref nextHelpCheckTick, nameof(nextHelpCheckTick), -1);

        Scribe_Values.Look(ref completionSummary, nameof(completionSummary), string.Empty);

        Scribe_Collections.Look(ref participants, nameof(participants), LookMode.Deep);
        Scribe_Collections.Look(ref patrolInteractionAcquired, nameof(patrolInteractionAcquired), LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref participatingResidentKnights, nameof(participatingResidentKnights), LookMode.Reference);
        Scribe_Collections.Look(ref interactionRecords, nameof(interactionRecords), LookMode.Deep);

        Scribe_Deep.Look(ref innerContainer, nameof(innerContainer));
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_JointPatrolManager(ratkinOrder));
    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"当前阶段: {curState}");
        listing_Rect.Label($"距下一阶段 (Tick): {tickToNextStage}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"BurdenSquadCount: {burdenCount}");
        listing_Rect.Gap(12f);

        if (curState != PatrolState.Invalid)
        {
            listing_Rect.Label($"当前联巡等级: {patrolLevel}");
            listing_Rect.Label($"参与分部数: {participantsDict.Count}");
            listing_Rect.Label($"已归队常驻骑士数: {participatingResidentKnights.Count} | {innerContainer.Count}");
            listing_Rect.SubLabel("左右两值应该相等，否则大概率是有问题的，可能导致某些常驻骑士无法返回", widthPct: 0.8f);
            listing_Rect.Label($"骑士牺牲数量: {sacrificeCount}");
        }
        listing_Rect.Label($"当前求助接取策略: {CurHelpPolicy}");
        if (listing_Rect.ButtonText("改变求助接取策略", widthPct: 0.5f))
        {
            ChangeHelpPolicy();
        }
        listing_Rect.Label($"可接取求助上限: {HelpCeiling}");
        listing_Rect.Label($"求助生成检测间隔 (Tick): {HelpCeiling}");
        listing_Rect.Label($"下一次求助生成检测时刻 (Tick): {nextHelpCheckTick}");
        if (curState == PatrolState.Ongoing)
        {
            if (listing_Rect.ButtonText("触发联巡事件", widthPct: 0.5f))
            {
                TryTriggerPatrolIncident(participants.RandomElement());
            }
            if (listing_Rect.ButtonText("触发联巡求助", widthPct: 0.5f))
            {
                TryTriggerCaravanHelp();
            }
        }

        listing_Rect.Gap(12f);
        listing_Rect.Label("上一次联巡总结");
        listing_Rect.SliderLabeled(completionSummary, 50f, 0f, 50f);
        if (listing_Rect.ButtonText("查看联巡事件"))
        {
            StringBuilder sb = new(128);
            int i = 0;
            foreach (var record in interactionRecords)
            {
                sb.AppendLine($"{++i}. {record}");
            }
            Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(sb.ToString(), ratkinOrder);
            Find.WindowStack.Add(nodeTree);
        }
        if (listing_Rect.ButtonText("快速进入下一阶段", widthPct: 0.5f))
        {
            tickToNextStage = 0;
        }
    }

    public void TickLong()
    {
        if ((tickToNextStage -= 1000) <= 0)
        {
            switch (curState)
            {
                case PatrolState.Invalid: TryStartPatrolPrep(); return;
                case PatrolState.Prepare: StartJointPatrol(); return;
                case PatrolState.Ongoing: EndJointPatrol(); return;
                default: tickToNextStage = 5 * 60000; return;
            }
        }

        if (curState == PatrolState.Ongoing)
        {
            PeriodicPatrolInteractionChecker();
        }
    }

    public bool IsParticipant(Branch branch) => participantsDict.ContainsKey(branch);
    public bool TryGetJointBranchRecord(Branch branch, out JointBranchRecord record) => participantsDict.TryGetValue(branch, out record);

    public void ChangeParticipant(IEnumerable<Branch> toAdd, IEnumerable<Branch> toRemove)
    {
        if (curState != PatrolState.Prepare)
        {
            Log.Error("[OARO] Trying to change participant branch when joint patrol has started.");
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
        if (curState != PatrolState.Ongoing || record is null)
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

    public void OnKnightSacrifice(int sacrificeCount) => this.sacrificeCount = Mathf.Max(0, this.sacrificeCount + sacrificeCount);

    public bool MarkResidentKnightBackTeam(ResidentKnightRecord record)
    {
        if (curState == PatrolState.Invalid)
        {
            Log.Error("[OARO] Trying to bring back resident knight when joint patrol is invalid.");
            return false;
        }

        if (!participantsDict.ContainsKey(record?.Branch))
        {
            return false;
        }
        if (!participatingResidentKnights.Contains(record))
        {
            participatingResidentKnights.Add(record);
        }
        return true;
    }

    public void OnResidentKnightBackTeam(Pawn knight)
    {
        if (!ResidentKnightsManager.Instance.TryGetKnightRecord(knight, out ResidentKnightRecord record))
        {
            return;
        }
        if (innerContainer.TryAddOrTransfer(knight))
        {
            if (!participantsDict.TryGetValue(record.Branch, out JointBranchRecord pRecord))
            {
                pRecord.PotencyOffset += record.CurRank switch
                {
                    ResidentKnightRecord.Rank.Regular => 5,
                    ResidentKnightRecord.Rank.Elite => 10,
                    ResidentKnightRecord.Rank.Honor => 20,
                    ResidentKnightRecord.Rank.Crown => 40,
                    _ => 5
                };
            }
        }
        else
        {
            participatingResidentKnights.Remove(record);
        }
    }

    public void OnResidentKnightRemoved(ResidentKnightRecord record)
    {
        participatingResidentKnights?.Remove(record);
        innerContainer?.Remove(record.Knight);
    }

    public void ChangeHelpPolicy()
    {
        curHelpPolicy = (HelpPolicy)(((int)curHelpPolicy + 1) % 3);
    }

    private void AddParticipant(Branch branch)
    {
        if (curState != PatrolState.Prepare)
        {
            Log.Error("[OARO] Trying to add participant branch when joint patrol has started or ended.");
            return;
        }

        if (!participantsDict.ContainsKey(branch))
        {
            JointBranchRecord record = new() { Branch = branch };
            participants.Add(record);
            participantsDict[branch] = record;
            branch.MarkWorkStateDirty();
        }
    }
    private void RemoveParticipant(Branch branch)
    {
        if (curState == PatrolState.Invalid)
        {
            return;
        }

        if (participantsDict.TryGetValue(branch, out JointBranchRecord record))
        {
            participantsDict.Remove(branch);
            participants.Remove(record);
            branch.MarkWorkStateDirty();

            participatingResidentKnights.RemoveAll(r => r is null || r.Branch == branch);

            if (curState == PatrolState.Ongoing)
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
            return !ResidentKnightsManager.Instance.TryGetKnightRecord(knight, out ResidentKnightRecord residentRecord) || residentRecord.Branch == branch;
        }
    }

    private void ClearPatrolData(PatrolState forState, bool forceClear = false)
    {
        if (!forceClear && curState == PatrolState.Ongoing)
        {
            Log.Error("[OARO] Trying to clear joint patrol data when joint patrol is ongoing.");
            return;
        }

        burdenCount = 0;

        curHelpCount = 0;
        nextHelpCheckTick = -1;

        tickToNextStage = forState switch
        {
            PatrolState.Invalid => (int)(JointPatrolInterval.RandomInRange * 60000f),
            PatrolState.Prepare => JointPatrolDurationPrepDays * 60000,
            _ => 5 * 60000
        };

        participants.Clear();
        participantsDict.Clear();

        participatingResidentKnights.Clear();
        if (!forceClear && innerContainer.Count > 0)
        {
            Log.Error("[OARO] Trying to clear joint patrol data when inner container is not empty.");
        }
        innerContainer.Clear();

        TaskPotencys.Reset();
        PotentialIncidents.Reset();

        if (forceClear || forState == PatrolState.Prepare)
        {
            patrolLevel = default;
            sacrificeCount = 0;
            completionSummary = string.Empty;
            interactionRecords.Clear();
        }

        curState = forState;
    }

    public void TryStartPatrolPrep()
    {
        if (curState != PatrolState.Invalid)
        {
            Log.Error("[OARO] Trying to start joint patrol prep when joint patrol is already started.");
            return;
        }

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
            ModUtility.LogExceptionError(ex, "set joint patrol level", nameof(JointPatrolManager), nameof(TryStartPatrolPrep), needStackTrace: true);
            patrolLevel = PatrolLevel.Popedom;
        }
        PotentialIncidents.MarkDirty();

        /*
        * PatrolState选择联巡参与分队PatrolState
        */
        short reformationTags = ratkinOrder.EffectTags.GetTagCount("");

        float rate = 0.2f + (reformationTags * 0.05f);
        int participantCount = Mathf.CeilToInt(BranchManager.AllBranches.Count * rate);
        //PatrolState至少应有两个分队参与联合巡逻PatrolState
        if (participantCount <= 1)
        {
            participantCount = 2;
        }
        burdenCount = participantCount;

        IEnumerable<Branch> tempEnumerables = BranchManager.AllBranches.Where(b => b.CanParticipateInJointPatrolFast())
                                                                       .Take(participantCount);

        foreach (Branch branch in tempEnumerables)
        {
            AddParticipant(branch);
        }

        /*
        * PatrolState终止分队当前任务丨移除无法准备联巡的分队PatrolState
        */
        List<Branch> toRemove = [];
        foreach (Branch branch in participantsDict.Keys)
        {
            try
            {
                if (branch.TaskHandler.HasTask)
                {
                    branch.TaskHandler.EndCurTask(startRest: false);
                }
            }
            catch
            {
                toRemove.Add(branch);
            }
        }

        if (toRemove.Count > 0)
        {
            Log.Error($"[OARO] Some branches cannot prepare for the Joint Patrol of {ratkinOrder}.");
            foreach (Branch branch in toRemove)
            {
                RemoveParticipant(branch);
            }
        }

        //PatrolState无任一分队参与联巡则清理，并在5~10天后尝试重新开始
        if (participants.NullOrEmpty())
        {
            Log.Error($"[OARO] No branch can participate in the Joint Patrol of {ratkinOrder}.");
            curState = PatrolState.Invalid;
            ClearPatrolData(forState: PatrolState.Invalid);
            tickToNextStage = (int)(Rand.Range(5f, 10f) * 60000);
            return;
        }

        /*
        * PatrolState开始联巡准备PatrolState
        */
        curState = PatrolState.Prepare;
        tickToNextStage = JointPatrolDurationPrepDays * 60000;

        return;
    }

    public bool ApplyJointCaravanHelpEffect(JointPatrolCaravanHelpDef def, Branch branch)
    {
        if (curState != PatrolState.Ongoing)
        {
            Log.Error("[OARO] Trying to apply a joint-patrol-caravan-incident when joint patrol is not ongoing.");
            return false;
        }

        if (!participantsDict.TryGetValue(branch, out JointBranchRecord record))
        {
            return false;
        }

        ApplyJointInteractionEffect(def, record);
        return true;
    }

    private void StartJointPatrol()
    {
        if (curState != PatrolState.Prepare)
        {
            Log.Error("[OARO] Trying to start joint patrol when joint patrol is not in prepare state.");
            return;
        }

        tickToNextStage = JointPatrolDurationDays * 60000;
        nextHelpCheckTick = Find.TickManager.TicksGame + HelpCheckInterval;

        curState = PatrolState.Ongoing;

        int overcap = participants.Count - burdenCount;
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

        BringResidentKnightBackTeam();

        TaskPotencys.MarkDirty();
        PotentialIncidents.MarkDirty();
    }

    private void BringResidentKnightBackTeam()
    {
        if (curState != PatrolState.Ongoing)
        {
            Log.Error("[OARO] Trying to start joint patrol when joint patrol is not in ongoing state.");
            return;
        }

        participatingResidentKnights.RemoveAll(r => !r.IsValid && !r.Knight.Spawned);
        foreach (ResidentKnightRecord record in participatingResidentKnights)
        {
            record.Knight.SetFaction(record.RatkinOrder.Faction);
        }
        if (participatingResidentKnights.Count > 0)
        {
            Dictionary<Map, List<Pawn>> lordMapDict = participatingResidentKnights.Select(r => r.Knight).GroupBy(p => p.Map).ToDictionary(g => g.Key, g => g.ToList());
            foreach (KeyValuePair<Map, List<Pawn>> kv in lordMapDict)
            {
                LordMaker.MakeNewLord(ratkinOrder.Faction, new LordJob_ExitMapBestForJointPatrol(ratkinOrder), kv.Key, startingPawns: kv.Value);
            }
        }
    }

    private void EndJointPatrol()
    {
        if (curState != PatrolState.Ongoing)
        {
            Log.Error("[OARO] Trying to end joint patrol when joint patrol is not ongoing.");
            return;
        }

        curState = PatrolState.Invalid;

        try
        {
            BringResidentKnightBackPlayer();
        }
        catch (Exception ex1)
        {
            ModUtility.LogExceptionError(ex1, "bring resident knights back to player", nameof(JointPatrolManager), nameof(EndJointPatrol), needStackTrace: true);
        }

        try
        {
            participants.RemoveAll(r => r is null || r.Branch is null);
            participantsDict.RemoveAll(kv => kv.Key is null || kv.Value is null);

            foreach (JointBranchRecord record in participants)
            {
                record.NextIncidentCheckTick = int.MaxValue;
                record.TaskPotency.MarkDirty();
            }
            TaskPotencys.MarkDirty();

            float fundGain = 0f;
            float reformationGain = 0f;
            int populationGain = 0;
            float publicSecurityGain = 0f;
            float participantPublicSecurityGain = 0f;

            float neededTaskPotency = NeededTaskPotency;
            IReadOnlyDictionary<BranchTaskType, float> endTaskPotencys = TaskPotencys.Value;
            IReadOnlyDictionary<BranchTaskType, List<Branch>> taskTypeBranches = participants.GroupBy(p => p.FocusedTaskType)
                                                                                             .ToDictionary(g => g.Key, g => g.Select(p => p.Branch).ToList());
            List<Branch> taskBranches = [];
            List<BranchTaskType> succeedTaksTypes = [];
            List<BranchTaskType> failedTaksTypes = [];
            try
            {
                if (CompletedTaskOfType(BranchTaskType.CrimeFighting))
                {
                    participantPublicSecurityGain += Rand.Range(0.05f, 0.15f);
                    fundGain += 0.05f;
                    TaskOfTypeSuccess(BranchTaskType.CrimeFighting, BranchMedalDefOf.OARO_Courage);
                }
                else
                {
                    TaskOfTypeFail(BranchTaskType.CrimeFighting);
                }

                if (CompletedTaskOfType(BranchTaskType.StabilityMaintenance))
                {
                    populationGain += Rand.Range(50, 150);
                    fundGain += 0.05f;
                    TaskOfTypeSuccess(BranchTaskType.StabilityMaintenance, BranchMedalDefOf.OARO_Tenacity);
                }
                else
                {
                    TaskOfTypeFail(BranchTaskType.StabilityMaintenance);
                }

                if (CompletedTaskOfType(BranchTaskType.Assistance))
                {
                    populationGain += Rand.Range(50, 150);
                    reformationGain += 10f;
                    TaskOfTypeSuccess(BranchTaskType.Assistance, BranchMedalDefOf.OARO_Rescue);
                }
                else
                {
                    TaskOfTypeFail(BranchTaskType.Assistance);
                }

                if (CompletedTaskOfType(BranchTaskType.Supervision))
                {
                    participantPublicSecurityGain += Rand.Range(0.05f, 0.15f);
                    reformationGain += 10f;
                    TaskOfTypeSuccess(BranchTaskType.Supervision, BranchMedalDefOf.OARO_Justice);
                }
                else
                {
                    TaskOfTypeFail(BranchTaskType.Supervision);
                }
            }
            catch (Exception subEx1)
            {
                ModUtility.LogExceptionError(subEx1, "calculate joint patrol task results", nameof(JointPatrolManager), nameof(EndJointPatrol), needStackTrace: true);
            }

            switch (patrolLevel)
            {
                case PatrolLevel.Kingdom:
                    fundGain *= 2f;
                    reformationGain *= 2f;
                    break;
                case PatrolLevel.Border:
                    fundGain *= 3f;
                    reformationGain *= 3f;
                    break;
                default:
                    break;
            }

            int publicSecurityUpCount = 0;
            int publicSecurityDownCount = 0;
            try
            {
                ratkinOrder.FundHandler.AdjustFundsImmediately(fundGain, "OARO_Fund_JointPatrolCompletion".Translate());
                ratkinOrder.ReformationManager.ReformProgress += reformationGain;

                foreach (Branch branch in ratkinOrder.BranchManager.AllBranches)
                {
                    float publicSecurityChange = participantsDict.ContainsKey(branch) ? participantPublicSecurityGain + publicSecurityGain : publicSecurityGain;
                    branch.PopulationHandler.PublicSecurity += publicSecurityChange;
                    if (publicSecurityChange > 0f)
                    {
                        publicSecurityUpCount++;
                    }
                    else if (publicSecurityChange < 0f)
                    {
                        publicSecurityDownCount++;
                    }
                }

                foreach (KeyValuePair<Branch, JointBranchRecord> kv in participantsDict)
                {
                    kv.Key.PopulationHandler.Population += populationGain;
                    if (kv.Value.HasInteraction(PatrolInteractionType.Diplomacy))
                    {
                        BranchMedalDef medalDef = DefDatabase<BranchMedalDef>.GetRandom();
                        kv.Key.MedalHandler.AddMedal(medalDef);
                    }
                }
            }
            catch (Exception subEx2)
            {
                ModUtility.LogExceptionError(subEx2, "apply joint patrol results", nameof(JointPatrolManager), nameof(EndJointPatrol), needStackTrace: true);
            }

            try
            {
                GrammarRequest grammarRequest = new()
                {
                    Includes = { OARO_RulePackDefOf.OARO_JointPatrolCompletion }
                };
                grammarRequest.Rules.AddRange(ModUtility.RulesForRatkinOrder("ORDER", ratkinOrder));
                grammarRequest.Rules.Add(new Rule_String("patrolLevel", $"OARO_JointPatrolLevel_{patrolLevel}".Translate()));
                grammarRequest.Rules.Add(new Rule_String("participantsCount", participants.Count.ToString()));

                grammarRequest.Constants.Add("sacrificeCount", sacrificeCount.ToString());
                grammarRequest.Rules.Add(new Rule_String("sacrificeCount", sacrificeCount.ToString()));

                grammarRequest.Rules.Add(new Rule_String("fundGain", fundGain.ToStringPercentSigned("0.##").Colorize(fundGain > 0f ? Color.green : ColorLibrary.RedReadable)));
                grammarRequest.Rules.Add(new Rule_String("reformationGain", reformationGain.ToStringWithSign("0.##").Colorize(reformationGain > 0f ? Color.green : ColorLibrary.RedReadable)));
                grammarRequest.Rules.Add(new Rule_String("publicSecurityUpCount", publicSecurityUpCount.ToString()));
                grammarRequest.Rules.Add(new Rule_String("publicSecurityDownCount", publicSecurityDownCount.ToString()));
                grammarRequest.Rules.Add(new Rule_String("totalPopulationGain", (populationGain * participants.Count).ToString()));

                grammarRequest.Constants.Add("succeedTaksTypeCount", succeedTaksTypes.Count.ToString());
                grammarRequest.Constants.Add("failedTaksTypeCount", failedTaksTypes.Count.ToString());
                grammarRequest.Rules.Add(new Rule_String("succeedTaksTypeCount", succeedTaksTypes.Count.ToString()));
                grammarRequest.Rules.Add(new Rule_String("failedTaksTypeCount", failedTaksTypes.Count.ToString()));

                grammarRequest.Rules.Add(new Rule_String("succeedTaksTypeNames", string.Join(", ", succeedTaksTypes.Select(t => $"OARO_JointPatrolTaskType_{t}".Translate()))));
                grammarRequest.Rules.Add(new Rule_String("failedTaksTypeNames", string.Join(", ", failedTaksTypes.Select(t => $"OARO_JointPatrolTaskType_{t}".Translate()))));
                completionSummary = GrammarResolver.Resolve("r_text", grammarRequest);
            }
            catch (Exception subEx3)
            {
                ModUtility.LogExceptionError(subEx3, "generate joint patrol completion summary", nameof(JointPatrolManager), nameof(EndJointPatrol), needStackTrace: true);
                completionSummary = "ERROR (；′⌒`)".Colorize(ColorLibrary.RedReadable);
            }

            OrderLetterUtility.ReceiveLetter(
                label: "OARO_JointPatrolCompletionSummary".Translate(ratkinOrder.Name.Named("ORDER_name")),
                text: completionSummary,
                def: OrderLetterDefOf.OARO_OfficialLetter,
                relatedOrder: ratkinOrder,
                sender: ratkinOrder.Name,
                relatedLetterType: OrderLetter.RelatedLetterType.Neutral);

            bool CompletedTaskOfType(BranchTaskType taskType)
            {
                if (endTaskPotencys?.TryGetValue(taskType, out float taskPotency) ?? false)
                {
                    return taskPotency > neededTaskPotency;
                }
                return false;
            }

            void TaskOfTypeSuccess(BranchTaskType taskType, BranchMedalDef medalDef)
            {
                succeedTaksTypes.Add(taskType);
                if (taskTypeBranches?.TryGetValue(taskType, out taskBranches) ?? false)
                {
                    foreach (Branch branch in taskBranches)
                    {
                        branch?.MedalHandler.AddMedal(medalDef);
                    }
                }
            }

            void TaskOfTypeFail(BranchTaskType taskType)
            {
                fundGain -= 0.03f;
                publicSecurityGain -= 0.025f;
                failedTaksTypes.Add(taskType);
            }
        }
        catch (Exception ex2)
        {
            ModUtility.LogExceptionError(ex2, "finalize joint patrol", nameof(JointPatrolManager), nameof(EndJointPatrol), needStackTrace: true);
        }

        ClearPatrolData(forState: PatrolState.Invalid);
        curState = PatrolState.Invalid;
        tickToNextStage = (int)(Rand.Range(55f, 65f) * 60000);
    }

    private void PeriodicPatrolInteractionChecker()
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

        if (ticksGame > nextHelpCheckTick)
        {
            nextHelpCheckTick = ticksGame + HelpCheckInterval;
            if (CurHelpPolicy != HelpPolicy.None && curHelpCount < HelpCeiling && Rand.Chance(HelpTriggerChance))
            {
                TryTriggerCaravanHelp();
            }
        }

        TaskPotencys.MarkDirty();
    }

    private void TryTriggerCaravanHelp()
    {
        Branch targetBranch;
        if (CurHelpPolicy == HelpPolicy.OnlyFriendly)
        {
            targetBranch = participantsDict.Keys.Where(b => b.IsBranchOfType(Branch.BranchType.Friendly)).RandomElementWithFallback(fallback: null);
        }
        else
        {
            targetBranch = participantsDict.Keys.RandomElementWithFallback(fallback: null);
        }

        if (targetBranch is null)
        {
            return;
        }

        JointPatrolCaravanHelpDef caravanHelpDef = DefDatabase<JointPatrolCaravanHelpDef>.AllDefsListForReading.Where(d => d.CanApplyOn(targetBranch, patrolLevel)).RandomElementWithFallback(fallback: null);
        if (caravanHelpDef is null)
        {
            return;
        }

        Slate slate = new();
        slate.Set("caravanHelpDef", caravanHelpDef);
        slate.Set("caravanHelpSiteDef", caravanHelpDef.relatedWorldObject);
        slate.Set("timeOutTicks", caravanHelpDef.timeOutTicks);
        try
        {
            slate.SetBasicBranchSlateVar(targetBranch, alsoSetOrder: true);
            slate.Set("helpDescription", caravanHelpDef.Worker.RequestHelpReason(targetBranch));
            slate.Set("map", OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false));
        }
        catch { }

        if (OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Quest_JointPatrolCaravanHelp, slate, forced: true))
        {
            curHelpCount++;
        }
    }

    private void TryTriggerPatrolIncident(JointBranchRecord record)
    {
        try
        {
            IncidentType selIncidentType = JointPatrolIncidentDef.GetPotentialIncidentType(record);
            if (!PotentialIncidents.Value.TryGetValue(selIncidentType, out List<JointPatrolIncidentDef> potentialIncidentsOfType))
            {
                return;
            }

            Branch branch = record.Branch;
            JointPatrolIncidentDef selIncident = potentialIncidentsOfType.Where(p => p.CanApplyOn(branch, patrolLevel)).RandomElementWithFallback(fallback: null);
            if (selIncident is null)
            {
                return;
            }
            ApplyJointInteractionEffect(selIncident, record);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex, "trigger patrol incident", nameof(JointPatrolManager), nameof(TryTriggerPatrolIncident), needStackTrace: true);
        }
    }

    private void BringResidentKnightBackPlayer()
    {
        if (curState != PatrolState.Invalid)
        {
            Log.Error("[OARO] Trying to bring back resident knight when joint patrol is not ended.");
            return;
        }
        if (innerContainer.Count == 0)
        {
            return;
        }

        List<Pawn> residentKnights = innerContainer.InnerListForReading.ToList();
        Slate slate = new();
        slate.SetBasicOrderSlateVar(ratkinOrder);
        slate.Set("residentKnights", residentKnights);
        OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Quest_ResidentKnightBackPlayer, slate, forced: true);

        innerContainer.Clear();
    }

    internal void PostLoadInit()
    {
        // 使用非短路的 | 运算符，以确保两个列表都会被清理
        if (participatingResidentKnights.RemoveAll(k => k is null) > 0 | innerContainer.RemoveAll(p => p is null) > 0)
        {
            Log.Error($"[OARO] Some participating resident knights of {ratkinOrder} were null after loading and have been removed.");
        }
        if (curState != PatrolState.Invalid)
        {
            if (participants.RemoveAll(r => r is null || r.Branch is null) > 0)
            {
                Log.Error($"[OARO] Some participant branches of {ratkinOrder} were null after loading and have been removed.");
            }
            participantsDict = participants.GroupBy(r => r.Branch).ToDictionary(g => g.Key, g => g.First());
        }
    }
    internal void PostOrderGenerated()
    {
        tickToNextStage = (int)(FirstJointPatrolInterval.RandomInRange * 60000f);
    }

    public IThingHolder ParentHolder => null;
    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, innerContainer);
    public ThingOwner GetDirectlyHeldThings() => innerContainer;

    internal void Notify_MyOrderRemoved() => ClearPatrolData(forState: PatrolState.Invalid, forceClear: true);
    internal void Notify_BranchDestroyed(Branch branch)
    {
        if (curState != PatrolState.Invalid)
        {
            RemoveParticipant(branch);
        }
    }

    /// <summary>
    /// 应用交互效果并写入联巡记录
    /// </summary>
    private void ApplyJointInteractionEffect(JointPatrolInteractionDef def, JointBranchRecord record)
    {
        if (record?.Branch is null)
        {
            return;
        }

        StringBuilder explainSB = new();
        if (!def.customDescriptions.NullOrEmpty())
        {
            explainSB.AppendLine(def.customDescriptions.RandomElement().Formatted(record.Branch.Name.Named(KeyLibrary_FormatArgName.BranchName)));
            explainSB.AppendLine();
        }

        if (!def.parts.NullOrEmpty())
        {
            for (int i = 0; i < def.parts.Count; i++)
            {
                def.parts[i].ApplyPart(def, record, explainSB);
            }
        }

        JointInteractionRecord interactionRecord = new JointInteractionRecord()
        {
            Label = def.label,
            RelatedBranch = record.Branch,
            Description = explainSB.ToString(),
            TriggerTick = Find.TickManager.TicksGame
        };

        interactionRecords.Add(interactionRecord);
    }
}