using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 善行求助者的 LordJob | TalkAction
/// </summary>
public class QuestPart_LordJob_HelpSeeker : QuestPart_LordJob_CommomTalk
{
    public string InSignalAccept;
    public string IninSignalReject;

    private bool canTriggered = true;

    public QuestScriptDef MmercyQuestDef;

    public Faction SubFaction;
    public Faction ParentFaction;
    public FactionDef ParentFactionDef;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignalAccept, "InSignalAccept");
        Scribe_Values.Look(ref IninSignalReject, "IninSignalReject");

        Scribe_Values.Look(ref canTriggered, "canTriggered", defaultValue: true);

        Scribe_Defs.Look(ref MmercyQuestDef, "MmercyQuestDef");

        Scribe_References.Look(ref SubFaction, "SubFaction");
        Scribe_References.Look(ref ParentFaction, "ParentFaction");
        Scribe_Defs.Look(ref ParentFactionDef, "ParentFactionDef");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalAccept = null;
        IninSignalReject = null;

        MmercyQuestDef = null;

        SubFaction = null;
        ParentFaction = null;
        ParentFactionDef = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (canTriggered && signal.tag == InSignalAccept)
        {
            canTriggered = false;
            TryTriggerQuest();
        }
        else if (signal.tag == IninSignalReject)
        {
            canTriggered = false;
            this.DeregisterTalkAction();
            talkWith = null;
        }
    }

    private bool TryTriggerQuest()
    {
        Slate slate = new();

        if (SubFaction is not null)
        {
            slate.Set(KeyLibrary_SlateStoreAs.SubRatkinFaction, SubFaction);
        }
        if (ParentFaction is not null)
        {
            slate.Set(KeyLibrary_SlateStoreAs.ParentRatkinFaction, ParentFaction);
        }
        if (ParentFactionDef is not null)
        {
            slate.Set(KeyLibrary_SlateStoreAs.ParentRatkinFactionDef, ParentFactionDef);
        }

        return OAFrame_QuestUtility.TryGenerateQuestAndMakeAvailable(out _, MmercyQuestDef, slate, forced: true);
    }

    public override void TalkAction(Pawn talker, Pawn talkWith)
    {
        Find.WindowStack.Add(HelpQuizNodeTree(talkWith));
    }

    private Dialog_NodeTree HelpQuizNodeTree(Pawn talkWith)
    {
        DiaNode rootNode = new(GetTalkText(MmercyQuestDef));
        DiaOption acceptOpt = new("OARO_TalkWithHelpSeeker_Accept".Translate())
        {
            action = delegate
            {
                QuestUtility.SendQuestTargetSignals(talkWith.questTags, "AcceptMercyQuest", talkWith.Named("SUBJECT"), MmercyQuestDef.Named("QUEST"));
                TalkActionUtility.DisableLordJobTalk(talkWith);
            },
            resolveTree = true
        };

        DiaOption rejectOpt = new("OARO_TalkWithHelpSeeker_Reject".Translate())
        {
            action = delegate
            {
                QuestUtility.SendQuestTargetSignals(talkWith.questTags, "RejectMercyQuest", talkWith.Named("SUBJECT"), MmercyQuestDef.Named("QUEST"));
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
