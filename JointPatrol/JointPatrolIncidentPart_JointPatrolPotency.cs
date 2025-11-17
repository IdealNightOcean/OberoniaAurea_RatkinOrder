using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolIncidentPart_JointPatrolPotency : JointPatrolIncidentPart
{
    public float potencyFactorOffset;
    public float potencyOffsetOffset;
    public override void ApplyPart(JointPatrolIncidentDef def, JointBranchRecord record, StringBuilder effectExplain)
    {
        if (potencyFactorOffset != 0f)
        {
            record.PotencyFactor += potencyFactorOffset;
            effectExplain.AppendLine("OARO_ChangeOffset_JointPatrolPotencyFactor".Translate(potencyFactorOffset.ToStringPercentSigned("0.##")));
        }
        if (potencyOffsetOffset != 0f)
        {
            record.PotencyOffset += potencyOffsetOffset;
            effectExplain.AppendLine("OARO_ChangeOffset_JointPatrolPotencyOffset".Translate(potencyFactorOffset.ToStringPercentSigned("0.##")));
        }
    }
}