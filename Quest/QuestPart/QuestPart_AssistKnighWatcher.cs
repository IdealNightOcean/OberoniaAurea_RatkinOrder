using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_AssistKnighWatcher : QuestPart_Delay
{
    public string InsignalRemovePawn;
    public ThoughtDef ThoughtToAdd;

    public RatkinOrder RatkinOrder;
    public List<Pawn> Pawns;

    private int retainCount;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InsignalRemovePawn, nameof(InsignalRemovePawn));
        Scribe_Defs.Look(ref ThoughtToAdd, nameof(ThoughtToAdd));

        Scribe_References.Look(ref RatkinOrder, nameof(RatkinOrder));
        Scribe_Values.Look(ref retainCount, nameof(retainCount), 0);
        Scribe_Collections.Look(ref Pawns, nameof(Pawns), LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Pawns?.RemoveAll(p => p is null);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InsignalRemovePawn = null;
        ThoughtToAdd = null;
        RatkinOrder = null;
        Pawns = null;

        retainCount = 0;
    }

    private bool CanRetain
    {
        get
        {
            return retainCount < (RatkinOrder?.Esteem ?? 0) switch
            {
                < 30 => 0,
                < 70 => 1,
                < 99 => 2,
                _ => 3
            };
        }
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (Pawns is not null && signal.tag == InsignalRemovePawn)
        {
            if (signal.args.TryGetArg(KeyLibrary_FormatArgName.SUBJECT, out Pawn p))
            {
                Pawns.Remove(p);
            }
        }
    }

    public override void DoDebugWindowContents(Rect innerRect, ref float curY)
    {
        if (State == QuestPartState.Enabled)
        {
            Rect rect = new(innerRect.x, curY, 500f, 25f);
            if (Widgets.ButtonText(rect, "End " + ToString()))
            {
                DelayFinished();
            }

            curY += rect.height + 4f;
        }
    }

    protected override void DelayFinished()
    {
        if (CanRetain)
        {
            Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.ConfirmDiaNodeTreeWithRatkinOrderInfo(
                text: "OARO_AssistKnight_RetainInfo".Translate(RatkinOrder.NameColored.Named(OARO_KeyLibrary_FormatArgName.OrderName)),
                ratkinOrder: RatkinOrder,
                acceptText: "OARO_AssistKnight_Retain".Translate(),
                acceptAction: DelayLeave,
                rejectText: "OARO_AssistKnight_SeeOff".Translate(),
                rejectAction: base.DelayFinished);

            Find.WindowStack.Add(nodeTree);
        }
        else
        {
            base.DelayFinished();
        }
    }

    private void DelayLeave()
    {
        if (Pawns is null)
        {
            base.DelayFinished();
            return;
        }

        retainCount++;
        delayTicks = 120000;
        enableTick = Find.TickManager.TicksGame;

        if (ThoughtToAdd is null)
        {
            return;
        }

        Pawns.RemoveAll(p => p.DestroyedOrNull());
        foreach (Pawn p in Pawns)
        {
            if (p.needs.mood is null)
            {
                continue;
            }

            Thought_Memory t = p.needs.mood.thoughts.memories.GetFirstMemoryOfDef(ThoughtToAdd);
            if (t is null)
            {
                t = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtToAdd);
                t.durationTicksOverride = 120000;
                t.SetForcedStage(1);
                p.needs.mood.thoughts.memories.TryGainMemory(t);
            }
            else
            {
                t.Renew();
                t.durationTicksOverride = 120000;
                t.SetForcedStage(1);
            }
        }
    }

}
