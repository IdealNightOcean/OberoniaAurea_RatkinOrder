using OberoniaAurea.RatkinOrder.Utility;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GetCriticalDemandBranchClique : QuestNode
{
    public SlateRef<PlanetTile?> centerTile;
    public SlateRef<int> maxCount = 8;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        if (!QuestGen.quest.TryGetCliquesManager(addPartIfMiss: false, out QuestPart_CliquesManager cliquesManager))
        {
            Log.Error($"[OARO] 从 {QuestGen.quest} 获取 {nameof(QuestPart_CliquesManager)} 失败。");
            return;
        }

        Branch demandBranch = cliquesManager.Branch;
        if (demandBranch.IsValid())
        {
            QuestClique demandBranchClique = new()
            {
                IsBribable = false,
                IsCommunicable = true,
                Potency = QuestClique.BranchPotencyToCliquePotency(demandBranch.Potency)
            };
            demandBranchClique.InitForBranch(demandBranch);
            cliquesManager.TryAddClique(demandBranchClique, defaultActive: true);
        }

        foreach (Branch branch in GetAvailableBranches(QuestGen.slate, demandBranch))
        {
            QuestClique branchClique = new()
            {
                IsBribable = false,
                IsCommunicable = true,
                Potency = QuestClique.BranchPotencyToCliquePotency(branch.Potency)
            };
            branchClique.InitForBranch(branch);
            cliquesManager.TryAddClique(branchClique);
        }
    }

    private IEnumerable<Branch> GetAvailableBranches(Slate slate, Branch demandBranch)
    {
        PlanetTile centerTile = this.centerTile.GetValue(slate) ?? slate.Get<Map>("map")?.Tile ?? PlanetTile.Invalid;
        if (!centerTile.Valid)
        {
            yield break;
        }

        int leftCount = Mathf.Max(1, maxCount.GetValue(slate));
        HashSet<Branch> addedDemandOrderBranch = [];
        RatkinOrder demandOrder = demandBranch?.RatkinOrder;

        //需求骑士团友好派系
        foreach (Branch branch in demandBranch.BranchManager.GetAllBranchesOfType(Branch.BranchType.Friendly))
        {
            if (!ValidateBranch(branch))
            {
                continue;
            }
            if (Rand.Chance(0.2f))
            {
                addedDemandOrderBranch.Add(branch);
                yield return branch;
                leftCount--;
                if (leftCount <= 0)
                {
                    yield break;
                }
            }
        }

        // 需求骑士团附近派系
        if (demandOrder.IsValid())
        {
            foreach (Branch branch in demandOrder.GetAllAffectedBranchForOrder(centerTile, ValidateBranch))
            {
                if (Rand.Chance(0.1f) && !addedDemandOrderBranch.Contains(branch))
                {
                    yield return branch;
                    leftCount--;
                    if (leftCount <= 0)
                    {
                        yield break;
                    }
                }
            }
        }

        // 其它骑士团附近派系
        foreach (RatkinOrder ratkinOrder in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            if (ratkinOrder == demandOrder)
            {
                continue;
            }
            foreach (Branch branch in demandOrder.GetAllAffectedBranchForOrder(centerTile, ValidateBranch))
            {
                if (Rand.Chance(0.03f))
                {
                    yield return branch;
                    leftCount--;
                    if (leftCount <= 0)
                    {
                        yield break;
                    }
                }
            }
        }

        bool ValidateBranch(Branch b)
        {
            return b != demandBranch && b.DemandHandler.CriticalDemand is null && !b.TaskHandler.HasTask;
        }
    }
}