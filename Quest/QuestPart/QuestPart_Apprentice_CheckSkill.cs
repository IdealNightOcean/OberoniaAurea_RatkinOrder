using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_Apprentice_CheckSkill : QuestPart
{
    public string inSignalCheckSkill;
    public string inSignalSuccessLeave;
    public string inSignalStay;

    public string outSignalSuccess;
    public string outSignalFail;
    public string outSignalChecked;

    public string outSignalSkillSuccessEnd;
    public string outSignalSkillFailEnd;

    private bool skillSuccess;
    private bool skillChecked;
    public Pawn apprentice;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignalCheckSkill, "inSignalCheckSkill");
        Scribe_Values.Look(ref inSignalSuccessLeave, "inSignalSuccessLeave");
        Scribe_Values.Look(ref inSignalStay, "inSignalStay");

        Scribe_Values.Look(ref outSignalSuccess, "outSignalSuccess");
        Scribe_Values.Look(ref outSignalFail, "outSignalFail");
        Scribe_Values.Look(ref outSignalChecked, "outSignalChecked");

        Scribe_Values.Look(ref outSignalSkillSuccessEnd, "outSignalSkillSuccessEnd");
        Scribe_Values.Look(ref outSignalSkillFailEnd, "outSignalSkillFailEnd");

        Scribe_Values.Look(ref skillSuccess, "skillSuccess", defaultValue: false);
        Scribe_Values.Look(ref skillChecked, "skillChecked", defaultValue: false);
        Scribe_References.Look(ref apprentice, "apprentice");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        inSignalCheckSkill = null;
        inSignalSuccessLeave = null;
        inSignalStay = null;

        outSignalSuccess = null;
        outSignalFail = null;
        outSignalChecked = null;

        outSignalSkillSuccessEnd = null;
        outSignalSkillFailEnd = null;

        apprentice = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);

        if (signal.tag == inSignalSuccessLeave)
        {
            SendEndSignal();
        }
        else if (signal.tag == inSignalStay)
        {
            apprentice?.apparel.UnlockAll();
            SendEndSignal();
        }
        else if (!skillChecked && signal.tag == inSignalCheckSkill)
        {
            CheckApprenticeSkill();
        }
    }

    private void SendEndSignal()
    {
        if (skillSuccess)
        {
            Find.SignalManager.SendSignal(new Signal(outSignalSkillSuccessEnd));
        }
        else
        {
            Find.SignalManager.SendSignal(new Signal(outSignalSkillFailEnd));
        }
    }

    private void CheckApprenticeSkill()
    {
        try
        {
            if (apprentice is null || apprentice.skills is null)
            {
                Find.SignalManager.SendSignal(new Signal(outSignalFail, apprentice.Named("SUBJECT")));
            }
            else
            {
                if (CheckSkill(SkillDefOf.Construction)
                  || CheckSkill(SkillDefOf.Plants)
                  || CheckSkill(SkillDefOf.Crafting)
                  || CheckSkill(SkillDefOf.Artistic))
                {
                    skillSuccess = true;
                    Find.SignalManager.SendSignal(new Signal(outSignalSuccess, apprentice.Named("SUBJECT")));
                }
                else
                {
                    skillSuccess = false;
                    Find.SignalManager.SendSignal(new Signal(outSignalFail, apprentice.Named("SUBJECT")));
                }
            }
        }
        finally
        {
            skillChecked = true;
            Find.SignalManager.SendSignal(new Signal(outSignalChecked, apprentice.Named("SUBJECT")));
        }
    }

    private bool CheckSkill(SkillDef skill)
    {
        return apprentice.skills.GetSkill(skill).GetLevel() >= 10;
    }
}