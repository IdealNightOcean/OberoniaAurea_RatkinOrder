using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal sealed class QuestNode_TownConstructionWatcher : QuestNode
{
    public SlateRef<WorldObject_TownConstruction> town;

    [NoTranslate]
    public SlateRef<string> inSignalSettled;
    [NoTranslate]
    public SlateRef<string> outSignalFailed;
    [NoTranslate]
    public SlateRef<string> outSignalSucceed;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_TownConstructionWatcher questPart_TownConstructionWatcher = new()
        {
            Town = town.GetValue(slate),

            InSignalSettled = QuestGenUtility.HardcodedSignalWithQuestID(inSignalSettled.GetValue(slate)),
            OutSignalFailed = QuestGenUtility.HardcodedSignalWithQuestID(outSignalFailed.GetValue(slate)),
            OutSignalSucceed = QuestGenUtility.HardcodedSignalWithQuestID(outSignalSucceed.GetValue(slate))
        };
        QuestGen.quest.AddPart(questPart_TownConstructionWatcher);
    }
}


internal sealed class QuestPart_TownConstructionWatcher : QuestPart
{
    public WorldObject_TownConstruction Town;
    public string InSignalSettled;
    public string OutSignalFailed;
    public string OutSignalSucceed;

    private float populationMulti = 1f;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Town, "Town");
        Scribe_Values.Look(ref InSignalSettled, "InSignalSettled");
        Scribe_Values.Look(ref OutSignalFailed, "OutSignalFailed");
        Scribe_Values.Look(ref OutSignalSucceed, "OutSignalSucceed");

        Scribe_Values.Look(ref populationMulti, "populationMulti", 1f);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        Town = null;
        InSignalSettled = null;
        OutSignalFailed = null;
        OutSignalSucceed = null;

        populationMulti = 1f;
    }

    public override void Notify_PreCleanup()
    {
        base.Notify_PreCleanup();
        if (quest.State != QuestState.EndedSuccess)
        {
            return;
        }
        Faction faction = Town.Faction;
        float rewardSilverCount = Town.Population * 0.5f;
        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true);
        if (map is not null)
        {
            int rewardSilverCountInt = Mathf.RoundToInt(rewardSilverCount);
            IntVec3 dropCell = OberoniaAurea_Frame.Utility.OAFrame_DropPodUtility.DefaultDropThingOfDef(ThingDefOf.Silver, rewardSilverCountInt, map, faction, sendLetter: false);
            Find.LetterStack.ReceiveLetter(
                label: "OARO_TownUnderConstruction_ExtraRewardSilverLabel".Translate(),
                text: "OARO_TownUnderConstruction_ExtraRewardSilverText".Translate(rewardSilverCountInt),
                textLetterDef: LetterDefOf.PositiveEvent,
                lookTargets: new LookTargets(dropCell, map),
                relatedFaction: faction,
                quest: quest);
        }

        if (Town.ConstructionScale >= 2)
        {
            Settlement settlement = SettleUtility.AddNewHome(Town.Tile, faction);
            if (Town.ConstructionScale >= 3)
            {
                Branch branch = Branch.GenerateBranchFor(Town.Branch.RatkinOrder, settlement, addToManager: true);
                if (branch.IsValid())
                {
                    branch.SetFriendly(active: true, showMessage: false);

                    if (Town.ConstructionScale >= 4)
                    {
                        BranchFacilityHandler facilityHandler = branch.FacilityHandler;
                        foreach (BranchFacilityDef facilityDef in DefDatabase<BranchFacilityDef>.AllDefs)
                        {
                            facilityHandler.TryAdjustFacilityStage(facilityDef, BranchFacilityLevel.Normal, addIfMiss: true);
                        }
                    }
                }
            }

            Find.LetterStack.ReceiveLetter(
                label: "OARO_TownUnderConstruction_SettleLabel".Translate(),
                text: $"OARO_TownUnderConstruction_SettleText_{Town.ConstructionScale}".Translate(),
                textLetterDef: LetterDefOf.PositiveEvent,
                lookTargets: settlement,
                relatedFaction: faction,
                quest: quest);
        }
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == QuestPart_CliquesManager.SignalCliqueActived(quest))
        {
            if (signal.args.TryGetArg(KeyLibrary_FormatArgName.SUBJECT, out QuestClique clique))
            {
                switch (clique.Key)
                {
                    case "TravelRatkin":
                        {
                            populationMulti = 1.2f;
                            break;
                        }
                    case "Builder":
                        {
                            Town.InitInnerTrader();
                            break;
                        }
                    case "SeniorResident":
                        {
                            Town.Population += (100f * populationMulti);
                            break;
                        }
                    case "VillageResident":
                        {
                            Town.Population += (200f * populationMulti);
                            break;
                        }
                    case "RemoteResident":
                        {
                            Town.Population += (150f * populationMulti);
                            break;
                        }
                    case "FramerResident":
                        {
                            Town.Population += (250f * populationMulti);
                            break;
                        }

                    default: break;
                }
            }
        }

        if (signal.tag == InSignalSettled)
        {
            if (Town.Population < 1600f)
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalFailed));
            }
            else
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalSucceed));
            }
        }
    }
}