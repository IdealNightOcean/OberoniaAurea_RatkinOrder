using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightAcademicStage
{
    [MustTranslate]
    public string label;
    [MustTranslate]
    public string shortDescription;
    [MustTranslate]
    public string description;

    public virtual void OnAcademicLevelUpgrade(Pawn pawn) { }
}