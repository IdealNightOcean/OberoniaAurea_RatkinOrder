using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolInteractionPart_SquadCrew : JointPatrolInteractionPart
{
    public int memberChange;
    public int commanderChange;
    public override void ApplyPart(JointPatrolInteractionDef def, JointBranchRecord record, StringBuilder effectExplain)
    {
        if (memberChange != 0)
        {
            record.Branch.Squad.AdjustCrew(member: memberChange, commander: 0f);
            effectExplain.AppendLine("OARO_ChangeOffset_SquadMemberCount".Translate(memberChange.ToStringWithSign()));
        }
        if (commanderChange != 0)
        {
            record.Branch.Squad.AdjustCrew(member: 0f, commander: commanderChange);
            effectExplain.AppendLine("OARO_ChangeOffset_SquadCommanderCount".Translate(commanderChange.ToStringWithSign()));
        }
    }
}