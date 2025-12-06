using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AroundKnightGroupsManager : IExposable, IOnBranchDestroyed
{
    public static AroundKnightGroupsManager Instance { get; private set; }

    private static readonly SimpleCurve newGroupChaceCurve = new(
    [
        new CurvePoint(0, 0.2f),
        new CurvePoint(1, 0.1f),
        new CurvePoint(5, 0.05f),
        new CurvePoint(20, 0f)
    ]);

    private List<AroundKnightGroup> aroundKnightGroups = [];
    public static IReadOnlyList<AroundKnightGroup> AroundKnightGroups => Instance?.aroundKnightGroups;

    private Season season;

    private int seasonInvitationUsed;
    public int SeasonInvitationUsed
    {
        get { return seasonInvitationUsed; }
        set { seasonInvitationUsed = value > 0 ? value : 0; }
    }

    public AroundKnightGroupsManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(AroundKnightGroupsManager));
        Instance = this;
    }
    public static void ClearStaticCache() => Instance = null;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref aroundKnightGroups, nameof(aroundKnightGroups), LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            aroundKnightGroups.RemoveAll(g => !AroundKnightGroup.Validate(g));
        }
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        if (listing_Rect.ButtonText("创建新的附近小队", widthPct: 0.6f))
        {
            CreateNewKnightGroups();
        }
        if (AroundKnightGroups is null || AroundKnightGroups.Count == 0)
        {
            listing_Rect.Label("None".Translate());
        }
        else
        {
            listing_Rect.Gap(6f);
            foreach (AroundKnightGroup knightGroup in AroundKnightGroups)
            {
                listing_Rect.Label(knightGroup.ToString());
                //按规则邀请
                if (listing_Rect.ButtonText("尝试邀请", widthPct: 0.4f))
                {
                    Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
                    AcceptanceReport acceptanceReport = GlobalInteractionUtility.CanInviteAroundKnightGroup(knightGroup, map, resultOnly: false);
                    if (acceptanceReport)
                    {
                        GlobalInteractionUtility.InviteAroundKnightGroup(knightGroup, map);
                    }
                    else
                    {
                        Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, historical: false);
                    }
                    break;
                }
                //直接触发邀请任务
                if (listing_Rect.ButtonText("强制触发拜访", widthPct: 0.4f))
                {
                    Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
                    if (map is null || !TriggerVisitQuest(knightGroup, map))
                    {
                        RemoveKnightGroup(knightGroup);
                        GlobalInteractionUtility.AroundKnightGroupVisitInvalidDialog(knightGroup, isProactive: false);
                    }
                    break;
                }
            }
        }
    }

    public void TickDay()
    {
        Season curSeason = GenDate.Season(GenTicks.TicksAbs, Vector2.zero);
        if (season != curSeason)
        {
            season = curSeason;
            SeasonInvitationUsed = 0;
        }

        if (Rand.Chance(newGroupChaceCurve.Evaluate(aroundKnightGroups.Count)))
        {
            CreateNewKnightGroups();
        }

        RemoveExpiredKnightGroups();
    }

    public void RemoveKnightGroup(AroundKnightGroup knightGroup)
    {
        if (Instance is null)
        {
            Log.Error($"[OARO] Attempted to use {nameof(AroundKnightGroupsManager)} before initialization.");
            return;
        }
        Instance.aroundKnightGroups.Remove(knightGroup);
    }

    public bool TriggerVisitQuest(AroundKnightGroup knightGroup, Map map)
    {
        Slate slate = new();
        slate.SetBasicBranchSlateVar(knightGroup.Branch, alsoSetOrder: true);
        slate.Set("map", map);
        slate.Set(KeyLibrary_SlateStoreAs.VisitingKnightsCount, knightGroup.MemberCount);
        slate.Set(KeyLibrary_SlateStoreAs.VisitingKnightsDelay, knightGroup.TravelTicks);
        int duration = knightGroup.CurBusyLevel switch
        {
            AroundKnightGroup.BusyLevel.Leisure => 3 * 60000,
            AroundKnightGroup.BusyLevel.Busy => 2 * 60000,
            AroundKnightGroup.BusyLevel.VeryBusy => 1 * 60000,
            _ => 2 * 60000
        };
        slate.Set(KeyLibrary_SlateStoreAs.VisitingKnightsDuration, duration);

        return OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Quest_KnightsVisit, slate, forced: false);
    }

    private void CreateNewKnightGroups()
    {
        HashSet<Branch> curBranch = aroundKnightGroups.Select(r => r.Branch).ToHashSet();
        ConcurrentBag<Branch> potentialBranches = [];
        RatkinOrderManager.Instance.AllRatkinOrders.AsParallel().ForAll((r) =>
        {
            IEnumerable<Branch> affectedBranches = r.BranchManager.AllBranches.Where(b => !curBranch.Contains(b));
            foreach (Branch branch in affectedBranches)
            {
                potentialBranches.Add(branch);
            }
        });
        if (potentialBranches.Count == 0)
        {
            return;
        }
        int takeCount = Rand.RangeInclusive(1, 2) + (aroundKnightGroups.Count == 0 ? 1 : 0);
        takeCount = Mathf.Min(takeCount, potentialBranches.Count);
        foreach (Branch branch in potentialBranches.TakeRandomElements(takeCount))
        {
            aroundKnightGroups.Add(new AroundKnightGroup(branch));
        }
    }

    private void RemoveExpiredKnightGroups()
    {

        int firstIndexToRemove = 0;
        for (int i = 0; i < aroundKnightGroups.Count; i++)
        {
            if (--aroundKnightGroups[i].DaysToExpired <= 0)
            {
                if (i != firstIndexToRemove)
                {
                    aroundKnightGroups[firstIndexToRemove] = aroundKnightGroups[i];
                }
                firstIndexToRemove++;
            }
        }

        if (firstIndexToRemove >= aroundKnightGroups.Count)
        {
            return;
        }

        if (!GlobalInteractionManager.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.KnightGroupProactiveVisit)
            && !Find.QuestManager.ActiveQuestsListForReading.Any(q => q.root == OARO_QuestScriptDefOf.OARO_Quest_KnightsVisit))
        {
            List<AroundKnightGroup> toRemoveGroups = aroundKnightGroups.GetRange(firstIndexToRemove, aroundKnightGroups.Count - firstIndexToRemove);
            AroundKnightGroup knightGroup = toRemoveGroups?.Where(g => g.CurBusyLevel == AroundKnightGroup.BusyLevel.Leisure
                                                                       && g.Branch.IsBranchOfType(Branch.BranchType.Friendly))
                                                           .RandomElementWithFallback(null);

            if (knightGroup is not null)
            {
                GlobalInteractionManager.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.KnightGroupProactiveVisit, cdTicks: 30 * 60000, removeWhenExpired: true);
                QuizAutoVisit(knightGroup);
            }
        }

        aroundKnightGroups.RemoveRange(firstIndexToRemove, aroundKnightGroups.Count - firstIndexToRemove);
    }

    private static void QuizAutoVisit(AroundKnightGroup knightGroup)
    {
        ChoiceLetter_KnightGroupProactiveVisit letter = (ChoiceLetter_KnightGroupProactiveVisit)LetterMaker.MakeLetter(
            label: "OARO_AroundKnightGroup_ProactiveVisitQuizLabel".Translate(),
            text: "OARO_AroundKnightGroup_ProactiveVisitQuizText".Translate(knightGroup.Branch.Name),
            def: OARO_LetterDefOf.OARO_KnightGroupProactiveVisitLetter,
            relatedFaction: knightGroup.RatkinOrder.Faction);

        letter.relatedOrder = knightGroup.RatkinOrder;
        letter.StartTimeout(30000);
        Find.LetterStack.ReceiveLetter(letter);
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        aroundKnightGroups.RemoveAll(g => !AroundKnightGroup.Validate(g) || g.Branch.RatkinOrder == ratkinOrder);
    }
    public void Notify_BranchDestroyed(Branch branch)
    {
        aroundKnightGroups.RemoveAll(g => !AroundKnightGroup.Validate(g) || g.Branch == branch);
    }
}
