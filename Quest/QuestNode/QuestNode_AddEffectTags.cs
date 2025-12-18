using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_AddEffectTags : QuestNode
{
    /// <summary>
    /// 这是用于TestRun的tag列表
    /// </summary>
    [NoTranslate]
    public SlateRef<string> addToTestList = KeyLibrary_SlateStoreAs.questEffectTags;

    [NoTranslate]
    public SlateRef<IEnumerable<QuestEffectTag>> tagsToAdd;

    protected override bool TestRunInt(Slate slate)
    {
        IEnumerable<string> tagsToAdd = this.tagsToAdd.GetValue(slate)?.Select(t => t.Key);
        if (tagsToAdd is not null)
        {
            QuestGenUtility.AddRangeToOrMakeList(slate: slate,
                                                 name: addToTestList.GetValue(slate),
                                                 objs: tagsToAdd.Cast<object>().ToList());
        }
        return true;
    }

    protected override void RunInt()
    {
        if (QuestGen.quest.TryGetEffectTagsPart(addPartIfMiss: true, out QuestPart_EffectTags questPart_EffectTags))
        {
            questPart_EffectTags.AddTags(tagsToAdd.GetValue(QuestGen.slate));
        }
    }
}