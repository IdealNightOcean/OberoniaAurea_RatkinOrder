using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_InDistressKnightLeaveLetter : QuestPart_RimOrderLetter
{
    public string InSignalRemovePawn;
    public string OutSignalRecruit;
    public string OutSignalMakeLeave;

    public List<Pawn> Pawns;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignalRemovePawn, nameof(InSignalRemovePawn));
        Scribe_Values.Look(ref OutSignalRecruit, nameof(OutSignalRecruit));
        Scribe_Values.Look(ref OutSignalMakeLeave, nameof(OutSignalMakeLeave));

        Scribe_Collections.Look(ref Pawns, nameof(Pawns), LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Pawns?.RemoveAll(p => p is null);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalRemovePawn = null;
        OutSignalRecruit = null;
        OutSignalMakeLeave = null;

        Pawns = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (Pawns.NullOrEmpty())
        {
            if (signal.tag == InSignal)
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalMakeLeave));
            }
            return;
        }
        else if (signal.tag == InSignalRemovePawn && signal.args.TryGetArg(KeyLibrary_FormatArgName.SUBJECT, out Pawn p))
        {
            Pawns.Remove(p);
        }
    }

    protected override void PostGenerateLetter(ChoiceLetter choiceLetter, out bool letterValid)
    {
        base.PostGenerateLetter(choiceLetter, out _);
        if (choiceLetter is ChoiceLetter_InDistressKnightLeave questLetter)
        {
            questLetter.OutSignalRecruit = OutSignalRecruit;
            questLetter.OutSignalMakeLeave = OutSignalMakeLeave;
            questLetter.StartTimeout(30000);
            if (!Pawns.NullOrEmpty())
            {
                questLetter.Pawns ??= [];
                questLetter.Pawns.AddRange(Pawns);
            }
        }

        letterValid = true;
    }

}