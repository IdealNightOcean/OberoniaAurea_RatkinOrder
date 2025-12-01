using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolCaravanHelp_SkillHelpExtension : DefModExtension
{
    [MustTranslate]
    public string requestHelpReason;
    public SkillDef requireSkill;
    public int minLevel;
    public int ticksNeeded;

    [MustTranslate]
    public string failedThankText;
}
