using OberoniaAurea_Frame;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolCaravanHelpWorker_TouristAreaPatrol : JointPatrolCaravanHelpWorker_FixedCaravan_SkillHelp
{
    public override void FinishWork(FixedCaravan fixedCaravan, Branch branch, WorldObject_JointPatrolCaravanHelpSite_FixedCaravan incidentSite)
    {
        if (fixedCaravan is not null)
        {
            foreach (Pawn p in fixedCaravan.PawnsListForReading)
            {
                p.needs.mood?.thoughts.memories.TryGainMemory(OARO_ThoughtDefOf.OARO_Thought_TouristAreaPatrol);
            }

            (Pawn maxSkillPawn, _) = OberoniaAurea_Frame.Utility.OAFrame_PawnUtility.GetMaxSkillLevelPawn(fixedCaravan.PawnsListForReading, SkillDefOf.Artistic);
            if (maxSkillPawn?.mindState?.inspirationHandler?.TryStartInspiration(InspirationDefOf.Inspired_Creativity, Def.label) ?? false)
            {
                extraRewardText.AppendLine();
                extraRewardText.AppendLine("OARO_TouristAreaPatrol_Inspiration".Translate(maxSkillPawn.Named(KeyLibrary_FormatArgName.PAWN)));
            }
        }
        base.FinishWork(fixedCaravan, branch, incidentSite);
    }
}