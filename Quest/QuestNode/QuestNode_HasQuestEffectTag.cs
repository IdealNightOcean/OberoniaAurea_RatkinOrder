using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_HasQuestEffectTag : QuestNode
{
    [NoTranslate]
    public SlateRef<string> tag;

    /// <summary>
    /// 这是用于TestRun的tag列表
    /// </summary>
    [NoTranslate]
    public SlateRef<string> tagsListForTestRun = KeyLibrary_SlateStoreAs.questEffectTags;

    public QuestNode matchNode;
    public QuestNode noMatchNode;

    protected override bool TestRunInt(Slate slate)
    {
        if (!slate.TryGet(tag.GetValue(slate), out string tagToCheck))
        {
            return false;
        }

        if (QuestGenUtility.IsInList(slate: slate,
                                     name: tagsListForTestRun.GetValue(slate),
                                     obj: tagToCheck))
        {
            return matchNode?.TestRun(slate) ?? true;
        }
        else
        {
            return noMatchNode?.TestRun(slate) ?? true;
        }
    }

    protected override void RunInt()
    {
        if (string.IsNullOrEmpty(tag.GetValue(QuestGen.slate)) || !QuestGen.quest.TryGetEffectTagsPart(addPartIfMiss: false, out QuestPart_EffectTags questPart_EffectTags))
        {
            noMatchNode?.Run();
            return;
        }

        if (questPart_EffectTags.HasTag(tag.GetValue(QuestGen.slate)))
        {
            matchNode?.Run();
        }
        else
        {
            noMatchNode?.Run();
        }
    }
}
