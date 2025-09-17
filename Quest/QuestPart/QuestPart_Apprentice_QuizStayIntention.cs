using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 询问小学徒加入意愿（内部特化类）
/// </summary>
internal class QuestPart_Apprentice_QuizStayIntention : QuestPart
{
    public bool IsNormalLeave;

    public string InSiganl;
    public string InSignalSkillSuccess;
    public string InSignalResolved;

    private bool resolved;
    private bool sentOnce;
    private bool skillSuccess;

    public string OutSignalStay;
    public string OutSignalLeave;

    public Faction Faction;
    public Pawn Apprentice;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref IsNormalLeave, "IsNormalLeave", defaultValue: true);

        Scribe_Values.Look(ref InSiganl, "InSiganl");
        Scribe_Values.Look(ref InSignalSkillSuccess, "InSignalSkillSuccess");
        Scribe_Values.Look(ref InSignalResolved, "InSignalResolved");

        Scribe_Values.Look(ref resolved, "resolved", defaultValue: false);
        Scribe_Values.Look(ref skillSuccess, "skillSuccess", defaultValue: false);
        Scribe_Values.Look(ref sentOnce, "sentOnce", defaultValue: false);

        Scribe_Values.Look(ref OutSignalStay, "OutSignalStay");
        Scribe_Values.Look(ref OutSignalLeave, "OutSignalLeave");

        Scribe_References.Look(ref Faction, "Faction");
        Scribe_References.Look(ref Apprentice, "Apprentice");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSiganl = null;
        InSignalSkillSuccess = null;
        InSignalResolved = null;

        OutSignalStay = null;
        OutSignalLeave = null;

        Faction = null;
        Apprentice = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (sentOnce) { return; }

        if (!skillSuccess && signal.tag == InSignalSkillSuccess)
        {
            skillSuccess = true;
        }
        else if (!IsNormalLeave && signal.tag == InSignalResolved)
        {
            resolved = true;
        }
        else if (signal.tag == InSiganl)
        {
            sentOnce = true;
            TaggedString label;
            TaggedString text;
            if (IsNormalLeave)
            {
                if (skillSuccess)
                {
                    label = "OARO_Apprentice_QuizStayIntentionLabel_NS".Translate();
                    text = "OARO_Apprentice_QuizStayIntention_NS".Translate(Apprentice);
                }
                else
                {
                    label = "OARO_Apprentice_QuizStayIntentionLabel_NF".Translate();
                    text = "OARO_Apprentice_QuizStayIntention_NF".Translate(Apprentice);
                }
            }
            else
            {
                if (resolved)
                {
                    if (skillSuccess)
                    {
                        label = "OARO_Apprentice_QuizStayIntentionLabel_SRS".Translate();
                        text = "OARO_Apprentice_QuizStayIntention_SRS".Translate(Apprentice);
                    }
                    else
                    {
                        label = "OARO_Apprentice_QuizStayIntentionLabel_SRF".Translate();
                        text = "OARO_Apprentice_QuizStayIntention_SRF".Translate(Apprentice);
                    }
                }
                else
                {
                    if (skillSuccess)
                    {
                        label = "OARO_Apprentice_QuizStayIntentionLabel_SS".Translate();
                        text = "OARO_Apprentice_QuizStayIntention_SS".Translate(Apprentice);
                    }
                    else
                    {
                        label = "OARO_Apprentice_QuizStayIntentionLabel_SF".Translate();
                        text = "OARO_Apprentice_QuizStayIntention_SF".Translate(Apprentice);
                    }
                }
            }

            ChoiceLetter_Apprentice_QuizStayIntention letter = (ChoiceLetter_Apprentice_QuizStayIntention)LetterMaker.MakeLetter(
                label: label,
                text: text,
                def: OARO_LetterDefOf.OARO_Apprentice_QuizStayIntentionLetter,
                lookTargets: Apprentice,
                relatedFaction: Faction,
                quest: quest);
            letter.outSignalStay = OutSignalStay;
            letter.outSignalLeave = OutSignalLeave;
            letter.apprentice = Apprentice;
            letter.StartTimeout(60000);
            Find.LetterStack.ReceiveLetter(letter);
        }
    }
}