using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class DebugRatkinOrders
{
    [DebugAction(category: "OberoniaAurea",
                 name: "Dev-Win RatkinOrder",
                 displayPriority: 500,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void OpenOrderDevWindow()
    {
        RatkinOrderOptions((order) => order.OpenDevWindow());
    }

    [DebugAction(category: "OberoniaAurea",
             name: "Dev-Win BranchManager",
             displayPriority: 490,
             actionType = DebugActionType.Action,
             allowedGameStates = AllowedGameStates.Playing)]
    private static void OpenBranchManagerDevWindow()
    {
        RatkinOrderOptions((order) => order.BranchManager.OpenDevWindow());
    }

    [DebugAction(category: "OberoniaAurea",
                 name: "Dev-Win OrderInteraction",
                 displayPriority: 480,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void OpenOrderInteractionDevWindow()
    {
        if (OrderInteractionHandler.Instance is null)
        {
            Messages.Message("OrderInteractionHandler is null", MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        OrderInteractionHandler.OpenDevWindow();
    }

    [DebugAction(category: "OberoniaAurea",
             name: "Add new branch demand",
             displayPriority: 470,
             actionType = DebugActionType.Action,
             allowedGameStates = AllowedGameStates.Playing)]
    private static void GenerateBranchDemand()
    {
        OrderBranchOptions(SelectDemandType);

        void SelectDemandType(Branch branch)
        {
            List<DebugMenuOption> demandTypeOptions = [];
            foreach (BranchDemandType demandType in Enum.GetValues(typeof(BranchDemandType)))
            {
                DebugMenuOption demandTypeOption = new(demandType.ToString(),
                                                       DebugMenuOptionMode.Action,
                                                       method: delegate { SelectDemand(branch, demandType); });
                demandTypeOptions.Add(demandTypeOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(demandTypeOptions));
        }

        void SelectDemand(Branch branch, BranchDemandType branchDemandType)
        {
            List<DebugMenuOption> demandOptions = [];
            foreach (BranchDemandDef demandDef in DefDatabase<BranchDemandDef>.AllDefs.Where(d => d.demandType == branchDemandType))
            {
                string label = demandDef.label;
                bool canAdd = branch.DemandHandler.CanAddDemand(demandDef.IsCriticalDemand, ignoreCD: true, replaceCur: true);
                if (!canAdd)
                {
                    label += " [NotNow]";
                }

                DebugMenuOption demandOption = new(label,
                                                  DebugMenuOptionMode.Action,
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

    [DebugAction(category: "OberoniaAurea",
             name: "Add a new resident knight",
             displayPriority: 460,
             actionType = DebugActionType.Action,
             allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void ApplyNewResidentKnight()
    {
        RatkinOrderOptions(delegate (RatkinOrder order)
        {
            OrderInteractionUtility.ApplyResidentKnight(
                ratkinOrder: order,
                map: MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false));
        });
    }

    private static void RatkinOrderOptions(Action<RatkinOrder> orderAction)
    {
        List<DebugMenuOption> orderOptions = [];
        foreach (RatkinOrder order in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            DebugMenuOption orderOption = new(order.Name,
                                              DebugMenuOptionMode.Action,
                                              method: () => orderAction(order));

            orderOptions.Add(orderOption);
        }
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(orderOptions));
    }

    private static void OrderBranchOptions(Action<Branch> branchAction)
    {
        List<DebugMenuOption> orderOptions = [];
        foreach (RatkinOrder order in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            DebugMenuOption orderOption = new(order.Name,
                                              DebugMenuOptionMode.Action,
                                              method: delegate { SelectBranch(order); });

            orderOptions.Add(orderOption);
        }
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(orderOptions));

        void SelectBranch(RatkinOrder order)
        {
            List<DebugMenuOption> branchOptions = [];
            foreach (Branch branch in order.BranchManager.AllBranches)
            {
                DebugMenuOption branchOption = new(branch.Name,
                                                   DebugMenuOptionMode.Action,
                                                   method: () => branchAction(branch));
                branchOptions.Add(branchOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(branchOptions));
        }
    }
}
