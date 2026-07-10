using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberoniaAurea.RatkinOrder.Window;

public class UICache_MentorshipStudent
{
    public ResidentKnight Teacher { get; }
    public ResidentPawn Student { get; }

    public List<(KnightAcademicDef def, int targetLevel)> TaughtableAcademics { get; private set; }
    public int TaughtableAcademicsCount => TaughtableAcademics.Count;

    public float DailyTutoringSuccessChance { get; private set; }
    public string DailyTutoringSuccessChanceExplanation { get; private set; }

    public int RelationBetweenEach { get; private set; }

    public UICache_MentorshipStudent(ResidentKnight teacher, ResidentPawn student)
    {
        Teacher = teacher;
        Student = student;
    }

    public void Refresh()
    {
        TaughtableAcademics = AcademicUtility.GetHigherAcademicsThanB(Teacher, Student).ToList();

        DailyTutoringSuccessChance = AcademicUtility.GetDailyTutoringSuccessChance(Teacher, Student.Pawn, resultOnly: false, out string dailyTutoringSuccessChanceExplanation);
        DailyTutoringSuccessChanceExplanation = dailyTutoringSuccessChanceExplanation;

        RelationBetweenEach = Student.Pawn.relations.OpinionOf(Teacher.Pawn);
    }
}
