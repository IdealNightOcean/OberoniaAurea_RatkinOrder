using RimWorld;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchTask_JurisdictionDuty : BranchTask
{
    protected override void PostTaskEnd(Branch branch)
    {
        RatkinOrder ratkinOrder = branch.RatkinOrder;
        BranchTaskHandler.RadicalismDegree curRadicalismDegree = branch.TaskHandler.CurRadicalismDegree;

        bool attackedOnTask = false;
        StringBuilder endSB = new();

        float attackedChane = curRadicalismDegree switch
        {
            BranchTaskHandler.RadicalismDegree.StabilityFocused => 0.05f,
            BranchTaskHandler.RadicalismDegree.Standard => 0.1f,
            BranchTaskHandler.RadicalismDegree.Aggressive => 0.2f,
            _ => 0.1f,
        };
        if (Rand.Chance(attackedChane) && !branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.BeAttackedOnTask))
        {
            attackedOnTask = true;
            branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.BeAttackedOnTask, cdTicks: 30 * 60000, removeWhenExpired: true);

            branch.Supply = 0f;
            int memberLoss = (int)(Rand.Range(0.1f, 0.75f) * branch.Squad.MemberCount);
            branch.Squad.AdjustCrew(member: -memberLoss, commander: 0f);
            endSB.AppendLine("OARO_Task_AttackedOnTask".Translate(memberLoss.ToString()).Colorize(ColorLibrary.RedReadable));
            ResidentKnightsManager.Instance.Notify_SquadBeAttackedOnTask(branch.RatkinOrder, branch);
        }
        else
        {
            branch.Supply -= 0.5f;
            endSB.AppendLine("OAOR_Task_SupplyCost".Translate(0.5f.ToStringPercent("0.##")));
        }

        bool gainMedal = attackedOnTask || curRadicalismDegree switch
        {
            BranchTaskHandler.RadicalismDegree.StabilityFocused => Rand.Chance(0.3f),
            BranchTaskHandler.RadicalismDegree.Standard => Rand.Chance(0.5f),
            BranchTaskHandler.RadicalismDegree.Aggressive => Rand.Chance(0.7f),
            _ => Rand.Chance(0.5f),
        };
        if (gainMedal)
        {
            BranchTaskType focusedTaskType = branch.TaskHandler.FocusedTaskType;
            BranchMedalDef medalDef = DefDatabase<BranchMedalDef>.AllDefsListForReading.RandomElementByWeight(weightSelector: d => d.focusedTaskType == focusedTaskType ? 40f : 20f);
            branch.MedalHandler.AddMedal(medalDef, 1);
        }

        switch (TaskType)
        {
            case BranchTaskType.CrimeFighting or BranchTaskType.StabilityMaintenance:
                {
                    float fundGain = ratkinOrder.BranchManager.AllBranches.Count * 0.04f
                                   + branch.Potency * 0.01f;
                    fundGain *= curRadicalismDegree switch
                    {
                        BranchTaskHandler.RadicalismDegree.StabilityFocused => 0.75f,
                        BranchTaskHandler.RadicalismDegree.Standard => 1f,
                        BranchTaskHandler.RadicalismDegree.Aggressive => 1.33f,
                        _ => 1f
                    };
                    if (TaskType == branch.TaskHandler.FocusedTaskType)
                    {
                        fundGain *= 1.15f;
                    }
                    ratkinOrder.FundHandler.AdjustFundsImmediately(fundGain, "OARO_FundChange_BranchTask".Translate());

                    endSB.AppendLine();
                    endSB.AppendLine("OARO_Jurisdiction_FundGain".Translate(fundGain.ToStringPercentSigned("0.##")));
                    break;
                }
            case BranchTaskType.Assistance or BranchTaskType.Supervision:
                {
                    float processGain = branch.BranchManager.AllBranches.Count * 0.06f
                                      + branch.Potency * 0.01f;
                    processGain *= curRadicalismDegree switch
                    {
                        BranchTaskHandler.RadicalismDegree.StabilityFocused => 0.75f,
                        BranchTaskHandler.RadicalismDegree.Standard => 1f,
                        BranchTaskHandler.RadicalismDegree.Aggressive => 1.33f,
                        _ => 1f
                    };
                    if (TaskType == branch.TaskHandler.FocusedTaskType)
                    {
                        processGain *= 1.15f;
                    }
                    ratkinOrder.ReformationManager.ReformProgress += processGain;

                    endSB.AppendLine();
                    endSB.AppendLine("OARO_Jurisdiction_ReformProgressGain".Translate(processGain.ToStringPercentSigned("0.##")));
                    break;
                }
            default: break;
        }

        float securityGain = Rand.Range(0.08f, 0.16f);
        branch.PopulationHandler.PublicSecurity += securityGain;
        endSB.AppendLine();
        endSB.AppendLine("OARO_Jurisdiction_PublicSecGain".Translate(securityGain.ToStringPercentSigned("0.##")));

        List<Branch> nearbyBranches = BranchUtility.GetAllAffectedBranch(branch.Tile);
        if (!nearbyBranches.NullOrEmpty())
        {
            endSB.AppendLine("OARO_Jurisdiction_OtherPublicSecGain".Translate(0.02f.ToStringPercentSigned("0.##")));
            for (int i = 0; i < nearbyBranches.Count; i++)
            {
                nearbyBranches[i].PopulationHandler.PublicSecurity += 0.02f;
                endSB.AppendWithSeparator(nearbyBranches[i].Name, ", ");
            }
        }
    }
}