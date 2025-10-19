using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class SpecialLetterDefBase : Def
{
    [MustTranslate]
    public string sender;

    //label使用Def字段

    [MustTranslate]
    public string text;

    public OrderLetter.LetterType letterType = OrderLetter.LetterType.Normal;

    public bool absolutelyUnique;
}


public class SpecialLetterDef : SpecialLetterDefBase;