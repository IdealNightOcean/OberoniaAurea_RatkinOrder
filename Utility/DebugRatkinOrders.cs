using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Utility;

namespace OberoniaAurea.RatkinOrder;

public static class DebugRatkinOrders
{
    private const string category = "OberoniaAurea";

    /// <summary>
    /// 打开骑士团调试窗口
    /// </summary>
    [DebugAction(category: category,
                 name: "Dev窗口：骑士团",
                 displayPriority: 1000,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void OpenOrderDevWindow()
    {
        RatkinOrderOptions((order) => order.OpenDevWindow());
    }

    /// <summary>
    /// 打开BranchManager调试窗口
    /// </summary>
    [DebugAction(category: category,
                 name: "Dev窗口：骑士团分部",
                 displayPriority: 990,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void OpenBranchManagerWindow()
    {
        RatkinOrderOptions((ratkinOrder) => ratkinOrder.BranchManager.OpenDevWindow());
    }

    /// <summary>
    /// 打开JointPatrolManager调试窗口
    /// </summary>
    [DebugAction(category: category,
                 name: "Dev窗口：骑士团联巡",
                 displayPriority: 980,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void OpenJointPatrolDevWindow()
    {
        RatkinOrderOptions((ratkinOrder) => ratkinOrder.JointPatrolManager.OpenDevWindow());
    }


    /// <summary>
    /// 全局交互管理器调试窗口
    /// </summary>
    [DebugAction(category: category,
                 name: "Dev窗口： 全局交互管理",
                 displayPriority: 970,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void OpenOrderInteractionDevWindow()
    {
        if (GlobalInteractionManager.Instance is null)
        {
            Messages.Message("GlobalOrderInteractionManager is null", MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        GlobalInteractionManager.OpenDevWindow();
    }

    /// <summary>
    /// 添加骑士团
    /// </summary>
    [DebugAction(category: category,
                 name: "添加一个骑士团",
                 displayPriority: 960,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void AddNewRatkinOrder()
    {
        List<DebugMenuOption> factionOptions = [];
        foreach (Faction faction in Find.FactionManager.AllFactions)
        {
            if (!RatkinOrderGenerator.CanHaveNewRatkinOrder(faction))
            {
                continue;
            }
            DebugMenuOption orderOption = new(label: faction.Name,
                                              mode: DebugMenuOptionMode.Action,
                                              method: () => RatkinOrderDefSelect(faction));

            factionOptions.Add(orderOption);
        }
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(factionOptions));

        void RatkinOrderDefSelect(Faction faction)
        {
            List<DebugMenuOption> orderDefOptions = [];
            DebugMenuOption defaultOption = new(label: "default",
                                                mode: DebugMenuOptionMode.Action,
                                                method: delegate
                                                {
                                                    RatkinOrderGenerator.GenerateRatkinOrderForFaction(faction, ratkinOrderDef: null);
                                                });
            orderDefOptions.Add(defaultOption);

            foreach (RatkinOrderDef ratkinOrderDef in DefDatabase<RatkinOrderDef>.AllDefs)
            {
                DebugMenuOption orderDefOption = new(label: ratkinOrderDef.label,
                                                     mode: DebugMenuOptionMode.Action,
                                                     method:
                                                     delegate
                                                     {
                                                         RatkinOrderGenerator.GenerateRatkinOrderForFaction(faction, ratkinOrderDef);
                                                     });
                orderDefOptions.Add(orderDefOption);
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(orderDefOptions));
        }
    }

    /// <summary>
    /// 移除骑士团
    /// </summary>
    [DebugAction(category: category,
                 name: "移除一个骑士团",
                 displayPriority: 950,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void RemoveRatkinOrder()
    {
        RatkinOrderOptions((ratkinOrder) => RatkinOrderManager.Instance.RemoveRatkinOrder(ratkinOrder));
    }

    /// <summary>
    /// 开始分部联巡
    /// </summary>
    [DebugAction(category: category,
                 name: "开始分部联巡",
                 displayPriority: 940,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void StartJointPatrol()
    {
        RatkinOrderOptions(SelectRaidLevel);

        static void SelectRaidLevel(RatkinOrder ratkinOrder)
        {
            ratkinOrder.JointPatrolManager.TryStartPatrolPrep();
        }
    }

    /// <summary>
    /// 触发骑士团交互
    /// </summary>
    [DebugAction(category: category,
                 name: "触发骑士团交互",
                 displayPriority: 930,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void TriggerOrderInteraction()
    {
        RatkinOrderOptions((ratkinOrder) => SelectInteraction(ratkinOrder));

        void SelectInteraction(RatkinOrder order)
        {
            Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
            List<DebugMenuOption> interactionOptions = [];
            foreach (OrderInteractionDef interactionDef in DefDatabase<OrderInteractionDef>.AllDefs)
            {
                bool canNoramlTrigger = interactionDef.Worker.CanUseInteraction(order, map, resultOnly: true);
                string optLabel = canNoramlTrigger ? interactionDef.label : (interactionDef.label + " (NotNow)");
                DebugMenuOption orderDefOption = new(label: optLabel,
                                                     mode: DebugMenuOptionMode.Action,
                                                     method: delegate
                                                     {
                                                         interactionDef.Worker.TryApplyInteraction(order, map);
                                                     });
                interactionOptions.Add(orderDefOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(interactionOptions));
        }
    }

    /// <summary>
    /// 触发分部袭击
    /// </summary>
    [DebugAction(category: category,
                 name: "触发分部袭击",
                 displayPriority: 920,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void TriggerBranchCombatDeploy()
    {
        OrderBranchOptions(SelectRaidLevel);

        void SelectRaidLevel(Branch branch)
        {
            List<DebugMenuOption> raidLevelOptions = [];
            foreach (BranchSupportUtility.DeploymentLevel level in EnumUtility.GetValues<BranchSupportUtility.DeploymentLevel>())
            {
                DebugMenuOption levelOption = new(label: level.ToString(),
                                                  mode: DebugMenuOptionMode.Action,
                                                  method: () => BranchSupportUtility.DoCombatKnightSupport(branch, Find.CurrentMap, level, sendStandardLetter: true));

                raidLevelOptions.Add(levelOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(raidLevelOptions));
        }
    }

    /// <summary>
    /// 添加分部需求
    /// </summary>
    [DebugAction(category: category,
                 name: "添加分部需求",
                 displayPriority: 910,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void GenerateBranchDemand()
    {
        OrderBranchOptions(SelectDemandType);

        void SelectDemandType(Branch branch)
        {
            List<DebugMenuOption> demandTypeOptions = [];
            foreach (BranchDemand.DemandType demandType in Enum.GetValues(typeof(BranchDemand.DemandType)))
            {
                DebugMenuOption demandTypeOption = new(label: demandType.ToString(),
                                                       mode: DebugMenuOptionMode.Action,
                                                       method: delegate { SelectDemand(branch, demandType); });
                demandTypeOptions.Add(demandTypeOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(demandTypeOptions));
        }

        void SelectDemand(Branch branch, BranchDemand.DemandType branchDemandType)
        {
            List<DebugMenuOption> demandOptions = [];
            foreach (BranchDemandDef demandDef in DefDatabase<BranchDemandDef>.AllDefs.Where(d => d.demandType == branchDemandType))
            {
                string label = demandDef.label;
                bool canAdd = branch.DemandHandler.CanAddDemand(demandDef.IsCritical, ignoreCD: true, replaceCur: true);
                if (!canAdd)
                {
                    label += " [NotNow]";
                }

                DebugMenuOption demandOption = new(label: label,
                                                   mode: DebugMenuOptionMode.Action,
                                                   method: delegate
                                                   {
                                                       if (canAdd)
                                                       {
                                                           branch.DemandHandler.AddNewDemand(demandDef);
                                                       }
                                                       else
                                                       {
                                                           Messages.Message($"Can not add new demand for {branch.Name} now.", MessageTypeDefOf.RejectInput, historical: false);
                                                       }
                                                   });
                demandOptions.Add(demandOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(demandOptions));
        }
    }

    /// <summary>
    /// 添加分部人口需求（合约）
    /// </summary>
    [DebugAction(category: category,
                 name: " 添加分部人口需求（合约）",
                 displayPriority: 900,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void AddBranchContract()
    {
        OrderBranchOptions(SelectDemand);

        void SelectDemand(Branch branch)
        {
            List<DebugMenuOption> contractOptions = [];
            foreach (BranchContractDef contractDef in DefDatabase<BranchContractDef>.AllDefs)
            {
                DebugMenuOption demandOption = new(label: contractDef.defName,
                                                   mode: DebugMenuOptionMode.Action,
                                                   method: delegate
                                                   {
                                                       branch.PopulationHandler.TryAddContract(contractDef);
                                                   });
                contractOptions.Add(demandOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(contractOptions));
        }
    }

    /// <summary>
    /// 添加新建筑材料储备
    /// </summary>
    [DebugAction(category: category,
                 name: "添加新建筑材料储备",
                 displayPriority: 890,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void AddNewStoresReservee()
    {
        OrderBranchOptions(SelectBuildngDef);

        void SelectBuildngDef(Branch branch)
        {
            List<DebugMenuOption> buildngDefOptions = [];
            foreach (BranchBuildingDef buildingDef in DefDatabase<BranchBuildingDef>.AllDefsListForReading)
            {
                DebugMenuOption levelOption = new(label: buildingDef.label,
                                                  mode: DebugMenuOptionMode.Action,
                                                  method: () => branch.StoresReserveHandler.AddNewReserve(buildingDef));

                buildngDefOptions.Add(levelOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(buildngDefOptions));
        }
    }

    /// <summary>
    /// 添加分部建筑
    /// </summary>
    [DebugAction(category: category,
                 name: "添加分部建筑",
                 displayPriority: 870,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void AddBranchBuilding()
    {
        OrderBranchOptions(AddBuilding);

        void AddBuilding(Branch branch)
        {
            BranchBuildingHandler buildingHandler = branch.BuildingHandler;
            List<DebugMenuOption> buildngDefOptions = [];
            foreach (BranchBuildingDef buildingDef in DefDatabase<BranchBuildingDef>.AllDefsListForReading)
            {
                DebugMenuOption levelOption;
                if (buildingHandler.HasBuilding(buildingDef) || (buildingDef.isSpecial && buildingHandler.SpecialBuildingDef is not null))
                {
                    levelOption = new(label: buildingDef.label + "(No)",
                                      mode: DebugMenuOptionMode.Action,
                                      method: null);

                }
                else
                {
                    levelOption = new(label: buildingDef.label,
                                      mode: DebugMenuOptionMode.Action,
                                      method: () => buildingHandler.AddBuilding(buildingDef));
                }
                buildngDefOptions.Add(levelOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(buildngDefOptions));
        }
    }

    /// <summary>
    /// 生成骑士团推荐信
    /// </summary>
    [DebugAction(category: category,
                 name: "生成骑士团推荐信",
                 displayPriority: 870,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void SpawnOrderRecommendation()
    {
        TargetingParameters parms = new()
        {
            canTargetLocations = true,
            canTargetBuildings = false
        };

        Find.Targeter.BeginTargeting(parms, action: delegate (LocalTargetInfo t)
        {
            OrderRecommendation recommendation = RecommendationUtility.MakeRecommendationForPlayer(count: 1);
            GenPlace.TryPlaceThing(recommendation, t.Cell, Find.CurrentMap, ThingPlaceMode.Near);
            SpawnOrderRecommendation();
        });
    }

    /// <summary>
    /// 触发善行任务
    /// </summary>
    [DebugAction(category: category,
                 name: "触发善行任务 ",
                 displayPriority: 860,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void TriggerMercyQuest()
    {
        List<DebugMenuOption> mercyQuestOptions = [];

        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
        map ??= Find.CurrentMap;
        foreach (MercyQuestDef mercyQuestDef in DefDatabase<MercyQuestDef>.AllDefs)
        {
            DebugMenuOption orderOption = new(label: mercyQuestDef.label,
                                              mode: DebugMenuOptionMode.Action,
                                              method: () => MercyQuestHandler.TryTriggerMercyQuest(mercyQuestDef, map));

            mercyQuestOptions.Add(orderOption);
        }
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(mercyQuestOptions));
    }

    private static void RatkinOrderOptions(Action<RatkinOrder> orderAction)
    {
        List<DebugMenuOption> orderOptions = [];
        foreach (RatkinOrder ratkinOrder in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            DebugMenuOption orderOption = new(label: ratkinOrder.Name,
                                              mode: DebugMenuOptionMode.Action,
                                              method: () => orderAction(ratkinOrder));

            orderOptions.Add(orderOption);
        }
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(orderOptions));
    }

    private static void OrderBranchOptions(Action<Branch> branchAction)
    {
        List<DebugMenuOption> orderOptions = [];
        foreach (RatkinOrder ratkinOrder in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            DebugMenuOption orderOption = new(label: ratkinOrder.Name,
                                              mode: DebugMenuOptionMode.Action,
                                              method: delegate { SelectBranch(ratkinOrder); });

            orderOptions.Add(orderOption);
        }
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(orderOptions));

        void SelectBranch(RatkinOrder order)
        {
            List<DebugMenuOption> branchOptions = [];
            foreach (Branch branch in order.BranchManager.AllBranches)
            {
                DebugMenuOption branchOption = new(label: branch.Name,
                                                   mode: DebugMenuOptionMode.Action,
                                                   method: () => branchAction(branch));
                branchOptions.Add(branchOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(branchOptions));
        }
    }
}