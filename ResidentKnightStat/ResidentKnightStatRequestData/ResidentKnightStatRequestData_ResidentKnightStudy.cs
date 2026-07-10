using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatRequestData_ResidentKnightStudy : ResidentKnightStatRequestData
{
    public IReadOnlyDictionary<KnightChivalryDef, int> MedalsCost { get; set; }
    public KnightVirtueDef VirtueDef { get; set; }

    public ResidentKnightStatRequestData_ResidentKnightStudy(ResidentKnight knight, ResidentKnightStatDef statDef) : base(knight, statDef) { }
}
