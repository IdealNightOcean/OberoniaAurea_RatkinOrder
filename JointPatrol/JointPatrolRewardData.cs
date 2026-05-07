using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

using static OberoniaAurea.RatkinOrder.JointBranchRecord;
using static OberoniaAurea.RatkinOrder.JointPatrolManager;

public class JointPatrolRewardData
{
    public RatkinOrder RatkinOrder { get; }

    public float Fund { get; set; }
    public int Population { get; set; }
    public float PublicSecurity { get; set; }
    public float ParticipantPublicSecurity { get; set; }
    public float Reformation { get; set; }
    public int SacrificeCount { get; set; }
    public Dictionary<KnightChivalryDef, int> BranchMedals { get; set; }

    private List<KnightChivalryDef> completedChivalries;
    private List<KnightChivalryDef> failedChivalries;

    public IReadOnlyList<KnightChivalryDef> CompletedChivalries => completedChivalries;
    private IReadOnlyList<KnightChivalryDef> FailedChivalries => failedChivalries;

    public JointPatrolRewardData(RatkinOrder ratkinOrder)
    {
        RatkinOrder = ratkinOrder;
    }

    public void OnChivalryCompleted(KnightChivalryDef chivalry)
    {
        completedChivalries ??= [];
        completedChivalries.AddDistinct(chivalry);
    }

    public void OnChivalryFailed(KnightChivalryDef chivalry)
    {
        failedChivalries ??= [];
        failedChivalries.AddDistinct(chivalry);
    }

    public void AdjustBranchMedal(KnightChivalryDef medalChivalry, int count)
    {
        if (medalChivalry is null || medalChivalry.medal is null)
            return;

        BranchMedals ??= [];
        if (BranchMedals.TryGetValue(medalChivalry, out int preCount))
        {
            int newCount = preCount + count;
            if (newCount > 0)
            {
                BranchMedals[medalChivalry] = newCount;
            }
            else
            {
                BranchMedals.Remove(medalChivalry);
            }
        }
        else if (preCount > 0)
        {
            BranchMedals[medalChivalry] = count;
        }
    }

    public string ApplyReward(
        PatrolLevel patrolLevel,
        Dictionary<Branch, JointBranchRecord> participants,
        bool generateSummary)
    {
        float fund = 0f;
        float reformation = 0f;
        switch (patrolLevel)
        {
            case PatrolLevel.Kingdom:
                fund *= 2f;
                reformation *= 2f;
                break;
            case PatrolLevel.Border:
                fund *= 3f;
                reformation *= 3f;
                break;
            default:
                break;
        }

        int publicSecurityUpCount = 0;
        int publicSecurityDownCount = 0;
        try
        {
            RatkinOrder.FundHandler.AdjustFundsImmediately(fund, "OARO_Fund_JointPatrolCompletion".Translate());
            RatkinOrder.ReformationManager.ReformProgress += Reformation;

            foreach (Branch branch in RatkinOrder.BranchManager.AllBranches)
            {
                float publicSecurityChange = participants.ContainsKey(branch) ? ParticipantPublicSecurity + PublicSecurity : PublicSecurity;
                branch.PopulationHandler.AdjustPublicSecurity(publicSecurityChange);
                if (publicSecurityChange > 0f)
                {
                    publicSecurityUpCount++;
                }
                else if (publicSecurityChange < 0f)
                {
                    publicSecurityDownCount++;
                }
            }

            foreach ((Branch branch, JointBranchRecord record) in participants)
            {
                branch.PopulationHandler.Population += Population;
                branch.MedalHandler.AdjustMedal(record.FocusedTaskChivalry, 1);

                if (record.HasInteraction(PatrolInteractionType.Diplomacy))
                {
                    KnightChivalryDef medalChivalry = OrderDefDatabase.MedalChivalries.RandomElement();
                    branch.MedalHandler.AdjustMedal(medalChivalry, 1);
                }
            }
        }
        catch (Exception exception1)
        {
            ModUtility.LogExceptionError(exception1, "应用联巡结果", nameof(JointPatrolRewardData), nameof(ApplyReward), needStackTrace: true);
        }

        if (!generateSummary)
            return string.Empty;

        try
        {
            GrammarRequest grammarRequest = new()
            {
                Includes = { OARO_RulePackDefOf.OARO_JointPatrolCompletion }
            };
            grammarRequest.Rules.AddRange(ModUtility.RulesForRatkinOrder(KeyLibrary_FormatArgName.ORDER, RatkinOrder));
            grammarRequest.Rules.Add(new Rule_String("patrolLevel", $"OARO_JointPatrolLevel_{patrolLevel}".Translate()));
            grammarRequest.Rules.Add(new Rule_String("participantsCount", participants.Count.ToString()));

            grammarRequest.Constants.Add("sacrificeCount", SacrificeCount.ToString());
            grammarRequest.Rules.Add(new Rule_String("sacrificeCount", SacrificeCount.ToString()));

            grammarRequest.Rules.Add(new Rule_String("fundGain", fund.ToStringPercentSigned("0.##").Colorize(fund > 0f ? Color.green : ColorLibrary.RedReadable)));
            grammarRequest.Rules.Add(new Rule_String("reformationGain", reformation.ToStringWithSign("0.##").Colorize(reformation > 0f ? Color.green : ColorLibrary.RedReadable)));
            grammarRequest.Rules.Add(new Rule_String("publicSecurityUpCount", publicSecurityUpCount.ToString()));
            grammarRequest.Rules.Add(new Rule_String("publicSecurityDownCount", publicSecurityDownCount.ToString()));
            grammarRequest.Rules.Add(new Rule_String("totalPopulationGain", (Population * participants.Count).ToString()));

            grammarRequest.Constants.Add("succeedTaksTypeCount", CompletedChivalries.Count.ToString());
            grammarRequest.Constants.Add("failedTaksTypeCount", FailedChivalries.Count.ToString());
            grammarRequest.Rules.Add(new Rule_String("succeedTaksTypeCount", CompletedChivalries.Count.ToString()));
            grammarRequest.Rules.Add(new Rule_String("failedTaksTypeCount", FailedChivalries.Count.ToString()));

            grammarRequest.Rules.Add(new Rule_String("succeedTaksTypeNames", string.Join(", ", CompletedChivalries.Select(t => t.jointPatrol?.TaskLabelCap))));
            grammarRequest.Rules.Add(new Rule_String("failedTaksTypeNames", string.Join(", ", FailedChivalries.Select(t => t.jointPatrol?.TaskLabelCap))));
            return GrammarResolver.Resolve("r_text", grammarRequest);
        }
        catch (Exception exception2)
        {
            ModUtility.LogExceptionError(exception2, "生成联巡完成摘要", nameof(JointPatrolRewardData), nameof(ApplyReward), needStackTrace: true);
            return "ERROR (；′⌒`)".Colorize(ColorLibrary.RedReadable);
        }
    }
}
