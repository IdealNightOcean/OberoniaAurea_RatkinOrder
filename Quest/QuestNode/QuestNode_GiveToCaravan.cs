using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GiveToCaravan : QuestNode
{
    [NoTranslate]
    public SlateRef<string> inSignalWithCaravan;
    public SlateRef<IEnumerable<Thing>> things;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        QuestPart_GiveToCaravan questPart_GiveToCaravan = new()
        {
            inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignalWithCaravan.GetValue(QuestGen.slate)) ?? QuestGen.slate.Get<string>(OARO_KeyLibrary_SlateStoreAs.inSignal),
            Things = things.GetValue(QuestGen.slate)
        };
        QuestGen.quest.AddPart(questPart_GiveToCaravan);
    }
}
