using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class RatkinOrderGenerator
{
    public static void StartNewGame()
    {
        foreach (Faction faction in Find.FactionManager.AllFactionsVisible)
        {
            try
            {
                if (CanHaveRatkinOrder(faction))
                {
                    RatkinOrderDef ratkinOrderDef = faction.def.GetModExtension<RatkinOrderFactionExtension>()?.ratkinOrderDef;
                    if (ratkinOrderDef is null)
                        continue;

                    GenerateRatkinOrderForFaction(faction, ratkinOrderDef);
                }
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"为派系 {faction.loadID} 生成 RatkinOrder",
                    typeName: nameof(RatkinOrderGenerator),
                    methodName: nameof(StartNewGame),
                    needStackTrace: true);
                continue;
            }
        }
    }

    public static bool CanHaveRatkinOrder(Faction faction)
    {
        if (faction is null || faction.temporary || faction.defeated)
        {
            return false;
        }

        return true;
    }

    public static bool CanHaveNewRatkinOrder(Faction faction)
    {
        if (faction is null || faction.temporary || faction.defeated)
        {
            return false;
        }
        return !RatkinOrderManager.Instance.FactionHasRatkinOrder(faction);
    }

    public static bool TryGenerateNewRatkinOrderForFaction(Faction faction, out RatkinOrder newOrder)
    {
        if (CanHaveNewRatkinOrder(faction))
        {
            newOrder = GenerateRatkinOrderForFaction(faction);
            return true;
        }
        else
        {
            newOrder = null;
            return false;
        }
    }

    public static RatkinOrder GenerateRatkinOrderForFaction(Faction faction, RatkinOrderDef ratkinOrderDef = null)
    {
        RatkinOrder ratkinOrder = null;
        try
        {
            ratkinOrderDef ??= faction.def.GetModExtension<RatkinOrderFactionExtension>().ratkinOrderDef;
            if (ratkinOrderDef is null)
            {
                Log.Error("[OARO] 尝试为 faction_" + faction.loadID + " 创建骑士团，但该阵营没有 RatkinOrderDef。");
                return null;
            }
            ratkinOrder = new RatkinOrder(ratkinOrderDef, faction)
            {
                Name = GenerateRatkinOrderName(ratkinOrderDef)
            };
            ratkinOrder.PostGenerated();
        }
        catch (Exception ex1)
        {
            ModUtility.LogExceptionError(ex1,
                errorDesc: $"为派系：{faction.Name} 生成 RatkinOrder",
                typeName: nameof(RatkinOrderGenerator),
                methodName: nameof(GenerateRatkinOrderForFaction),
                needStackTrace: true);
            return null;
        }

        try
        {
            InitBranchForNewOrder(ratkinOrder);
        }
        catch (Exception ex2)
        {
            ModUtility.LogExceptionError(ex2,
                errorDesc: $"为派系：{faction.Name} 初始化 RatkinOrder ",
                typeName: nameof(RatkinOrderGenerator),
                methodName: nameof(GenerateRatkinOrderForFaction),
                needStackTrace: true);
            return null;
        }

        RatkinOrderManager.Instance.AddRatkinOrder(ratkinOrder);
        return ratkinOrder;
    }

    private static bool InitBranchForNewOrder(RatkinOrder ratkinOrder)
    {
        if (!ratkinOrder.IsValid() || ratkinOrder.Faction is null)
        {
            return false;
        }

        bool atLeastOneSite = false;
        foreach (Settlement settlement in Find.WorldObjects.Settlements.Where(s => s.Faction == ratkinOrder.Faction))
        {
            if (Rand.Chance(0.4f))
            {
                continue;
            }
            try
            {
                if (Branch.GenerateBranchFor(ratkinOrder, settlement, addToManager: true) is not null)
                {
                    atLeastOneSite = true;
                }
            }
            catch (Exception ex1)
            {
                ModUtility.LogExceptionError(ex1,
                    errorDesc: $"为 {ratkinOrder} 在 {settlement} 生成新分部失败",
                    typeName: nameof(RatkinOrderGenerator),
                    methodName: nameof(InitBranchForNewOrder),
                    needStackTrace: true);
            }
        }

        if (!atLeastOneSite)
        {
            Settlement settlement = Find.WorldObjects.Settlements.Where(s => s.Faction == ratkinOrder.Faction).RandomElement();
            try
            {
                if (Branch.GenerateBranchFor(ratkinOrder, settlement, addToManager: true) is not null)
                {
                    atLeastOneSite = true;
                }
            }
            catch (Exception ex1)
            {
                ModUtility.LogExceptionError(ex1,
                    errorDesc: $"为 {ratkinOrder} 在 {settlement} 生成新分部失败",
                    typeName: nameof(RatkinOrderGenerator),
                    methodName: nameof(InitBranchForNewOrder),
                    needStackTrace: true);
            }
        }

        BranchManager branchManager = ratkinOrder.BranchManager;

        /*
         * 初始化骑士团荣誉分部
         */
        List<BranchBuildingDef> honorBuildingDefs = DefDatabase<BranchBuildingDef>.AllDefs.Where(b => b.IsHonorSymbol).ToList();
        foreach (Branch branch in branchManager.AllBranches)
        {
            if (Rand.Chance(0.92f))
            {
                continue;
            }
            BranchBuildingDef honorBuildingDef = null;
            try
            {
                honorBuildingDef = honorBuildingDefs.RandomElement();
                branch.BuildingHandler.AddBuilding(honorBuildingDef);
            }
            catch (Exception ex2)
            {
                ModUtility.LogExceptionError(ex2,
                    errorDesc: $"添加荣誉建筑 ({honorBuildingDef}) 到 {branch} 失败",
                    typeName: nameof(RatkinOrderGenerator),
                    methodName: nameof(InitBranchForNewOrder),
                    needStackTrace: true);
            }
        }

        /*
         * 保证至少一个荣誉分部
         */
        if (!branchManager.GetAllBranchesOfType(Branch.BranchType.Honor).Any())
        {
            Branch branch = branchManager.AllBranches.RandomElement();
            if (branch is not null)
            {
                BranchBuildingDef honorBuildingDef = null;
                try
                {
                    honorBuildingDef = honorBuildingDefs.RandomElement();
                    branch.BuildingHandler.AddBuilding(honorBuildingDef);
                }
                catch (Exception ex2)
                {
                    ModUtility.LogExceptionError(ex2,
                    errorDesc: $"添加荣誉建筑 ({honorBuildingDef}) 到 {branch} 失败",
                    typeName: nameof(RatkinOrderGenerator),
                    methodName: nameof(InitBranchForNewOrder),
                    needStackTrace: true);
                }
            }
        }

        /*
         * 初始化骑士团关注分部
         */
        branchManager.ChangeFollowedBranches(branchManager.AllBranches.TakeRandomElements(3));

        return atLeastOneSite;
    }


    public static string GenerateRatkinOrderName(RatkinOrderDef def)
    {
        if (!string.IsNullOrEmpty(def.fixedName))
        {
            return def.fixedName;
        }

        return NameGenerator.GenerateName(def.nameMaker, RatkinOrderManager.Instance.AllRatkinOrders.Select(o => o.Name));
    }

}
