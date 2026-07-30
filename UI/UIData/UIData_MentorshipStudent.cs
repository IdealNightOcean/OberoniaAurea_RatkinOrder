using NightOcean;
using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIData_MentorshipStudent : UIDataBase
{
    public ResidentKnight Teacher { get; private set; }
    public ResidentPawn Student { get; private set; }
    public override bool IsValid => Student is not null && Teacher is not null;

    public (int s2t, int t2s) RelationBetweenEach { get; private set; } = (0, 0);

    public float DailyTutoringSuccessChance { get; private set; }

    public LazyMutable<string> DailyTutoringSuccessChanceExplanation { get; }


    public List<(KnightAcademicDef def, int targetLevel)> TaughtableAcademics { get; } = [];
    public int TaughtableAcademicsCount => TaughtableAcademics.Count;


    public UIData_MentorshipStudent(ResidentKnight teacher, ResidentPawn student)
    {
        this.Teacher = teacher;
        this.Student = student;

        DailyTutoringSuccessChanceExplanation = new(refreshFunc: RefreshDailyTutoringSuccessChanceExplanation);
    }

    public void ResetData(ResidentKnight teacher, ResidentPawn student)
    {
        this.Teacher = teacher;
        this.Student = student;

        IsReady = false;
    }

    protected override void RefreshInner()
    {
        TaughtableAcademics.Clear();
        TaughtableAcademics.AddRange(AcademicUtility.GetHigherAcademicsThanB(Teacher, Student));
        DailyTutoringSuccessChance = AcademicUtility.GetDailyTutoringSuccessChance(Teacher, Student.Pawn, resultOnly: true, out _);
        DailyTutoringSuccessChanceExplanation.MarkDirty();
        RelationBetweenEach = (Student.Pawn.relations.OpinionOf(Teacher.Pawn), Teacher.Pawn.relations.OpinionOf(Student.Pawn));
    }

    private string RefreshDailyTutoringSuccessChanceExplanation()
    {
        if (!IsValid)
            return string.Empty;

        DailyTutoringSuccessChance = AcademicUtility.GetDailyTutoringSuccessChance(Teacher, Student.Pawn, resultOnly: false, out string dailyTutoringSuccessChanceExplanation);
        return dailyTutoringSuccessChanceExplanation;
    }
}
