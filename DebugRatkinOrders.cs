using LudeonTK;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class DebugRatkinOrders
{
    [DebugAction(category: "OberoniaAurea",
                 name: "Dev-Win RatkinOrder",
                 displayPriority: 0,
                 actionType = DebugActionType.Action,
                 allowedGameStates = AllowedGameStates.Playing)]
    private static void OpenOrderDevWindow()
    {
        List<DebugMenuOption> menuOptions = [];
        foreach (RatkinOrder order in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            menuOptions.Add(new DebugMenuOption(order.Name, DebugMenuOptionMode.Action, order.OpenDevWindow));
        }

        Find.WindowStack.Add(new Dialog_DebugOptionListLister(menuOptions));
    }

    [DebugAction(category: "OberoniaAurea",
             name: "Add new branch demand",
             displayPriority: 0,
             actionType = DebugActionType.Action,
             allowedGameStates = AllowedGameStates.Playing)]
    private static void GenerateBranchDemand()
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
                                                   method: delegate { SelectDemandType(branch); });
                branchOptions.Add(branchOption);
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(branchOptions));
        }

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
}
