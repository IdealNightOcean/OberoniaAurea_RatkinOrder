using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_LittleApprentice : QuestNode_Root_RefugeeBase
{
    private bool normalLeave;
    private string durationEndSignal;
    private string staySignal;
    private string skillSuccessSignal;
    private string skillCheckedSignal;

    protected override Faction GetOrGenerateFaction()
    {
        return ModUtility.GenerateSubRatkinFaction(OARO_ModDefOf.OARO_Rakinia_Sub, OARO_ModDefOf.Rakinia);
    }

    protected override void InitQuestParameter()
    {
        questParameter = new QuestParameter()
        {
            allowAssaultColony = false,
            LodgerCount = 1,
            ChildCount = 1,

            goodwillFailure = -20,
            goodwillSuccess = 20,
            rewardValueRange = new FloatRange(1000, 2000),

            questDurationTicks = Rand.RangeInclusive(8 * 60000, 12 * 60000),

            fixedPawnKind = PawnKindDefOf.Villager,
            //addMemory = ModDefOf.OARO_Thought_ChildrenCare
        };

        normalLeave = false;
        durationEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("durationEnd");
        staySignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_Stay");
        skillSuccessSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillSuccess");
        skillCheckedSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillChecked");
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

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string bigFailSignal, string successSignal)
    {
        Quest quest = QuestGen.quest;
        quest.Delay(questParameter.questDurationTicks, inner: null, inSignalEnable: null, inSignalDisable: null, outSignalComplete: durationEndSignal, isQuestTimeout: false, expiryInfoPart: "GuestsDepartsIn".Translate(), expiryInfoPartTip: "GuestsDepartsOn".Translate(), debugLabel: "QuestDelay");

        string skillSuccessEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillSuccessEnd");
        string skillFailEndSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillFailEnd");
        QuestPart_Apprentice_CheckSkill questPart_Apprentice_CheckSkill = new()
        {
            inSignalCheckSkill = durationEndSignal,
            inSignalSuccessLeave = successSignal,
            inSignalStay = staySignal,

            outSignalFail = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_SkillFail"),
            outSignalSuccess = skillSuccessSignal,
            outSignalChecked = skillCheckedSignal,

            outSignalSkillSuccessEnd = skillSuccessEndSignal,
            outSignalSkillFailEnd = skillFailEndSignal,

            apprentice = questParameter.pawns[0]
        };
        quest.AddPart(questPart_Apprentice_CheckSkill);

        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange_Success = new()
        {
            inSignalTrigger = skillSuccessEndSignal,
            change = 2,
            reason = "OARO_LittleApprentice".Translate()
        };

        quest.AddPart(questPart_AllOrdersEsteemChange_Success);

        quest.End(QuestEndOutcome.Success, 0, null, skillSuccessEndSignal, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Fail, 0, null, skillFailEndSignal);


        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange_BigFail = new()
        {
            inSignalTrigger = bigFailSignal,
            change = -20,
            reason = "OARO_HarmingChildren".Translate()
        };
        quest.AddPart(questPart_AllOrdersEsteemChange_BigFail);

        base.SetQuestEndComp(questPart_Interactions, failSignal, bigFailSignal, successSignal);
    }

    protected override void SetPawnsLeaveComp(string lodgerArrivalSignal, string inSignalRemovePawn)
    {
        Quest quest = QuestGen.quest;

        string leaveSignal = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_Leave");

        QuestPart_Apprentice_QuizStayIntention questPart_Apprentice_QuizStayIntention = new()
        {
            isNormalLeave = normalLeave,
            inSignalSkillSuccess = skillSuccessSignal,

            outSignalLeave = leaveSignal,
            outSignalStay = staySignal,

            faction = questParameter.faction,
            apprentice = questParameter.pawns[0]
        };

        if (normalLeave)
        {
            questPart_Apprentice_QuizStayIntention.inSiganl = skillCheckedSignal;
        }
        else
        {
            OAFrame_TileFinderUtility.TryFindNewAvaliableTile(out PlanetTile tile, questParameter.map.Parent.Tile, 4, 15);
            WorldObject apprenticeHome = WorldObjectMaker.MakeWorldObject(OARO_ModDefOf.OARO_WO_ApprenticeHome);
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

            questPart_Apprentice_QuizStayIntention.inSiganl = QuestGenUtility.HardcodedSignalWithQuestID("apprenticeHome.Destroyed");
            questPart_Apprentice_QuizStayIntention.inSignalResolved = QuestGenUtility.HardcodedSignalWithQuestID("apprenticeHome.Resolved");
        }
        quest.AddPart(questPart_Apprentice_QuizStayIntention);

        string inSignalRemovePawnNew = QuestGenUtility.HardcodedSignalWithQuestID("Apprentice_Remove");
        quest.SignalPassAny(action: null, [inSignalRemovePawn, staySignal], outSignal: inSignalRemovePawnNew);
        quest.Leave(questParameter.pawns, inSignal: leaveSignal, sendStandardLetter: true, leaveOnCleanup: false, inSignalRemovePawn: inSignalRemovePawnNew, wakeUp: true);
    }
}