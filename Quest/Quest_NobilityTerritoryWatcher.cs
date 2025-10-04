using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Utility;
using static OberoniaAurea.RatkinOrder.WorldObject_NobilityTerritory;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 一个特化的用于叛乱镇压 - 贵族领地管理的特化类
/// 应该在QuestEffectTag后使用，否则强制贵族类型不会生效
/// </summary>
internal sealed class QuestNode_NobilityTerritoryWatcher : QuestNode
{
    public SlateRef<Branch> branch;
    public SlateRef<IEnumerable<WorldObject>> nobilityTerritories;

    [NoTranslate]
    public SlateRef<IEnumerable<string>> inSignalsResolved;
    [NoTranslate]
    public SlateRef<string> outSignalsAllResolved;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_NobilityTerritoryWatcher questPart_NobilityTerritoryWatcher = new()
        {
            Branch = branch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch),
            InSignalsResolved = [],
            OutSignalsAllResolved = QuestGenUtility.HardcodedSignalWithQuestID(outSignalsAllResolved.GetValue(slate))
        };
        IEnumerable<string> inSignalsResolved = this.inSignalsResolved.GetValue(slate);
        if (inSignalsResolved is not null)
        {
            foreach (string inSignal in inSignalsResolved)
            {
                questPart_NobilityTerritoryWatcher.InSignalsResolved.Add(QuestGenUtility.HardcodedSignalWithQuestID(inSignal));
            }
        }
        questPart_NobilityTerritoryWatcher.InitNobilityTerritories(nobilityTerritories.GetValue(slate));


        QuestGen.quest.AddPart(questPart_NobilityTerritoryWatcher);
    }
}

internal sealed class QuestPart_NobilityTerritoryWatcher : QuestPart
{
    public Branch Branch;
    public List<string> InSignalsResolved;
    public string OutSignalsAllResolved;

    private List<WorldObject_NobilityTerritory> nobilityTerritories;

    private int extraRecommendation;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref OutSignalsAllResolved, "OutSignalsAllResolved");
        Scribe_Values.Look(ref extraRecommendation, "extraRecommendation", 0);
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Collections.Look(ref InSignalsResolved, "InSignalsResolved", LookMode.Value);
        Scribe_Collections.Look(ref nobilityTerritories, "nobilityTerritories", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            nobilityTerritories?.RemoveAll(t => t is null);
        }
    }

    public override void Notify_PreCleanup()
    {
        base.Notify_PreCleanup();
        if (quest.State == QuestState.EndedSuccess && extraRecommendation > 0 && Branch is not null)
        {
            Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true);
            if (map is not null)
            {
                IntVec3 spawnCell = DropCellFinder.TradeDropSpot(map);
                RecommendationUtility.GiveRecommendationsToPlayer_Map(Branch.RatkinOrder, extraRecommendation, map, sendStandLetter: false, spawnCell: spawnCell, dropPod: true);
                ChoiceLetter_RatkinOrder letter = (ChoiceLetter_RatkinOrder)LetterMaker.MakeLetter(
                    label: "OARO_NobilityTerritory_ExtraRecommendationLabel".Translate(),
                    text: "OARO_NobilityTerritory_ExtraRecommendationText".Translate(Branch?.RatkinOrder.Name, extraRecommendation),
                    def: OARO_LetterDefOf.OARO_RatkinOrderPositiveLetter,
                    lookTargets: new LookTargets(spawnCell, map),
                    relatedFaction: Branch?.RatkinOrder.Faction,
                    quest: quest);
                letter.relatedOrder = Branch?.RatkinOrder;
                Find.LetterStack.ReceiveLetter(letter);
            }
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        OutSignalsAllResolved = null;
        InSignalsResolved = null;
        Branch = null;
        nobilityTerritories = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (InSignalsResolved?.Contains(signal.tag) ?? false)
        {
            if (nobilityTerritories is not null && signal.args.TryGetArg("SUBJECT", out WorldObject_NobilityTerritory territory))
            {
                if (nobilityTerritories.Remove(territory))
                {
                    if (territory.HasYield && (territory.TypeOfNobility == NobilityType.Justice || territory.TypeOfNobility == NobilityType.Kindness))
                    {
                        extraRecommendation++;
                    }
                    if (nobilityTerritories.Count == 0)
                    {
                        Find.SignalManager.SendSignal(new Signal(OutSignalsAllResolved));
                    }
                }
            }
        }
    }

    public void InitNobilityTerritories(IEnumerable<WorldObject> territories)
    {
        if (territories is null)
        {
            return;
        }
        nobilityTerritories ??= new(4);
        nobilityTerritories.AddRange(territories.OfType<WorldObject_NobilityTerritory>());

        int selCount = 4;
        Stack<(NobilityType, bool)> alternatives = new(4);
        List<NobilityType> allTypes = EnumUtility.GetValues<NobilityType>().Where(nt => nt != NobilityType.None).ToList();
        if (QuestPart_EffectTags.TryGetEffectTagsPart(quest, addPartIfMiss: false, out QuestPart_EffectTags questPart_EffectTags))
        {
            if (questPart_EffectTags.HasTag("AKindnessLord"))
            {
                alternatives.Push((NobilityType.Kindness, true));
                allTypes.Remove(NobilityType.Kindness);
                selCount--;
            }
            if (questPart_EffectTags.HasTag("AKindnessLord"))
            {
                alternatives.Push((NobilityType.Tyrannical, true));
                allTypes.Remove(NobilityType.Tyrannical);
                selCount--;
            }

        }

        foreach (NobilityType type in allTypes.TakeRandomDistinct(selCount))
        {
            alternatives.Push((type, false));
        }

        allTypes = EnumUtility.GetValues<NobilityType>().Where(nt => nt != NobilityType.None).ToList();
        foreach (WorldObject_NobilityTerritory territory in nobilityTerritories)
        {
            (NobilityType type, bool hasExposed) = (NobilityType.None, false);
            if (alternatives.Count > 0)
            {
                (type, hasExposed) = alternatives.Pop();
            }
            else
            {
                type = allTypes.RandomElement();
                hasExposed = false;
            }
            territory.InitNobilityType(type, hasExposed);
        }
    }
}