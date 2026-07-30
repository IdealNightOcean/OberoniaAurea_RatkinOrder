using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_SetPresetEffectTags : QuestNode
{
    /// <summary>
    /// 这是用于TestRun的tagKey列表
    /// </summary>
    public SlateRef<string> addToTestList = OARO_KeyLibrary_SlateStoreAs.questEffectTags;

    protected override bool TestRunInt(Slate slate)
    {
        if (QuestGen.slate.TryGet(OARO_KeyLibrary_SlateStoreAs.preSetQuestEffectTags, out List<QuestEffectTag> preSetQuestEffectTags))
        {
            if (preSetQuestEffectTags.NullOrEmpty())
            {
                return true;
            }
            List<object> preSetQuestEffectTagKeys = preSetQuestEffectTags.Select(t => t.Key).Cast<object>().ToList();
            QuestGenUtility.AddRangeToOrMakeList(slate: slate,
                                                 name: addToTestList.GetValue(slate),
                                                 objs: preSetQuestEffectTagKeys);
        }
        return true;
    }

    protected override void RunInt()
    {
        if (!QuestGen.quest.TryGetEffectTagsPart(addPartIfMiss: true, out QuestPart_EffectTags questPart_EffectTags))
        {
            return;
        }

        if (QuestGen.slate.TryGet(OARO_KeyLibrary_SlateStoreAs.preSetQuestEffectTags, out List<QuestEffectTag> preSetQuestEffectTags))
        {
            questPart_EffectTags.AddTags(preSetQuestEffectTags);
        }
    }
}
