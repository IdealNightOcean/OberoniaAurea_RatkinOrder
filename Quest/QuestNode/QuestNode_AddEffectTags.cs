using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_AddEffectTags : QuestNode
{
    public SlateRef<string> addToTestList = KeyLibrary_SlateStoreAs.QuestEffectTags;
    public SlateRef<IEnumerable<string>> tagsToAdd;

    protected override bool TestRunInt(Slate slate)
    {
        IEnumerable<string> tagsToAdd = this.tagsToAdd.GetValue(slate);
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
        if (!QuestPart_EffectTags.TryGetEffectTags(QuestGen.quest, addPartIfMiss: true, out QuestPart_EffectTags questPart_EffectTags))
        {
            return;
        }
        IEnumerable<string> tagsToAdd = this.tagsToAdd.GetValue(QuestGen.slate);
        if (tagsToAdd is not null)
        {
            questPart_EffectTags.AddTags(tagsToAdd);
        }
    }
}