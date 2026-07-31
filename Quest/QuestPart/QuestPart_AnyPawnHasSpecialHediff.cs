using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_AnyPawnHasSpecialHediff : QuestPart
{
    public string inSignalCheck;
    public string inSignalRemovePawn;

    public string outSignalHas;
    public string outSignalNoOneHas;

    public HediffDef hediffDef;
    public List<Pawn> pawns = [];

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignalCheck, "inSignalCheck");
        Scribe_Values.Look(ref inSignalRemovePawn, "inSignalRemovePawn");

        Scribe_Values.Look(ref outSignalHas, "outSignalHas");
        Scribe_Values.Look(ref outSignalNoOneHas, "outSignalNoOneHas");

        Scribe_Defs.Look(ref hediffDef, "hediffDef");
        Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            pawns?.RemoveAll(p => p is null);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        inSignalCheck = null;
        inSignalRemovePawn = null;

        outSignalHas = null;
        outSignalNoOneHas = null;

        hediffDef = null;
        pawns = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (!pawns.NullOrEmpty() && signal.tag == inSignalRemovePawn)
        {
            if (signal.args.TryGetArg(KeyLibrary_FormatArgName.SUBJECT, out Pawn p))
            {
                pawns?.Remove(p);
            }
        }
        if (signal.tag == inSignalCheck)
        {
            if (hediffDef is null || pawns.NullOrEmpty())
            {
                Find.SignalManager.SendSignal(new Signal(outSignalNoOneHas));
            }
            else
            {
                foreach (Pawn p in pawns)
                {
                    if (p.health.hediffSet.HasHediff(hediffDef))
                    {
                        Find.SignalManager.SendSignal(new Signal(outSignalHas, p.Named(KeyLibrary_FormatArgName.SUBJECT)));
                        return;
                    }
                }
                Find.SignalManager.SendSignal(new Signal(outSignalNoOneHas));
            }
        }
    }
}