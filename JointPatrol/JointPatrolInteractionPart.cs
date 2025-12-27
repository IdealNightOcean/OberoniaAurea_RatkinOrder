using System.Text;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public abstract class JointPatrolInteractionPart
{
    public Color partRecordColor = Color.white;
    public abstract void ApplyPart(JointPatrolInteractionDef def, JointBranchRecord record, StringBuilder effectExplain);
}