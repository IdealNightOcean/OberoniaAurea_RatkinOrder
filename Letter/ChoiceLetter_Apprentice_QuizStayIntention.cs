using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal class ChoiceLetter_Apprentice_QuizStayIntention : ChoiceLetter
{
    public string outSignalStay;
    public string outSignalLeave;
    public Pawn apprentice;

    public override bool CanDismissWithRightClick => false;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref outSignalStay, "outSignalStay");
        Scribe_Values.Look(ref outSignalLeave, "outSignalLeave");

        Scribe_References.Look(ref apprentice, "apprentice");
    }

    public override void Removed()
    {
        base.Removed();
        outSignalStay = null;
        outSignalLeave = null;

        apprentice = null;
    }

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (ArchivedOnly)
            {
                yield return Option_Close;
            }
            else
            {
                yield return Option_Stay;
                yield return Option_Leave;
                yield return Option_Postpone;
            }

            if (quest is not null)
            {
                yield return Option_ViewInQuestsTab(postpone: true);
            }
        }
    }

    private DiaOption Option_Stay => new("OARO_Apprentice_Stay".Translate(apprentice.Named(KeyLibrary_FormatArgName.PAWN)))
    {
        action = delegate
        {
            Find.SignalManager.SendSignal(new Signal(outSignalStay, apprentice.Named(KeyLibrary_FormatArgName.SUBJECT)));
            Find.LetterStack.RemoveLetter(this);
        },
        resolveTree = true
    };

    private DiaOption Option_Leave => new("OARO_Apprentice_Leave".Translate(apprentice.Named(KeyLibrary_FormatArgName.PAWN)))
    {
        action = delegate
        {
            Find.SignalManager.SendSignal(new Signal(outSignalLeave, apprentice.Named(KeyLibrary_FormatArgName.SUBJECT)));
            Find.LetterStack.RemoveLetter(this);
        },
        resolveTree = true
    };
}