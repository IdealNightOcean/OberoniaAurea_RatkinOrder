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
    private bool NormalLeave { get; set; }
    private string DurationEndSignal { get; set; }
    private string StaySignal { get; set; }
    private string SkillSuccessSignal { get; set; }
    private string SkillCheckedSignal { get; set; }

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

        NormalLeave = false;
        DurationEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("durationEnd");
        StaySignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_Stay");
        SkillSuccessSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillSuccess");
        SkillCheckedSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillChecked");

        return true;
    }

    protected override void ClearQuestParameter()
    {
        base.ClearQuestParameter();
        NormalLeave = false;
        DurationEndSignal = null;
        StaySignal = null;
        SkillSuccessSignal = null;
        SkillCheckedSignal = null;
    }

    protected override void SetPawnsLeaveComp(string lodgerArrivalSignal, string inSignalRemovePawn)
    {
        Quest quest = QuestGen.quest;

        string leaveSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_Leave");

        QuestPart_Apprentice_QuizStayIntention questPart_Apprentice_QuizStayIntention = new()
        {
            IsNormalLeave = NormalLeave,
            InSignalSkillSuccess = SkillSuccessSignal,

            OutSignalLeave = leaveSignal,
            OutSignalStay = StaySignal,

            Faction = questParameter.faction,
            Apprentice = questParameter.pawns[0]
        };

        if (NormalLeave)
        {
            questPart_Apprentice_QuizStayIntention.InSiganl = SkillCheckedSignal;
        }
        else
        {
            OAFrame_TileFinderUtility.TryFindNewAvaliableTile(out PlanetTile tile, questParameter.map.Parent.Tile, 4, 15);
            WorldObject_ApprenticeHome apprenticeHome = (WorldObject_ApprenticeHome)WorldObjectMaker.MakeWorldObject(OARO_WorldObjectDefOf.OARO_WO_ApprenticeHome);
            apprenticeHome.Tile = tile;
            apprenticeHome.Apprentice = questParameter.pawns[0];
            apprenticeHome.SetAssociatedQuest(quest);
            apprenticeHome.SetFaction(questParameter.faction);
            apprenticeHome.Name = "OARO_ApprenticeHomeName".Translate(questParameter.faction.Name.Named(KeyLibrary_FormatArgName.FACTION), questParameter.pawns[0].Named(KeyLibrary_FormatArgName.PAWN));
            QuestGen.slate.Set("apprenticeHome", apprenticeHome);

            quest.SpawnWorldObject(apprenticeHome, inSignal: DurationEndSignal);
            quest.WorldObjectTimeout(apprenticeHome, delayTicks: 20 * 60000, inSignalEnable: DurationEndSignal, isQuestTimeout: true);

            quest.Letter(letterDef: LetterDefOf.NeutralEvent,
                         inSignal: DurationEndSignal,
                         relatedFaction: questParameter.faction,
                         lookTargets: [apprenticeHome],
                         text: "[apprenticeNoOnePickUpText]",
                         label: "[apprenticeNoOnePickUpLabel]");

            questPart_Apprentice_QuizStayIntention.InSiganl = QuestGenUtility.HardcodedSignalWithQuestID("apprenticeHome.Destroyed");
            questPart_Apprentice_QuizStayIntention.InSignalResolved = QuestGenUtility.HardcodedSignalWithQuestID("apprenticeHome.Resolved");
        }
        quest.AddPart(questPart_Apprentice_QuizStayIntention);

        string inSignalRemovePawnNew = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_Remove");
        quest.SignalPassAny(action: null, [inSignalRemovePawn, StaySignal], outSignal: inSignalRemovePawnNew);
        quest.Leave(questParameter.pawns, inSignal: leaveSignal, sendStandardLetter: true, leaveOnCleanup: false, inSignalRemovePawn: inSignalRemovePawnNew, wakeUp: true);
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string delayFailSignal, string successSignal)
    {
        Quest quest = QuestGen.quest;
        quest.Delay(questParameter.questDurationTicks, inner: null, inSignalEnable: null, inSignalDisable: null, outSignalComplete: DurationEndSignal, isQuestTimeout: false, expiryInfoPart: "GuestsDepartsIn".Translate(), expiryInfoPartTip: "GuestsDepartsOn".Translate(), debugLabel: "QuestDelay");

        string skillSuccessEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillSuccessEnd");
        string skillFailEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillFailEnd");
        QuestPart_Apprentice_CheckSkill questPart_Apprentice_CheckSkill = new()
        {
            InSignalCheckSkill = DurationEndSignal,
            InSignalSuccessLeave = successSignal,
            InSignalStay = StaySignal,

            OutSignalFail = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillFail"),
            OutSignalSuccess = SkillSuccessSignal,
            OutSignalChecked = SkillCheckedSignal,

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

        quest.End(outcome: QuestEndOutcome.Success, inSignal: skillSuccessEndSignal, sendStandardLetter: true);
        quest.End(outcome: QuestEndOutcome.Fail, inSignal: skillFailEndSignal);

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