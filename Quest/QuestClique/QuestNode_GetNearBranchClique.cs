using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GetCriticalDemandBranchClique : QuestNode
{
    public SlateRef<PlanetTile?> centerTile;
    public SlateRef<int> baseCount = 3;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        if (!QuestPart_CliquesManager.TryGetCliquesManager(QuestGen.quest, addPartIfMiss: false, out QuestPart_CliquesManager cliquesManager))
        {
            Log.Error($"Failed to get QuestPart_CliquesManager from {QuestGen.quest}.");
            return;
        }

        Branch demandBranch = cliquesManager.Branch;
        if (demandBranch is not null)
        {
            QuestClique demandBranchClique = new()
            {
                IsActivatable = true,
                IsBribable = false,
                IsCommunicable = true
            };
            demandBranchClique.InitForBranch(demandBranch);
            cliquesManager.TryAddClique(demandBranchClique, defaultActive: true);
        }


        foreach (Branch branch in GetAvailableBranches(QuestGen.slate, demandBranch))
        {
            QuestClique branchClique = new()
            {
                IsActivatable = true,
                IsBribable = false,
                IsCommunicable = true
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

        int leftCount = Mathf.Max(1, baseCount.GetValue(slate));
        HashSet<Branch> addedDemandOrderBranch = [];
        RatkinOrder demandOrder = demandBranch?.RatkinOrder;

        // 需求骑士团附近派系
        if (demandOrder is not null)
        {
            foreach (Branch branch in demandOrder.GetAllAffectedBranchForOrder(centerTile, ValidateBranch))
            {
                yield return branch;
                addedDemandOrderBranch.Add(branch);
                leftCount--;
                if (leftCount <= 0)
                {
                    yield break;
                }
            }
        }

        // 其它骑士团附近派系
        foreach (RatkinOrder ratkinOrder in RatkinOrderManager.AllRatkinOrders)
        {
            if (ratkinOrder == demandOrder)
            {
                continue;
            }
            foreach (Branch branch in demandOrder.GetAllAffectedBranchForOrder(centerTile, ValidateBranch))
            {
                yield return branch;
                leftCount--;
                if (leftCount <= 0)
                {
                    yield break;
                }
            }
        }

        //需求骑士团友好派系
        if (demandBranch is not null)
        {
            foreach (Branch branch in demandBranch.BranchManager.FriendlyBranches)
            {
                if (!ValidateBranch(branch) || addedDemandOrderBranch.Contains(branch))
                {
                    continue;
                }

                yield return branch;
                leftCount--;
                if (leftCount <= 0)
                {
                    yield break;
                }
            }
        }

        bool ValidateBranch(Branch b)
        {
            return b != demandBranch && b.DemandHandler.CriticalDemand is null && !b.TaskHandler.HasTask;
        }
    }
}