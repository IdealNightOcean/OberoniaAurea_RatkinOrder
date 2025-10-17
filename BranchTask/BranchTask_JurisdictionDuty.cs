using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTask_JurisdictionDuty : BranchTask
{
    protected override void PostTaskEnd(Branch branch)
    {
        branch.Supply -= 0.5f;

        if (Rand.Chance(0.1f) && !branch.RatkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.AttackedOnTask))
        {
            branch.Supply = 0f;
            // squadStat.MemberCount -= (Rand.Range(0.1f, 0.75f) * squadStat.MemberCount);

            branch.RatkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.AttackedOnTask, cdTicks: 15 * 60000, shouldRemoveWhenExpired: true);
        }
    }
}