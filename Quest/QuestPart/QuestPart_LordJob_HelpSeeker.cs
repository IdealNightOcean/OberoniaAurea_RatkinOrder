using RimWorld;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 善行求助者的 LordJob | TalkAction
/// </summary>
public class QuestPart_LordJob_HelpSeeker : QuestPart_LordJob_CommomTalk
{
    public QuestScriptDef MmercyQuestDef;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref MmercyQuestDef, "MmercyQuestDef");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        MmercyQuestDef = null;
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
