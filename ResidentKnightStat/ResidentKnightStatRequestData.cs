using System;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatRequestData
{
    public ResidentKnight Knight { get; }
    public ResidentKnightStatDef StatDef { get; }

    public ResidentKnightStatRequestData(ResidentKnight knight, ResidentKnightStatDef statDef)
    {
        Knight = knight ?? throw new ArgumentNullException(nameof(knight));
        StatDef = statDef ?? throw new ArgumentNullException(nameof(statDef));
    }
}
