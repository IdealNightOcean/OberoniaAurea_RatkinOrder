using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

internal class QuestPart_LordJob_HelpSeeker : QuestPart_LordJob_CommomTalk
{
    public string inSignalAccept;
    public string ininSignalReject;

    private bool canTriggered = true;

    public QuestScriptDef mercyQuestDef;

    public Faction subFaction;
    public Faction parentFaction;
    public FactionDef parentFactionDef;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignalAccept, "inSignalAccept");
        Scribe_Values.Look(ref ininSignalReject, "ininSignalReject");

        Scribe_Values.Look(ref canTriggered, "canTriggered", defaultValue: true);

        Scribe_Defs.Look(ref mercyQuestDef, "mercyQuestDef");

        Scribe_References.Look(ref subFaction, "subFaction");
        Scribe_References.Look(ref parentFaction, "parentFaction");
        Scribe_Defs.Look(ref parentFactionDef, "parentFactionDef");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        inSignalAccept = null;
        ininSignalReject = null;

        mercyQuestDef = null;

        subFaction = null;
        parentFaction = null;
        parentFactionDef = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (canTriggered && signal.tag == inSignalAccept)
        {
            canTriggered = false;
            TryTriggerQuest();
        }
        else if (signal.tag == ininSignalReject)
        {
            canTriggered = false;
            this.DeregisterTalkAction();
            talkWith = null;
        }
    }

    private bool TryTriggerQuest()
    {
        Slate slate = new();

        if (subFaction is not null)
        {
            slate.Set(KeyLibrary_SlateStoreAs.SubRatkinFactionStoreAs, subFaction);
        }
        if (parentFaction is not null)
        {
            slate.Set(KeyLibrary_SlateStoreAs.ParentRatkinFactionStoreAs, parentFaction);
        }
        if (parentFactionDef is not null)
        {
            slate.Set(KeyLibrary_SlateStoreAs.ParentRatkinFactionDefStoreAs, parentFactionDef);
        }

        return OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, mercyQuestDef, slate, forced: true);
    }

    public override void TalkAction(Pawn talker, Pawn talkWith)
    {
        Find.WindowStack.Add(HelpQuizNodeTree(talkWith));
    }

    private Dialog_NodeTree HelpQuizNodeTree(Pawn talkWith)
    {
        DiaNode rootNode = new(GetTalkText(mercyQuestDef));
        DiaOption acceptOpt = new("OARO_TalkWithHelpSeeker_Accept".Translate())
        {
            action = delegate
            {
                QuestUtility.SendQuestTargetSignals(talkWith.questTags, "AcceptMercyQuest", talkWith.Named("SUBJECT"), mercyQuestDef.Named("QUEST"));
                TalkActionUtility.DisableLordJobTalk(talkWith);
            },
            resolveTree = true
        };

        DiaOption rejectOpt = new("OARO_TalkWithHelpSeeker_Reject".Translate())
        {
            action = delegate
            {
                QuestUtility.SendQuestTargetSignals(talkWith.questTags, "RejectMercyQuest", talkWith.Named("SUBJECT"), mercyQuestDef.Named("QUEST"));
                TalkActionUtility.DisableLordJobTalk(talkWith);
            },
            resolveTree = true
        };
        DiaOption ignoreOpt = new("OARO_TalkWithHelpSeeker_Ignore".Translate())
        {
            resolveTree = true
        };

        rootNode.options.Add(acceptOpt);
        rootNode.options.Add(rejectOpt);
        rootNode.options.Add(ignoreOpt);

        return new Dialog_NodeTreeWithFactionInfo(rootNode, talkWith.Faction);
    }

    private static TaggedString GetTalkText(QuestScriptDef mercyQuest)
    {
        TaggedString talkText;
        MercyQuestExtension mercyQuestExtension = mercyQuest.GetModExtension<MercyQuestExtension>();
        if (mercyQuestExtension is null)
        {
            talkText = "OARK_RatkinMercyQuest_HelpSeekDefault".Translate();
        }
        else
        {
            if (mercyQuestExtension.fixedQuestDesc is not null)
            {
                talkText = mercyQuestExtension.fixedQuestDesc;
            }
            else if (mercyQuestExtension.questDescMaker is null)
            {
                talkText = "OARK_RatkinMercyQuest_HelpSeekDefault".Translate();
            }
            else
            {
                GrammarRequest grammarRequest = new();
                grammarRequest.Includes.Add(mercyQuestExtension.questDescMaker);
                talkText = GrammarResolver.Resolve("r_text", grammarRequest);
            }
        }
        return talkText;
    }
}
