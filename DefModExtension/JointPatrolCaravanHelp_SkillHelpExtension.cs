using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolCaravanHelp_SkillHelpExtension : DefModExtension
{
    [MustTranslate]
    public string requireReason;
    public SkillRequirement skillRequirement;
    public int ticksNeeded;
}
