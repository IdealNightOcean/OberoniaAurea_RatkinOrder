using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.EsteemHandler;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolManager : IExposable, IDrawDevWindow
{
    public enum PatrolType : byte
    {
        Popedom,
        Kingdom,
        Border
    }
    public enum PatrolEndType : byte
    {
        Nothing,
        Normal,
        Friendly,
        Accident,
        Disaster
    }

    public class JointBranchRecord : IExposable
    {
        public Branch Branch;
        public bool HadPassby;
        private float reconnaissance;
        public float Reconnbissance => reconnaissance;

        public bool IsExploration;
        public ThingDef TargetOre;
        private int expectedOreCount;
        private bool reachMax;
        public (int, bool) ExpectedResult => (expectedOreCount, reachMax);

        public void ExposeData()
        {
            Scribe_References.Look(ref Branch, "Branch");
            Scribe_Values.Look(ref HadPassby, "HadPassby", defaultValue: false);
            Scribe_Values.Look(ref reconnaissance, "reconnaissance", 0f);

            Scribe_Values.Look(ref IsExploration, "IsExploration", defaultValue: false);
            Scribe_Defs.Look(ref TargetOre, "TargetOre");
            Scribe_Values.Look(ref expectedOreCount, "expectedOreCount", 0);
            Scribe_Values.Look(ref reachMax, "reachMax", defaultValue: false);
        }

        /// <summary>
        /// 4小时更新一次 (10000 tick)
        /// </summary>
        public void RecordUpdate()
        {

            if (IsExploration)
            {
                GetExpectedOreCount();
            }
        }

        private void GetReconnaissance()
        {
            reconnaissance = (Branch.SquadStat.MemberCount * 10f)
                  * (1f + Branch.MedalHandler.MedalTypeCount * 0.1f)
                  * (1f + Branch.FacilityHandler.TotalFacilityLevel * 0.02f)
                  * (Branch.IsBranchOfType(Branch.BranchType.Honor) ? 1.2f : 1f);
        }

        private void GetExpectedOreCount()
        {
            float rewardValue = Branch.SquadStat.MemberCount * 50f * Rand.Range(0.5f, 1.75f)
                                * (Branch.RatkinOrder.ReformationManager.HasReformation(null) ? 1.5f : 1f)
                                * (Branch.IsBranchOfType(Branch.BranchType.Friendly) ? 1.2f : 1f);

            Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: false);
            if (map is null)
            {
                rewardValue *= 0.75f;
            }
            else
            {
                float distance = Branch.DistanceTo(map.Tile);
                if (distance > 30f)
                {
                    rewardValue -= Mathf.Min((distance - 30f) * 0.01f, 0.25f);
                }
            }

            rewardValue = Mathf.Clamp(rewardValue, 0f, 2000f);
            bool hasReachMax = rewardValue >= 2000f;
            int rewardCount = (int)Mathf.Clamp(rewardValue / StatExtension.GetStatValueAbstract(TargetOre, StatDefOf.MarketValue), 0, 500);
            hasReachMax = hasReachMax || rewardCount >= 500;

            (expectedOreCount, reachMax) = (rewardCount, hasReachMax);
        }
    }

    private const int JointPatrolDurationPrepDays = 3;
    private const int JointPatrolDurationDays = 7;

    public static readonly PatrolEndType[] PatrolEndTypeArr = (PatrolEndType[])Enum.GetValues(typeof(PatrolEndType));

    [Unsaved] private readonly List<(PatrolEndType, float)> patrolEndChances;
    [Unsaved] private int nextEndChancesGetTick = -1;
    public List<(PatrolEndType, float)> PatrolEndChances
    {
        get
        {
            if (Find.TickManager.TicksGame > nextEndChancesGetTick)
            {
                RecachePatrolEndChances();
            }
            return patrolEndChances;
        }
    }

    private readonly RatkinOrder ratkinOrder;
    private BranchManager BranchManager => ratkinOrder.BranchManager;

    private bool isPatrolStarted = false;
    [Unsaved] private bool isPatrolEnded = false;

    private int tickToNextStage = 180000;
    private int tickToNextCheck = 60000;

    public bool IsPatrolStarted => isPatrolStarted;

    private int adjustCeiling;
    private int adjustCount;
    private int burdenSquadCount;

    public int AdjustCeiling => adjustCeiling;
    public int AdjustCount => adjustCount;
    public int BurdenSquadCount => burdenSquadCount;

    private PatrolType patrolType;
    public PatrolType PatrolTypeValue => patrolType;

    private List<JointBranchRecord> participants = [];
    [Unsaved] private HashSet<Branch> participantsHash = [];
    public IReadOnlyList<JointBranchRecord> Participants => participants;

    private float curReconnaissance;
    public float NeedReconnaissanceValue
    {
        get
        {
            return patrolType switch
            {
                PatrolType.Popedom => BranchManager.TotalKnights * 0.16f * 10f,
                PatrolType.Kingdom => BranchManager.TotalKnights * 0.3f * 10f,
                PatrolType.Border => BranchManager.TotalKnights * 0.44f * 10f,
                _ => 0f,
            };
        }
    }

    public StringBuilder endResultText = new();

    public JointPatrolManager(RatkinOrder ratkinOrder)
    {
        this.ratkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        patrolEndChances = new List<(PatrolEndType, float)>(PatrolEndTypeArr.Length)
        {
            (PatrolEndType.Nothing, 1f)
        };

        for (int i = 1; i < PatrolEndTypeArr.Length; i++)
        {
            patrolEndChances.Add((PatrolEndTypeArr[i], 0f));
        }
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_JointPatrolManager(ratkinOrder));
    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"IsPatrolStarted: {isPatrolStarted}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"TickToNextStage: {tickToNextStage}");
        listing_Rect.Label($"TickToNextCheck: {tickToNextCheck}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"AdjustCeiling: {adjustCeiling}");
        listing_Rect.Label($"AdjustCount: {adjustCount}");
        listing_Rect.Label($"BurdenSquadCount: {burdenSquadCount}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"CurPatrolType: {patrolType}");
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
            for (int i = 0; i < participants.Count; i++)
            {
                participants[i].RecordUpdate();
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

        if (isPatrolStarted && tickToNextStage > 60000 && ratkinOrder.Relationship >= RelationshipKind.Acquaintance)
        {
            BranchSquadPassBy();
        }
    }

    public bool IsParticipant(Branch branch) => participantsHash.Contains(branch);

    private void AddParticipant(Branch branch)
    {
        if (participantsHash.Add(branch))
        {
            participants.Add(new JointBranchRecord() { Branch = branch });
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
                    break;
                }
            }
        }
    }

    public bool TryStartPatrolPrep()
    {
        /*
        * 选择联巡类型
        */
        try
        {
            (PatrolType, int)[] typeChance =
            [
                (PatrolType.Popedom, 300),
                (PatrolType.Kingdom, 200 + (int)(ratkinOrder.Funds / 0.01f * 5f)),
                (PatrolType.Border, 10 + (int)(ratkinOrder.Funds / 0.01f * 6f + ratkinOrder.ReformationManager.ReformationsCount * 10f)),
            ];
            patrolType = typeChance.RandomElementByWeight(r => r.Item2).Item1;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to set group patrol type: " + ex);
            patrolType = PatrolType.Popedom;
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
        adjustCount = 0;
        adjustCeiling = 0;
        if (ratkinOrder.Relationship == RelationshipKind.Trustworthy)
        {
            adjustCeiling = ratkinOrder.ReformationManager.HasReformation(null) ? 3 : 2;
        }
        else if (ratkinOrder.Relationship == RelationshipKind.Soulmate)
        {
            adjustCeiling = ratkinOrder.ReformationManager.HasReformation(null) ? 6 : 4;
        }
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
            ratkinOrder.FundHandler.AdjustFundsImmediately(overcap * 0.05f);
        }

        foreach (JointBranchRecord record in participants)
        {
            record.Branch.SquadStat.Supply -= 0.5f;
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

        RecachePatrolEndChances();

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
        if (record.IsExploration)
        {
            OrderLetter letter = OrderLetterUtility.MakeOrderLetter("OARO_LetterLabel_SquadExplorationResult".Translate(), "OARO_Letter_SquadExplorationResult".Translate(), OrderLetter.LetterType.Official, record.Branch.RatkinOrder, record.Branch.Name);
            letter.RelatedThings = [new ThingDefCount(record.TargetOre, record.ExpectedResult.Item1)];
            OrderLetterBox.Instance.ReceiveLetter(letter);
        }

        PatrolEndType endType = patrolEndChances.RandomElementByWeight(t => t.Item2).Item1;
        switch (endType)
        {
            case PatrolEndType.Nothing: break;
            case PatrolEndType.Normal: break;
            case PatrolEndType.Friendly: break;
            case PatrolEndType.Accident: break;
            case PatrolEndType.Disaster: break;
        }
    }

    private (float fundGain, float reformProgressGain) GetJointPatrolEndResult()
    {
        float reconnaissanceRate = Mathf.Clamp(curReconnaissance / NeedReconnaissanceValue, 0f, 2f);
        float rewardMulti = patrolType switch
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

    private void RecachePatrolEndChances()
    {
        nextEndChancesGetTick = Find.TickManager.TicksGame + 30000;
        float preparedMulti = 0.5f;

        patrolEndChances.Clear();
        switch (patrolType)
        {
            case PatrolType.Popedom:
                patrolEndChances.Add((PatrolEndType.Nothing, 0.69f));
                patrolEndChances.Add((PatrolEndType.Normal, 0.15f));
                patrolEndChances.Add((PatrolEndType.Friendly, 0.08f));
                patrolEndChances.Add((PatrolEndType.Accident, 0.07f * preparedMulti));
                patrolEndChances.Add((PatrolEndType.Disaster, 0.01f * preparedMulti));
                return;
            case PatrolType.Kingdom:
                patrolEndChances.Add((PatrolEndType.Nothing, 0.60f));
                patrolEndChances.Add((PatrolEndType.Normal, 0.15f));
                patrolEndChances.Add((PatrolEndType.Friendly, 0.1f));
                patrolEndChances.Add((PatrolEndType.Accident, 0.12f * preparedMulti));
                patrolEndChances.Add((PatrolEndType.Disaster, 0.03f * preparedMulti));
                return;
            case PatrolType.Border:
                patrolEndChances.Add((PatrolEndType.Nothing, 0.52f));
                patrolEndChances.Add((PatrolEndType.Normal, 0.16f));
                patrolEndChances.Add((PatrolEndType.Friendly, 0.12f));
                patrolEndChances.Add((PatrolEndType.Accident, 0.14f * preparedMulti));
                patrolEndChances.Add((PatrolEndType.Disaster, 0.06f * preparedMulti));
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

    private void BranchSquadPassBy()
    {
        List<(JointBranchRecord, int)> potentialPass = [];
        foreach (JointBranchRecord record in participants)
        {
            if (!record.HadPassby)
            {
                potentialPass.Add((record, record.Branch.IsBranchOfType(Branch.BranchType.Friendly) ? 3 : 1));
            }
        }

        if (potentialPass.Count == 0)
        {
            return;
        }

        JointBranchRecord targetRecord = potentialPass.RandomElementByWeight(s => s.Item2).Item1;
        potentialPass = null;
        targetRecord.HadPassby = true;

        Branch targetBranch = targetRecord.Branch;
        bool targetFriendly = targetBranch.IsBranchOfType(Branch.BranchType.Friendly);
        int relationShipDiff = ratkinOrder.Relationship - RelationshipKind.Acquaintance;

        List<(int, float)> passByTypeList =
        [
            (0, Mathf.Max(0f, 75f - (relationShipDiff > 0 ? relationShipDiff * 5f : 0f) - (targetFriendly ? 50f : 0f))),
            (1, Mathf.Max(0f, 20f + (relationShipDiff > 0 ? relationShipDiff * 5f : 0f) + (targetFriendly ? 30f : 0f))),
            (2, Mathf.Max(5f, 20f + (ratkinOrder.Relationship >= RelationshipKind.Soulmate ?  5f : 0f) + (targetFriendly ? 20f : 0f))),
        ];

        int passByType = passByTypeList.RandomElementByWeight(t => t.Item2).Item1;
        passByTypeList = null;

        switch (passByType)
        {
            case 1:
                Slate slate = new();
                slate.SetBasicBranchSlateVar(targetBranch, alsoSetOrder: true);
                if (OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Quest_TemporaryEncampment, slate, forced: false))
                {
                    return;
                }
                else
                {
                    goto default;
                }
            case 2:
                if (false)
                {
                    return;
                }
                else
                {
                    goto default;
                }
            default:
                {
                    Messages.Message("OARO_Message_GroupPatrolPassBy".Translate(targetBranch.Name), MessageTypeDefOf.NeutralEvent, historical: true);
                    return;
                }
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref adjustCeiling, "adjustCeiling", 0);
        Scribe_Values.Look(ref adjustCount, "adjustCount", 0);

        Scribe_Values.Look(ref isPatrolStarted, "isPatrolStarted", defaultValue: false);
        Scribe_Values.Look(ref tickToNextStage, "tickToNextStage", 1800000);
        Scribe_Values.Look(ref tickToNextCheck, "tickToStart", 60000);

        Scribe_Values.Look(ref patrolType, "CurPatrolType", PatrolType.Popedom);
        Scribe_Values.Look(ref burdenSquadCount, "burdenSquadCount", 0);

        Scribe_Values.Look(ref curReconnaissance, "curReconnaissance", 0f);

        Scribe_Collections.Look(ref participants, "participants", LookMode.Deep);
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
