using System.Collections.Generic;
using System.Linq;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIData_MentorshipStudent : UIDataBase
{
    public ResidentKnight Teacher { get; }
    public ResidentPawn Student { get; }

    public List<(KnightAcademicDef def, int targetLevel)> TaughtableAcademics { get; private set; }
    public int TaughtableAcademicsCount => TaughtableAcademics.Count;

    public float DailyTutoringSuccessChance { get; private set; }

    private string dailyTutoringSuccessChanceExplanation;
    public string DailyTutoringSuccessChanceExplanation
    {
        get
        {
            if (dailyTutoringSuccessChanceExplanation is null)
            {
                RefreshDailyTutoringSuccessChanceExplanation();
            }
            return dailyTutoringSuccessChanceExplanation;
        }
    }

    public int RelationBetweenEach { get; private set; }

    public UIData_MentorshipStudent(ResidentKnight teacher, ResidentPawn student)
    {
        Teacher = teacher;
        Student = student;
    }

    protected override void RefreshInner()
    {
        TaughtableAcademics = AcademicUtility.GetHigherAcademicsThanB(Teacher, Student).ToList();

        DailyTutoringSuccessChance = AcademicUtility.GetDailyTutoringSuccessChance(Teacher, Student.Pawn, resultOnly: true, out _);
        dailyTutoringSuccessChanceExplanation = null;
        RelationBetweenEach = Student.Pawn.relations.OpinionOf(Teacher.Pawn);
    }

    private void RefreshDailyTutoringSuccessChanceExplanation()
    {
        DailyTutoringSuccessChance = AcademicUtility.GetDailyTutoringSuccessChance(Teacher, Student.Pawn, resultOnly: false, out dailyTutoringSuccessChanceExplanation);
    }
}
