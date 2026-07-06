using System;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatRequestData
{
    public ResidentKnight Knight { get; set; }
    public ResidentKnightStatDef StatDef { get; set; }
    public KnightChivalryDef OtherChivalry { get; set; }

    public float BaseValue { get; set; }

    public ResidentKnightStatRequestData() { }
    public ResidentKnightStatRequestData(ResidentKnight knight, ResidentKnightStatDef statDef)
    {
        Knight = knight ?? throw new ArgumentNullException(nameof(knight));
        StatDef = statDef ?? throw new ArgumentNullException(nameof(statDef));
    }

    public ResidentKnightStatRequestData(ResidentKnight knight, ResidentKnightStatDef statDef, float baseValue) : this(knight, statDef)
    {
        BaseValue = baseValue;
    }

}
