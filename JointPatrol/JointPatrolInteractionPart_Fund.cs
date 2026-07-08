using OberoniaAurea_Frame;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolInteractionPart_Fund : JointPatrolInteractionPart
{
    [MustTranslate] public string changeReason;
    public float change;

    public override void ApplyPart(JointPatrolInteractionDef def, JointBranchRecord record, StringBuilder effectExplain)
    {
        record.Branch.RatkinOrder.FundHandler.AdjustFundsImmediately(change, changeReason ?? def.label);
        effectExplain.AppendLine("OARO_ChangeOffset_Fund".Translate(change.ToStringPercentSigned("0.##").Named(KeyLibrary_FormatArgName.Offset)).Colorize(partRecordColor));
    }
}