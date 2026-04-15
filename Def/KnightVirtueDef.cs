using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;


/// <summary>
/// 骑士美德Def
/// </summary>
public class KnightVirtueDef : Def
{
    public KnightPersonality relatedPersonality;

    public int maxLevel = 3;

    public KnightVirtueType virtueType = KnightVirtueType.Normal;

    public KnightAcademicDef relatedAcademicDef;
    public int unlockOnAcademicLevel = -1;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (virtueType == KnightVirtueType.Academic && relatedAcademicDef is null)
        {
            yield return "Academic virtue type requires a related academic definition.";
        }
    }

}
