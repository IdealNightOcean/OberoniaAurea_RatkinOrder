using OberoniaAurea_Frame;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AroundKnightGroupsManager : IExposable, IOnBranchDestoryed
{
    private List<AroundKnightGroup> aroundKnightGroups = [];
    public IReadOnlyList<AroundKnightGroup> AroundKnightGroups => aroundKnightGroups;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref aroundKnightGroups, "aroundKnightGroups", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            aroundKnightGroups.RemoveAll(g => !AroundKnightGroup.Validate(g));
        }
    }

    public void TickDay()
    {
        RemoveExpiredKnightGroups();
        CreateNewKnightGroups();
    }

    public void TriggerVisitQuest(AroundKnightGroup knightGroup)
    {
        aroundKnightGroups.Remove(knightGroup);

        Slate slate = new();
        slate.SetBasicOrderSlateVar(knightGroup.Branch);
        slate.Set("knightGroupPawnCount", knightGroup.MemberCount);
        slate.Set("map", QuestGen_Get.GetMap());

        if (!OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, OARO_QuestScriptDefOf.OARO_Quest_AroundKnightGroupVisit, slate, forced: true))
        {
            Dialog_NodeTreeWithRatkinOrderInfo nodeTree = ModUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo("OARO_AroundKnightGroup_ProactiveVisitFailed".Translate(knightGroup.Branch.Name), knightGroup.RatkinOrder);
            Find.WindowStack.Add(nodeTree);
        }
    }

    private void CreateNewKnightGroups()
    {

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
           && !Find.QuestManager.ActiveQuestsListForReading.Any(q => q.root == OARO_QuestScriptDefOf.OARO_Quest_AroundKnightGroupVisit))
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
            label: "OARO_KnightGroupProactiveVisit_QuizLetterLabel".Translate(knightGroup.Branch.Name),
            text: "OARO_AroundKnightGroup_ProactiveVisitQuizLetter".Translate(knightGroup.Branch.Name),
            def: OARO_ModDefOf.OARO_KnightGroupProactiveVisitLetter,
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
