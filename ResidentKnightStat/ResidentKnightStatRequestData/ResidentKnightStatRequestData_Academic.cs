using System;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatRequestData_Academic : ResidentKnightStatRequestData
{
    public KnightAcademicDef AcademicDef { get; set; }

    public ResidentKnightStatRequestData_Academic(ResidentKnight knight, ResidentKnightStatDef statDef, KnightAcademicDef academicDef) : base(knight, statDef)
    {
        AcademicDef = academicDef ?? throw new ArgumentNullException(nameof(academicDef));
        OtherChivalry = academicDef.chivalry;
    }
}
