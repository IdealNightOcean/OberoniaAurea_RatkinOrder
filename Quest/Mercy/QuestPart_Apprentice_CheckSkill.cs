using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 小学徒技能判定（内部特化类）
/// </summary>
internal sealed class QuestPart_Apprentice_CheckSkill : QuestPart
{
    public string InSignalCheckSkill;
    public string InSignalSuccessLeave;
    public string InSignalStay;

    public string OutSignalSuccess;
    public string OutSignalFail;
    public string OutSignalChecked;

    public string OutSignalSkillSuccessEnd;
    public string OutSignalSkillFailEnd;

    private bool skillSuccess;
    private bool skillChecked;
    public Pawn Apprentice;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignalCheckSkill, "InSignalCheckSkill");
        Scribe_Values.Look(ref InSignalSuccessLeave, "InSignalSuccessLeave");
        Scribe_Values.Look(ref InSignalStay, "InSignalStay");

        Scribe_Values.Look(ref OutSignalSuccess, "OutSignalSuccess");
        Scribe_Values.Look(ref OutSignalFail, "OutSignalFail");
        Scribe_Values.Look(ref OutSignalChecked, "OutSignalChecked");

        Scribe_Values.Look(ref OutSignalSkillSuccessEnd, "OutSignalSkillSuccessEnd");
        Scribe_Values.Look(ref OutSignalSkillFailEnd, "OutSignalSkillFailEnd");

        Scribe_Values.Look(ref skillSuccess, "skillSuccess", defaultValue: false);
        Scribe_Values.Look(ref skillChecked, "skillChecked", defaultValue: false);
        Scribe_References.Look(ref Apprentice, "Apprentice");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalCheckSkill = null;
        InSignalSuccessLeave = null;
        InSignalStay = null;

        OutSignalSuccess = null;
        OutSignalFail = null;
        OutSignalChecked = null;

        OutSignalSkillSuccessEnd = null;
        OutSignalSkillFailEnd = null;

        Apprentice = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);

        if (signal.tag == InSignalSuccessLeave)
        {
            SendEndSignal();
        }
        else if (signal.tag == InSignalStay)
        {
            Apprentice?.apparel.UnlockAll();
            SendEndSignal();
        }
        else if (!skillChecked && signal.tag == InSignalCheckSkill)
        {
            CheckApprenticeSkill();
        }
    }

    private void SendEndSignal()
    {
        if (skillSuccess)
        {
            Find.SignalManager.SendSignal(new Signal(OutSignalSkillSuccessEnd));
        }
        else
        {
            Find.SignalManager.SendSignal(new Signal(OutSignalSkillFailEnd));
        }
    }

    private void CheckApprenticeSkill()
    {
        try
        {
            if (Apprentice is null || Apprentice.skills is null)
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalFail, Apprentice.Named(KeyLibrary_FormatArgName.SUBJECT)));
            }
            else
            {
                if (CheckSkill(SkillDefOf.Construction)
                  || CheckSkill(SkillDefOf.Plants)
                  || CheckSkill(SkillDefOf.Crafting)
                  || CheckSkill(SkillDefOf.Artistic))
                {
                    skillSuccess = true;
                    Find.SignalManager.SendSignal(new Signal(OutSignalSuccess, Apprentice.Named(KeyLibrary_FormatArgName.SUBJECT)));
                }
                else
                {
                    skillSuccess = false;
                    Find.SignalManager.SendSignal(new Signal(OutSignalFail, Apprentice.Named(KeyLibrary_FormatArgName.SUBJECT)));
                }
            }
        }
        finally
        {
            skillChecked = true;
            Find.SignalManager.SendSignal(new Signal(OutSignalChecked, Apprentice.Named(KeyLibrary_FormatArgName.SUBJECT)));
        }
    }

    private bool CheckSkill(SkillDef skill)
    {
        return Apprentice.skills.GetSkill(skill).GetLevel() >= 10;
    }
}