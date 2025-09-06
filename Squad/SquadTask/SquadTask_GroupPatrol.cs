using RimWorld;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadTaskStartChecker_GroupPatrol : SquadTaskStartChecker
{
    public override AcceptanceReport CanStartNow(Squad squad, bool resultOnly = false)
    {
        return squad.SquadManager.GroupPatrolManager.IsPatrolStarted ? true : (resultOnly ? false : "OARO_GroupPatrolNotStarted".Translate());
    }
}

public class SquadTask_GroupPatrol : SquadTask
{
    public float reconnaissanceValue;
    public bool hadPassedBy;

    public bool isExploration;
    public ThingDef targetOre;

    private int expectedExplorationCount;
    private bool reachMax;
    public (int, bool) ExpectedResult => (expectedExplorationCount, reachMax);

    private static SquadGroupPatrolManager GroupPatrolManager(Squad squad) => squad.SquadManager.GroupPatrolManager;

    public override void TickHour(Squad squad)
    {
        RecacheReconnaissance(squad);
        if (isExploration)
        {
            (expectedExplorationCount, reachMax) = GetExpectedExplorationCount(squad);
        }
    }

    public override void TaskStart(Squad squad)
    {
        squad.SquadStat.Supply -= 0.5f;
        RecacheReconnaissance(squad);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RecacheReconnaissance(Squad squad)
    {
        reconnaissanceValue = (squad.SquadStat.MemberCount * 10f)
            * (1f + squad.SquadStat.MedalRecords.Count * 0.1f)
            * (1f + squad.Branch.FacilityHandler.TotalFacilityLevel * 0.02f)
            * (squad.IsSquadOfType(BranchType.Honor) ? 1.2f : 1f);
    }

    public override void TaskEnd(Squad squad, bool interrupt)
    {
        StringBuilder resultBuilder = new();
        TaskEndOutcome(squad, resultBuilder);
        if (isExploration)
        {
            int explorationCount = GetExpectedExplorationCount(squad).Item1;
            OrderLetter letter = OrderLetterUtility.MakeOrderLetter("OARO_LetterLabel_SquadExplorationResult".Translate(), "OARO_Letter_SquadExplorationResult".Translate(), OrderLetterType.Official, squad.RatkinOrder, squad.Name);
            letter.RelatedThings = [new ThingDefCount(targetOre, explorationCount)];
            OrderLetterBox.Instance.ReceiveLetter(letter);
        }
        GroupPatrolManager(squad).Notify_SquadPatrolEnd(squad, reconnaissanceValue, resultBuilder);
    }

    private static void TaskEndOutcome(Squad squad, StringBuilder resultBuilder)
    {
        SquadGroupPatrolManager.PatrolEndType endType = GroupPatrolManager(squad).PatrolEndChances.RandomElementByWeightWithFallback(t => t.Item2, fallback: (SquadGroupPatrolManager.PatrolEndType.Normal, 0f)).Item1;

        switch (endType)
        {
            case SquadGroupPatrolManager.PatrolEndType.Nothing: break;
            case SquadGroupPatrolManager.PatrolEndType.Normal: break;
            case SquadGroupPatrolManager.PatrolEndType.Friendly: break;
            case SquadGroupPatrolManager.PatrolEndType.Accident: break;
            case SquadGroupPatrolManager.PatrolEndType.Disaster: break;
        }
    }

    private (int, bool) GetExpectedExplorationCount(Squad squad)
    {
        float rewardValue = squad.SquadStat.MemberCount * 50f * Rand.Range(0.5f, 1.75f)
                            * (squad.RatkinOrder.ReformationManager.EffectTags.HasActiveTag("") ? 1.5f : 1f)
                            * (squad.IsSquadOfType(BranchType.Friendly) ? 1.2f : 1f);

        Map map = Find.AnyPlayerHomeMap;
        if (map is null)
        {
            reconnaissanceValue *= 0.75f;
        }
        else
        {
            float distance = squad.Branch.DistanceTo(map.Tile);
            if (distance > 30f)
            {
                rewardValue -= Mathf.Min((distance - 30f) * 0.01f, 0.25f);
            }
        }

        rewardValue = Mathf.Clamp(rewardValue, 0f, 2000f);
        bool reachMax = rewardValue >= 2000f;
        int rewardCount = (int)Mathf.Clamp(rewardValue / StatExtension.GetStatValueAbstract(targetOre, StatDefOf.MarketValue), 0, 500);
        reachMax = reachMax || rewardCount >= 500;
        return (rewardCount, reachMax);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref reconnaissanceValue, "reconnaissanceValue", 0f);

        Scribe_Values.Look(ref isExploration, "isExploration", defaultValue: false);
        Scribe_Defs.Look(ref targetOre, "targetOre");
        Scribe_Values.Look(ref expectedExplorationCount, "expectedExplorationCount", 0);
        Scribe_Values.Look(ref reachMax, "reachMax", defaultValue: false);
    }
}