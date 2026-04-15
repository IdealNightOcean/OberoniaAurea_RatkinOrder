using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal class ChoiceLetter_InDistressKnightLeave : ChoiceLetter_RatkinOrder
{
    internal string OutSignalRecruit;
    internal string OutSignalMakeLeave;
    internal List<Pawn> Pawns;

    public override bool CanDismissWithRightClick => false;

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (quest?.State != QuestState.Ongoing || ArchivedOnly)
            {
                yield return Option_Close;
                yield break;
            }

            yield return RecruitOption();
            yield return RejectOpt;
            yield return Option_Postpone;
            if (quest is not null)
            {
                yield return Option_ViewInQuestsTab(postpone: true);
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref OutSignalRecruit, nameof(OutSignalRecruit));
        Scribe_Values.Look(ref OutSignalMakeLeave, nameof(OutSignalMakeLeave));

        Scribe_Collections.Look(ref Pawns, nameof(Pawns), LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Pawns?.RemoveAll(p => p is null);
        }
    }

    public override void Removed()
    {
        base.Removed();
        OutSignalRecruit = null;
        OutSignalMakeLeave = null;
        Pawns = null;
    }

    private DiaOption RejectOpt => new("OARO_InDistressKnight_SendAway".Translate())
    {
        action = delegate
        {
            Find.SignalManager.SendSignal(new Signal(OutSignalMakeLeave));
            Find.LetterStack.RemoveLetter(this);
        },
        resolveTree = true
    };

    private DiaOption RecruitOption()
    {
        DiaOption opt = new("OARO_InDistressKnight_Recruit".Translate())
        {
            action = Recruit,
            resolveTree = true
        };

        if (OrderStationHandler.Instance.OrderHallRoom is null)
        {
            opt.Disable("OARO_NoRatkinOrderHall".Translate());
        }
        return opt;
    }

    private void Recruit()
    {
        if (Pawns is not null)
        {
            Pawns.RemoveAll(p => p.DestroyedOrNull());
            foreach (Pawn p in Pawns)
            {
                OAFrame_PawnUtility.MakePawnJoinPlayer(p);
                ResidentPawnsManager.Instance.TryRegisterKnight(p);
            }
        }

        Find.SignalManager.SendSignal(new Signal(OutSignalRecruit));
        Find.LetterStack.RemoveLetter(this);
    }
}