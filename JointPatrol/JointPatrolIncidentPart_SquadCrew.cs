using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolIncidentPart_SquadCrew : JointPatrolIncidentPart
{
    public int memberChange;
    public int commanderChange;
    public override void ApplyPart(JointPatrolIncidentDef def, JointBranchRecord record, StringBuilder effectExplain)
    {
        if (memberChange != 0)
        {
            record.Branch.Squad.MemberCount += memberChange;
            effectExplain.AppendLine("OARO_ChangeOffset_SquadMemberCount".Translate(memberChange.ToStringWithSign()));
        }
        if (commanderChange != 0)
        {
            record.Branch.Squad.CommanderCount += commanderChange;
            effectExplain.AppendLine("OARO_ChangeOffset_SquadCommanderCount".Translate(commanderChange.ToStringWithSign()));
        }
    }
}