using System;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatRequestData_Academic : ResidentKnightStatRequestData
{
    public KnightAcademicDef AcademicDef { get; set; }
    public int CurLevel { get; set; }
    public int TargetLevel { get; set; }

    public ResidentKnightStatRequestData_Academic(ResidentKnight knight, ResidentKnightStatDef statDef, KnightAcademicDef academicDef) : base(knight, statDef)
    {
        AcademicDef = academicDef ?? throw new ArgumentNullException(nameof(academicDef));
    }

    public ResidentKnightStatRequestData_Academic(ResidentKnight knight, ResidentKnightStatDef statDef, KnightAcademicDef academicDef, int curLevel, int targetLevel) : base(knight, statDef)
    {
        AcademicDef = academicDef ?? throw new ArgumentNullException(nameof(academicDef));
        CurLevel = curLevel;
        TargetLevel = targetLevel;
    }
}
