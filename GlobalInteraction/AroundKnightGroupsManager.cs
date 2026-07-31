using NightOcean.Collection;
using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
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
        OberoniaAurea_Frame.Utility.OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(AroundKnightGroupsManager));
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
                    if (map is not null)
                    {
                        TryTriggerVisitQuest(knightGroup, map, removeWhenInvalid: true);
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

    public bool RemoveKnightGroup(AroundKnightGroup knightGroup) => aroundKnightGroups.Remove(knightGroup);

    public bool TryTriggerVisitQuest(AroundKnightGroup knightGroup, Map map, bool removeWhenInvalid = true)
    {
        bool result = false;
        if (knightGroup is not null && map is not null)
        {
            Slate slate = new();
            slate.SetBasicBranchSlateVar(knightGroup.Branch, alsoSetOrder: true);
            slate.Set("map", map);
            slate.Set(OARO_KeyLibrary_SlateStoreAs.visitingKnightsCount, knightGroup.MemberCount);
            slate.Set(OARO_KeyLibrary_SlateStoreAs.visitingKnightsDelay, knightGroup.TravelTicks);
            int duration = knightGroup.CurBusyLevel switch
            {
                AroundKnightGroup.BusyLevel.Leisure => 3 * 60000,
                AroundKnightGroup.BusyLevel.Busy => 2 * 60000,
                AroundKnightGroup.BusyLevel.VeryBusy => 1 * 60000,
                _ => 2 * 60000
            };
            slate.Set(OARO_KeyLibrary_SlateStoreAs.visitingKnightsDuration, duration);

            result = OberoniaAurea_Frame.Utility.OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Quest_KnightsVisit, slate, forced: false);
        }

        if (result || removeWhenInvalid)
        {
            RemoveKnightGroup(knightGroup);
        }
        return result;
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
        List<AroundKnightGroup> expiredGroups = aroundKnightGroups.ExtractMatching(g => ((--g.DaysToExpired) <= 0));
        if (expiredGroups.NullOrEmpty())
        {
            return;
        }

        if (!GameComponent_RatkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.KnightGroupProactiveVisit)
            && !Find.QuestManager.ActiveQuestsListForReading.Any(q => q.root == OARO_QuestScriptDefOf.OARO_Quest_KnightsVisit))
        {
            AroundKnightGroup knightGroup = expiredGroups.Where(g => g.CurBusyLevel == AroundKnightGroup.BusyLevel.Leisure
                                                                  && g.Branch.IsBranchOfType(Branch.BranchType.Friendly))
                                                         .RandomElementWithFallback(null);

            if (knightGroup is not null)
            {
                GameComponent_RatkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.KnightGroupProactiveVisit, cdTicks: 30 * 60000, removeWhenExpired: true);
                QuizAutoVisit(knightGroup);
            }
        }
    }

    private static void QuizAutoVisit(AroundKnightGroup knightGroup)
    {
        ChoiceLetter_KnightGroupProactiveVisit letter = (ChoiceLetter_KnightGroupProactiveVisit)LetterMaker.MakeLetter(
            label: "OARO_AroundKnightGroup_ProactiveVisitQuizLabel".Translate(),
            text: "OARO_AroundKnightGroup_ProactiveVisitQuizText".Translate(knightGroup.Branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName)),
            def: OARO_LetterDefOf.OARO_KnightGroupProactiveVisitLetter,
            relatedFaction: knightGroup.RatkinOrder.Faction);
        letter.KnightGroup = knightGroup;
        letter.RelatedOrder = knightGroup.RatkinOrder;
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
