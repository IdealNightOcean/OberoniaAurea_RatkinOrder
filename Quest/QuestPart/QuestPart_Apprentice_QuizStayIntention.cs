using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_Apprentice_QuizStayIntention : QuestPart
{
    public bool isNormalLeave;

    public string inSiganl;
    public string inSignalSkillSuccess;
    public string inSignalResolved;

    private bool resolved;
    private bool sentOnce;
    private bool skillSuccess;

    public string outSignalStay;
    public string outSignalLeave;

    public Faction faction;
    public Pawn apprentice;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref isNormalLeave, "isNormalLeave", defaultValue: true);

        Scribe_Values.Look(ref inSiganl, "inSiganl");
        Scribe_Values.Look(ref inSignalSkillSuccess, "inSignalSkillSuccess");
        Scribe_Values.Look(ref inSignalResolved, "inSignalResolved");

        Scribe_Values.Look(ref resolved, "resolved", defaultValue: false);
        Scribe_Values.Look(ref skillSuccess, "skillSuccess", defaultValue: false);
        Scribe_Values.Look(ref sentOnce, "sentOnce", defaultValue: false);

        Scribe_Values.Look(ref outSignalStay, "outSignalStay");
        Scribe_Values.Look(ref outSignalLeave, "outSignalLeave");

        Scribe_References.Look(ref faction, "faction");
        Scribe_References.Look(ref apprentice, "apprentice");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        inSiganl = null;
        inSignalSkillSuccess = null;
        inSignalResolved = null;

        outSignalStay = null;
        outSignalLeave = null;

        faction = null;
        apprentice = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (sentOnce) { return; }

        if (!skillSuccess && signal.tag == inSignalSkillSuccess)
        {
            skillSuccess = true;
        }
        else if (!isNormalLeave && signal.tag == inSignalResolved)
        {
            resolved = true;
        }
        else if (signal.tag == inSiganl)
        {
            sentOnce = true;
            TaggedString label;
            TaggedString text;
            if (isNormalLeave)
            {
                if (skillSuccess)
                {
                    label = "OARO_Apprentice_QuizStayIntentionLabel_NS".Translate();
                    text = "OARO_Apprentice_QuizStayIntention_NS".Translate(apprentice);
                }
                else
                {
                    label = "OARO_Apprentice_QuizStayIntentionLabel_NF".Translate();
                    text = "OARO_Apprentice_QuizStayIntention_NF".Translate(apprentice);
                }
            }
            else
            {
                if (resolved)
                {
                    if (skillSuccess)
                    {
                        label = "OARO_Apprentice_QuizStayIntentionLabel_SRS".Translate();
                        text = "OARO_Apprentice_QuizStayIntention_SRS".Translate(apprentice);
                    }
                    else
                    {
                        label = "OARO_Apprentice_QuizStayIntentionLabel_SRF".Translate();
                        text = "OARO_Apprentice_QuizStayIntention_SRF".Translate(apprentice);
                    }
                }
                else
                {
                    if (skillSuccess)
                    {
                        label = "OARO_Apprentice_QuizStayIntentionLabel_SS".Translate();
                        text = "OARO_Apprentice_QuizStayIntention_SS".Translate(apprentice);
                    }
                    else
                    {
                        label = "OARO_Apprentice_QuizStayIntentionLabel_SF".Translate();
                        text = "OARO_Apprentice_QuizStayIntention_SF".Translate(apprentice);
                    }
                }
            }

            ChoiceLetter_Apprentice_QuizStayIntention letter = (ChoiceLetter_Apprentice_QuizStayIntention)LetterMaker.MakeLetter(label: label,
                                                                                                                                 text: text,
                                                                                                                                 def: OARO_ModDefOf.OARO_Apprentice_QuizStayIntentionLetter,
                                                                                                                                 lookTargets: apprentice,
                                                                                                                                 relatedFaction: faction,
                                                                                                                                 quest: quest);
            letter.outSignalStay = outSignalStay;
            letter.outSignalLeave = outSignalLeave;
            letter.apprentice = apprentice;
            letter.StartTimeout(60000);
            Find.LetterStack.ReceiveLetter(letter);
        }
    }
}