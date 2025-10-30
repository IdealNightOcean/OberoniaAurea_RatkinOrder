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
    /// <summary>
    /// 打开骑士团调试窗口
    /// </summary>
    [DebugAction(category: "OberoniaAurea",
                 name: "Dev-Win RatkinOrder",
                 displayPriority: 500,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void OpenOrderDevWindow()
    {
        RatkinOrderOptions((order) => order.OpenDevWindow());
    }

    /// <summary>
    /// 打开BranchManager调试窗口
    /// </summary>
    [DebugAction(category: "OberoniaAurea",
                 name: "Dev-Win BranchManager",
                 displayPriority: 490,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void OpenBranchManagerDevWindow()
    {
        RatkinOrderOptions((ratkinOrder) => ratkinOrder.BranchManager.OpenDevWindow());
    }

    /// <summary>
    /// 添加骑士团
    /// </summary>
    [DebugAction(category: "OberoniaAurea",
                 name: "Add a new RatkinOrder",
                 displayPriority: 480,
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
    [DebugAction(category: "OberoniaAurea",
                 name: "Remove a RatkinOrder",
                 displayPriority: 470,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void RemoveRatkinOrder()
    {
        RatkinOrderOptions((ratkinOrder) => RatkinOrderManager.RemoveRatkinOrder(ratkinOrder));
    }

    /// <summary>
    /// 全局骑士团交互管理器调试窗口
    /// </summary>
    [DebugAction(category: "OberoniaAurea",
                 name: "Dev-Win GlobalOrderInteraction",
                 displayPriority: 460,
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
    /// 触发骑士团交互
    /// </summary>
    [DebugAction(category: "OberoniaAurea",
                 name: "Trigger order interaction",
                 displayPriority: 450,
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
                                                         interactionDef.Worker.ApplyInteraction(order, map);
                                                     });
                interactionOptions.Add(orderDefOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(interactionOptions));
        }
    }

    /// <summary>
    /// 添加分部需求
    /// </summary>
    [DebugAction(category: "OberoniaAurea",
                 name: "Add new branch demand",
                 displayPriority: 440,
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
                                                           Log.Message($"Can add new demand for {branch.Name} now.");
                                                       }
                                                   });
                demandOptions.Add(demandOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(demandOptions));
        }
    }

    /// <summary>
    /// 添加分部合约
    /// </summary>
    [DebugAction(category: "OberoniaAurea",
                 name: "Add new branch contract",
                 displayPriority: 440,
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
    /// 添加常驻骑士
    /// </summary>
    [DebugAction(category: "OberoniaAurea",
                 name: "Add a new resident knight",
                 displayPriority: 430,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void ApplyNewResidentKnight()
    {
        RatkinOrderOptions(delegate (RatkinOrder ratkinOrder)
        {
            GlobalInteractionUtility.ApplyResidentKnight(ratkinOrder: ratkinOrder,
                                                              map: OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false));
        });
    }

    /// <summary>
    /// 触发善行任务（实际前置任务）
    /// </summary>
    [DebugAction(category: "OberoniaAurea",
                 name: "Trigger MercyQuest",
                 displayPriority: 430,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void TriggerMercyQuest()
    {
        List<DebugMenuOption> questOptions = [];
        foreach (QuestScriptDef scriptDef in OrderDefDataBase.MercyQuestsList)
        {
            DebugMenuOption orderOption = new(label: scriptDef.defName,
                                              mode: DebugMenuOptionMode.Action,
                                              method: () => GlobalInteractionUtility.TryTriggerMercyQuest(scriptDef));

            questOptions.Add(orderOption);
        }
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(questOptions));
    }

    /// <summary>
    /// 触发袭击
    /// </summary>
    [DebugAction(category: "OberoniaAurea",
                 name: "Trigger Branch Raid",
                 displayPriority: 420,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void TriggerBranchRaid()
    {
        OrderBranchOptions(SelectRaidLevel);

        void SelectRaidLevel(Branch branch)
        {
            List<DebugMenuOption> raidLevelOptions = [];
            foreach (BranchSupportUtility.SupportLevel level in EnumUtility.GetValues<BranchSupportUtility.SupportLevel>())
            {
                DebugMenuOption levelOption = new(label: level.ToString(),
                                                  mode: DebugMenuOptionMode.Action,
                                                  method: () => BranchSupportUtility.DoCombatSupport(branch, level, Find.CurrentMap));

                raidLevelOptions.Add(levelOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(raidLevelOptions));
        }
    }

    private static void RatkinOrderOptions(Action<RatkinOrder> orderAction)
    {
        List<DebugMenuOption> orderOptions = [];
        foreach (RatkinOrder ratkinOrder in RatkinOrderManager.AllRatkinOrders)
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
        foreach (RatkinOrder ratkinOrder in RatkinOrderManager.AllRatkinOrders)
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