using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_OrderCrewAidDelay : QuestNode_Delay
{
    public SlateRef<RatkinOrder> ratkinOrder;
    public SlateRef<IEnumerable<Pawn>> pawns;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;

        QuestPart_OrderCrewAidDelay questPart_OrderCrewAidDelay = new()
        {
            delayTicks = delayTicks.GetValue(slate),

            inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal"),
            inSignalDisable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalDisable.GetValue(slate)),
            reactivatable = reactivatable.GetValue(slate),
            ratkinOrder = ratkinOrder.GetValue(slate)
        };
        questPart_OrderCrewAidDelay.pawns.AddRange(pawns.GetValue(slate));


        if (!inspectStringTargets.GetValue(slate).EnumerableNullOrEmpty())
        {
            questPart_OrderCrewAidDelay.inspectString = inspectString.GetValue(slate);
            questPart_OrderCrewAidDelay.inspectStringTargets = [.. inspectStringTargets.GetValue(slate)];
        }
        if (isQuestTimeout.GetValue(slate))
        {
            questPart_OrderCrewAidDelay.isBad = true;
            questPart_OrderCrewAidDelay.expiryInfoPart = "QuestExpiresIn".Translate();
            questPart_OrderCrewAidDelay.expiryInfoPartTip = "QuestExpiresOn".Translate();
        }
        else
        {
            questPart_OrderCrewAidDelay.expiryInfoPart = expiryInfoPart.GetValue(slate);
            questPart_OrderCrewAidDelay.expiryInfoPartTip = expiryInfoPartTip.GetValue(slate);
        }

        if (node is not null)
        {
            QuestGenUtility.RunInnerNode(node, questPart_OrderCrewAidDelay);
        }

        if (!outSignalComplete.GetValue(slate).NullOrEmpty())
        {
            questPart_OrderCrewAidDelay.outSignalsCompleted.Add(QuestGenUtility.HardcodedSignalWithQuestID(outSignalComplete.GetValue(slate)));
        }

        QuestGen.quest.AddPart(questPart_OrderCrewAidDelay);
    }
}


public class QuestPart_OrderCrewAidDelay : QuestPart_Delay
{
    public RatkinOrder ratkinOrder;
    protected int persuadeCount;
    public List<Pawn> pawns = [];

    public int MaxPersuadeCount
    {
        get
        {
            if (ratkinOrder.Esteem >= 1.0f)
            {
                return 3;
            }
            else if (ratkinOrder.Esteem >= 0.7f)
            {
                return 2;
            }
            else if (ratkinOrder.Esteem >= 0.3f)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
    protected override void DelayFinished()
    {
        if (ratkinOrder is null || persuadeCount >= MaxPersuadeCount)
        {
            NotPersuadeToStay();
        }
        else
        {
            enableTick = Find.TickManager.TicksGame;
            delayTicks = 60;
            TaggedString treeText = "OARO_PersuadeToStayInfo".Translate(ratkinOrder.Name, ratkinOrder.Faction);
            Dialog_NodeTree persuadeTree = OberoniaAurea_Frame.OAFrame_DiaUtility.ConfirmDiaNodeTree(treeText,
                                                                                                     "OARO_PersuadeToStay".Translate(), PersuadeToStay,
                                                                                                     "OARO_NotPersuadeToStay".Translate(), NotPersuadeToStay);
            Find.WindowStack.Add(persuadeTree);
        }
    }

    public void PersuadeToStay()
    {
        persuadeCount++;
        enableTick = Find.TickManager.TicksGame;
        delayTicks = 120000;
    }
    public void NotPersuadeToStay()
    {
        pawns.RemoveAll((Pawn x) => x is null);
        foreach (Pawn p in pawns)
        {

        }
        Complete();
    }

    public override void Cleanup()
    {
        ratkinOrder = null;
        pawns = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref ratkinOrder, "ratkinOrder");
        Scribe_Values.Look(ref persuadeCount, "persuadeCount", 0);
        Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            pawns.RemoveAll((Pawn x) => x is null);
        }
    }
}




