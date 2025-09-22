using RimWorld.QuestGen;
using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_AddEffectTag : QuestNode
{
    public SlateRef<IEnumerable<string>> tagsToAdd;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        if (!QuestPart_EffectTags.TryGetEffectTags(QuestGen.quest, addPartIfMiss: true, out QuestPart_EffectTags questPart_EffectTags))
        {
            return;
        }
        questPart_EffectTags.AddTags(tagsToAdd.GetValue(QuestGen.slate));
    }
}