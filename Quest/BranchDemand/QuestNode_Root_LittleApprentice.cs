using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 任务实现：小学徒（内部特化类）
/// </summary>
internal sealed class QuestNode_Root_LittleApprentice : QuestNode_Root_RefugeeBase
{
    private bool normalLeave;
    private string durationEndSignal;
    private string staySignal;
    private string skillSuccessSignal;
    private string skillCheckedSignal;

    protected override PawnKindDef FixedPawnKind => OARO_PawnKindDefOf.OARO_RatkinVillageChild;
    protected override ThoughtDef ThoughtToAdd => OARO_ThoughtDefOf.OARO_Thought_ChildrenCare;

    protected override Faction GetOrGenerateFaction()
    {
        Faction subFaction = QuestGen.slate.Get<Faction>(KeyLibrary_SlateStoreAs.SubFaction);
        QuestPart_MercyQuestWatcher questPart_MercyQuestWatcher = new()
        {
            SubFaction = subFaction,
            ParentFaction = QuestGen.slate.Get<Faction>(KeyLibrary_SlateStoreAs.ParentFaction)
        };
        QuestGen.quest.AddPart(questPart_MercyQuestWatcher);

        QuestGen.slate.Set(IsMainFactionSlate, true);
        return subFaction;
    }

    protected override bool InitQuestParameter()
    {
        questParameter = new QuestParameter()
        {
            allowAssaultColony = false,
            LodgerCount = 1,
            ChildCount = 1,

            goodwillFailure = -20,
            goodwillSuccess = 20,
            rewardValueRange = new FloatRange(1000, 2000),

            questDurationTicks = Rand.RangeInclusive(8 * 60000, 12 * 60000)
        };

        QuestGen.slate.Set("uniqueQuestDesc", true);
        QuestGen.slate.Set("uniqueLeavingLetter", true);

        normalLeave = false;
        durationEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("durationEnd");
        staySignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_Stay");
        skillSuccessSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillSuccess");
        skillCheckedSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillChecked");

        return true;
    }

    protected override void ClearQuestParameter()
    {
        base.ClearQuestParameter();
        normalLeave = false;
        durationEndSignal = null;
        staySignal = null;
        skillSuccessSignal = null;
        skillCheckedSignal = null;
    }

    protected override void SetPawnsLeaveComp(string lodgerArrivalSignal, string inSignalRemovePawn)
    {
        Quest quest = QuestGen.quest;

        string leaveSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_Leave");

        QuestPart_Apprentice_QuizStayIntention questPart_Apprentice_QuizStayIntention = new()
        {
            IsNormalLeave = normalLeave,
            InSignalSkillSuccess = skillSuccessSignal,

            OutSignalLeave = leaveSignal,
            OutSignalStay = staySignal,

            Faction = questParameter.faction,
            Apprentice = questParameter.pawns[0]
        };

        if (normalLeave)
        {
            questPart_Apprentice_QuizStayIntention.InSiganl = skillCheckedSignal;
        }
        else
        {
            OAFrame_TileFinderUtility.TryFindNewAvaliableTile(out PlanetTile tile, questParameter.map.Parent.Tile, 4, 15);
            WorldObject apprenticeHome = WorldObjectMaker.MakeWorldObject(OARO_WorldObjectDefOf.OARO_WO_ApprenticeHome);
            apprenticeHome.Tile = tile;
            apprenticeHome.SetFaction(questParameter.faction);
            (apprenticeHome as IQuestAssociate)?.SetAssociatedQuest(quest);
            QuestGen.slate.Set("apprenticeHome", apprenticeHome);

            quest.SpawnWorldObject(apprenticeHome, inSignal: durationEndSignal);
            quest.WorldObjectTimeout(apprenticeHome, delayTicks: 7 * 60000, inSignalEnable: durationEndSignal, isQuestTimeout: true);

            quest.Letter(letterDef: LetterDefOf.NeutralEvent,
                         inSignal: durationEndSignal,
                         relatedFaction: questParameter.faction,
                         lookTargets: [apprenticeHome],
                         text: "OARO_Apprentice_NoOnePickUp".Translate(),
                         label: "OARO_Apprentice_NoOnePickUpLabel".Translate());

            questPart_Apprentice_QuizStayIntention.InSiganl = QuestGenUtility.HardcodedSignalWithQuestID("apprenticeHome.Destroyed");
            questPart_Apprentice_QuizStayIntention.InSignalResolved = QuestGenUtility.HardcodedSignalWithQuestID("apprenticeHome.Resolved");
        }
        quest.AddPart(questPart_Apprentice_QuizStayIntention);

        string inSignalRemovePawnNew = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_Remove");
        quest.SignalPassAny(action: null, [inSignalRemovePawn, staySignal], outSignal: inSignalRemovePawnNew);
        quest.Leave(questParameter.pawns, inSignal: leaveSignal, sendStandardLetter: true, leaveOnCleanup: false, inSignalRemovePawn: inSignalRemovePawnNew, wakeUp: true);
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string delayFailSignal, string successSignal)
    {
        Quest quest = QuestGen.quest;
        quest.Delay(questParameter.questDurationTicks, inner: null, inSignalEnable: null, inSignalDisable: null, outSignalComplete: durationEndSignal, isQuestTimeout: false, expiryInfoPart: "GuestsDepartsIn".Translate(), expiryInfoPartTip: "GuestsDepartsOn".Translate(), debugLabel: "QuestDelay");

        string skillSuccessEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillSuccessEnd");
        string skillFailEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillFailEnd");
        QuestPart_Apprentice_CheckSkill questPart_Apprentice_CheckSkill = new()
        {
            InSignalCheckSkill = durationEndSignal,
            InSignalSuccessLeave = successSignal,
            InSignalStay = staySignal,

            OutSignalFail = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillFail"),
            OutSignalSuccess = skillSuccessSignal,
            OutSignalChecked = skillCheckedSignal,

            OutSignalSkillSuccessEnd = skillSuccessEndSignal,
            OutSignalSkillFailEnd = skillFailEndSignal,

            Apprentice = questParameter.pawns[0]
        };
        quest.AddPart(questPart_Apprentice_CheckSkill);

        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange_Success = new()
        {
            InSignalTrigger = skillSuccessEndSignal,
            Change = 2,
            Reason = "OARO_LittleApprentice".Translate()
        };

        quest.AddPart(questPart_AllOrdersEsteemChange_Success);

        quest.End(QuestEndOutcome.Success, 0, null, skillSuccessEndSignal, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Fail, 0, null, skillFailEndSignal);


        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange_DelayFail = new()
        {
            InSignalTrigger = delayFailSignal,
            Change = -20,
            Reason = "OARO_HarmingChildren".Translate()
        };
        quest.AddPart(questPart_AllOrdersEsteemChange_DelayFail);

        base.SetQuestEndComp(questPart_Interactions, failSignal, delayFailSignal, successSignal);
    }
}