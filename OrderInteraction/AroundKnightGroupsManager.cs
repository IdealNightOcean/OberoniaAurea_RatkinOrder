using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AroundKnightGroupsManager : IExposable, IOnBranchDestoryed
{
    private static readonly SimpleCurve newGroupChaceCurve = new(
    [
        new CurvePoint(0, 0.2f),
        new CurvePoint(1, 0.1f),
        new CurvePoint(5, 0.05f),
        new CurvePoint(20, 0f)
    ]);

    private List<AroundKnightGroup> aroundKnightGroups = [];
    public IReadOnlyList<AroundKnightGroup> AroundKnightGroups => aroundKnightGroups;

    private Season season;

    private int seasonInvitationUsed;
    public int SeasonInvitationUsed
    {
        get { return seasonInvitationUsed; }
        set { seasonInvitationUsed = value > 0 ? value : 0; }
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref aroundKnightGroups, "aroundKnightGroups", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            aroundKnightGroups.RemoveAll(g => !AroundKnightGroup.Validate(g));
        }
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        if (listing_Rect.ButtonText("Create NewKnight Groups", widthPct: 0.6f))
        {
            CreateNewKnightGroups(force: true);
        }
        if (aroundKnightGroups.NullOrEmpty())
        {
            listing_Rect.Label("None");
        }
        else
        {
            listing_Rect.Gap(6f);
            foreach (AroundKnightGroup knightGroup in aroundKnightGroups)
            {
                listing_Rect.Label(knightGroup.ToString());
                if (listing_Rect.ButtonText("Trigger", widthPct: 0.4f))
                {
                    Map map = MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
                    if (map is null || !TriggerVisitQuest(knightGroup, map))
                    {
                        OrderInteractionHandler.AroundKnightGroupsManager.RemoveKnightGroup(knightGroup);
                        OrderInteractionUtility.AroundKnightGroupVisitInvalid(knightGroup.Branch, isProactive: false);
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

        CreateNewKnightGroups();
        RemoveExpiredKnightGroups();
    }

    public void RemoveKnightGroup(AroundKnightGroup knightGroup) => aroundKnightGroups.Remove(knightGroup);

    public bool TriggerVisitQuest(AroundKnightGroup knightGroup, Map map)
    {
        aroundKnightGroups.Remove(knightGroup);

        Slate slate = new();
        slate.SetBasicOrderSlateVar(knightGroup.Branch);
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

    private void CreateNewKnightGroups(bool force = false)
    {
        float chance = force ? 1f : newGroupChaceCurve.Evaluate(aroundKnightGroups.Count);
        if (Rand.Chance(chance))
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
            List<Branch> targetBranch = potentialBranches.TakeRandomDistinct(takeCount);
            foreach (Branch branch in targetBranch)
            {
                aroundKnightGroups.Add(new AroundKnightGroup(branch));
            }
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

        if (!OrderInteractionHandler.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.KnightGroupProactiveVisit)
           && !Find.QuestManager.ActiveQuestsListForReading.Any(q => q.root == OARO_QuestScriptDefOf.OARO_Quest_KnightsVisit))
        {
            AroundKnightGroup knightGroup = aroundKnightGroups.GetRange(
                firstIndexToRemove,
                aroundKnightGroups.Count - firstIndexToRemove)?.Where(g => g.CurBusyLevel == AroundKnightGroup.BusyLevel.Leisure
                                                                           && g.Branch.IsBranchOfType(BranchType.Friendly))
                                                              .RandomElementWithFallback(null);

            if (knightGroup is not null)
            {
                OrderInteractionHandler.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.KnightGroupProactiveVisit, cdTicks: 30 * 60000, shouldRemoveWhenExpired: true);
                QuizAutoVisit(knightGroup);
            }
        }

        aroundKnightGroups.RemoveRange(firstIndexToRemove, aroundKnightGroups.Count - firstIndexToRemove);
    }

    private void QuizAutoVisit(AroundKnightGroup knightGroup)
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
    public void Notify_BranchDestoryed(Branch branch)
    {
        aroundKnightGroups.RemoveAll(g => !AroundKnightGroup.Validate(g) || g.Branch == branch);
    }
}
