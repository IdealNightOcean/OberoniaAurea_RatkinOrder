using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolInteractionPart_PublicSecurity : JointPatrolInteractionPart
{
    public float change;

    public override void ApplyPart(JointPatrolInteractionDef def, JointBranchRecord record, StringBuilder effectExplain)
    {
        record.Branch.PopulationHandler.PublicSecurity += change;
        effectExplain.AppendLine("OARO_ChangeOffset_PublicSecurity".Translate(change.ToStringPercentSigned("0.##")));
    }
}