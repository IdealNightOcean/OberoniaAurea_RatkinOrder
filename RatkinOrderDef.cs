using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrderDef : Def
{
    [MustTranslate]
    public string fixedName;

    public RulePackDef ratkinOrderNameMaker;

    public RulePackDef branchNameMaker;

}
