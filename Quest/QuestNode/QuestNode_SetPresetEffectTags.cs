using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_SetPresetEffectTags : QuestNode
{
    public SlateRef<string> addToTestList = KeyLibrary_SlateStoreAs.QuestEffectTags;

    protected override bool TestRunInt(Slate slate)
    {
        if (QuestGen.slate.TryGet(KeyLibrary_SlateStoreAs.PreSetQuestEffectTags, out List<string> preSetQuestEffectTags))
        {
            if (preSetQuestEffectTags.NullOrEmpty())
            {
                return true;
            }

            QuestGenUtility.AddRangeToOrMakeList(slate: slate,
                                                 name: addToTestList.GetValue(slate),
                                                 objs: preSetQuestEffectTags.Cast<object>().ToList());
        }
        return true;
    }

    protected override void RunInt()
    {
        if (!QuestPart_EffectTags.TryGetEffectTags(QuestGen.quest, addPartIfMiss: true, out QuestPart_EffectTags questPart_EffectTags))
        {
            return;
        }

        if (QuestGen.slate.TryGet(KeyLibrary_SlateStoreAs.PreSetQuestEffectTags, out List<string> preSetQuestEffectTags))
        {
            if (!preSetQuestEffectTags.NullOrEmpty())
            {
                questPart_EffectTags.AddTags(preSetQuestEffectTags);
            }
        }
    }
}
