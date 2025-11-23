using OberoniaAurea_Frame;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class DevWindow_BranchManager : DevWindowBase
{
    private readonly RatkinOrder ratkinOrder;
    private readonly BranchManager branchManager;
    private readonly IReadOnlyList<Branch> allBranches;
    private readonly string mobileBranchName;
    public DevWindow_BranchManager(RatkinOrder ratkinOrder) : base()
    {
        this.ratkinOrder = ratkinOrder;
        branchManager = ratkinOrder.BranchManager;
        optionalTitle = ratkinOrder.Name;

        allBranches = branchManager.AllBranches;

        if (branchManager.MobileBranches.Any())
        {
            mobileBranchName = string.Join(", ", branchManager.MobileBranches.Select(b => b.Name));
        }
        else
        {
            mobileBranchName = "None";
        }

    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect viewRect = inRect.ContractedBy(8f);
        viewRect.height = viewRectHeight;
        Listing_Standard listing_Rect = new(inRect, () => scrollPosition)
        {
            ColumnWidth = viewRect.width
        };
        Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
        listing_Rect.Begin(viewRect);

        if (listing_Rect.ButtonText("GoBack".Translate(), null, 0.8f))
        {
            Close();
            EndContents();
            ratkinOrder.OpenDevWindow();
            return;
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        listing_Rect.Label($"BranchCount: {allBranches.Count}");
        listing_Rect.Label($"InvitedBranchCreationsCount: {branchManager.InvitedBranchCreationsCount}");

        listing_Rect.Gap(6f);
        listing_Rect.Label("MobileBranch:");
        listing_Rect.SubLabel(mobileBranchName, 0.8f);

        listing_Rect.Gap(6f);
        if (listing_Rect.ButtonText("Honor Branches", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(GetAllHonorBranchesName()));
        }
        if (listing_Rect.ButtonText("Friendly Branches", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(GetAllFriendlyBranchesName()));
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        listing_Rect.Label($"NormalDemandFulfillCount: {branchManager.NormalDemandFulfillCount}");
        listing_Rect.Label($"CriticalDemandFulfillCount: {branchManager.CriticalDemandFulfillCount}");

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("All Branches");
        Text.Font = GameFont.Small;
        listing_Rect.Gap(6f);
        int selectIndex = -1;
        for (int i = 0; i < allBranches.Count; i++)
        {
            if (listing_Rect.ButtonText(allBranches[i].Name, null, 0.8f))
            {
                selectIndex = i;
            }
        }

        if (selectIndex >= 0)
        {
            Close();
            EndContents();
            allBranches[selectIndex].OpenDevWindow();
            return;
        }

        if (Event.current.type == EventType.Layout)
        {
            viewRectHeight = listing_Rect.MaxColumnHeightSeen + 50f;
        }
        EndContents();

        void EndContents()
        {
            listing_Rect.End();
            Widgets.EndScrollView();
        }
    }

    private string GetAllHonorBranchesName()
    {
        StringBuilder sb = new();
        int i = 0;
        foreach (Branch branch in branchManager.HonorBranches)
        {
            sb.AppendInNewLine($"{++i}. {branch.Name}");
        }

        if (sb.Length > 0)
        {
            return sb.ToString();
        }
        return "None";
    }

    private string GetAllFriendlyBranchesName()
    {
        StringBuilder sb = new();
        int i = 0;
        foreach (Branch branch in branchManager.FriendlyBranches)
        {
            sb.AppendInNewLine($"{++i}. {branch.Name}");
        }

        if (sb.Length > 0)
        {
            return sb.ToString();
        }
        return "None";
    }
}