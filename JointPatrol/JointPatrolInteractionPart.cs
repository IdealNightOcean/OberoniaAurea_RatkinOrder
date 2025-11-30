using System.Text;

namespace OberoniaAurea.RatkinOrder;

public abstract class JointPatrolInteractionPart
{
    public abstract void ApplyPart(JointPatrolInteractionDef def, JointBranchRecord record, StringBuilder effectExplain);
}