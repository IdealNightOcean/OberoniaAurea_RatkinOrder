using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolInteractionPart_ReformationProgress : JointPatrolInteractionPart
{
    public float change;

    public override void ApplyPart(JointPatrolInteractionDef def, JointBranchRecord record, StringBuilder effectExplain)
    {
        record.Branch.RatkinOrder.ReformationManager.ReformProgress += change;
        effectExplain.AppendLine("OARO_ChangeOffset_ReformProgress".Translate(change.ToStringPercentSigned("0.##")).Colorize(partRecordColor));
    }
}
