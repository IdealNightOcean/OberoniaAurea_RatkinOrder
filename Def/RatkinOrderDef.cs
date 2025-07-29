using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrderDef : Def
{
    [MustTranslate]
    public string fixedName;

    public RulePackDef nameMaker;

    public RulePackDef branchNameMaker;

    public Color? color;

}