using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class DevWindow_Branch : DevWindowBase
{
    private readonly Branch branch;
    public DevWindow_Branch(Branch branch) : base()
    {
        this.branch = branch;
        optionalTitle = branch.Name;
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

        if (listing_Rect.ButtonText("Dev窗口 - 分部总览", null, 0.8f))
        {
            Close();
            EndContents();
            branch.BranchManager.OpenDevWindow();
            return;
        }
        if (listing_Rect.ButtonText("Dev窗口 - 骑士团总览", null, 0.8f))
        {
            Close();
            EndContents();
            branch.RatkinOrder.OpenDevWindow();
            return;
        }

        listing_Rect.Gap(12f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label($"ID: {branch.LoadID}");
        listing_Rect.Label($"名称: {branch.Name}");
        Text.Font = GameFont.Small;
        listing_Rect.Gap(12f);
        listing_Rect.Label("————————————————");
        listing_Rect.Label($"友好分部: {branch.IsBranchOfType(Branch.BranchType.Friendly)}");
        listing_Rect.Label($"荣誉分部: {branch.IsBranchOfType(Branch.BranchType.Honor)}");
        if (listing_Rect.ButtonText("设为友好分部", widthPct: 0.5f))
        {
            branch.SetFriendly(active: true);
        }

        listing_Rect.Gap(12f);
        listing_Rect.Label("————————————————");
        listing_Rect.Label($"状态 WorkStateDesc: {branch.CurWorkState}");
        listing_Rect.Label($"状态描述: {branch.CurWorkStateDesc}");
        listing_Rect.Label($"效能: {branch.Potency}");
        listing_Rect.Label($"补给: {branch.Supply.ToStringPercent("F2")}");
        if (listing_Rect.ButtonText("补给 +10%", widthPct: 0.5f))
        {
            branch.Supply += 0.1f;
        }
        if (listing_Rect.ButtonText("补给 -10%", widthPct: 0.5f))
        {
            branch.Supply -= 0.1f;
        }

        listing_Rect.Gap(6f);
        if (branch.BaseSite is not null)
        {
            if (listing_Rect.ButtonTextLabeled($"站点: {branch.BaseSite}", "Jump to"))
            {
                CameraJumper.TryJumpAndSelect(branch.BaseSite);
            }
        }
        if (listing_Rect.ButtonText("效果标志 EffectTags", null, 0.8f))
        {
            Find.WindowStack.Add(OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(branch.EffectTags.GetDetailString()));
        }
        if (listing_Rect.ButtonText("修正 StatTransformers", null, 0.8f))
        {
            Find.WindowStack.Add(OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(branch.TransformerHandler.GetDetailString()));
        }


        listing_Rect.Gap(12f);
        Text.Font = GameFont.Medium;
        listing_Rect.Label($"分队:");
        Text.Font = GameFont.Small;
        listing_Rect.Label("————————————————");
        branch.Squad.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(12f);
        Text.Font = GameFont.Medium;
        listing_Rect.Label("印记:");
        Text.Font = GameFont.Small;
        listing_Rect.Label("————————————————");
        branch.MedalHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(12f);
        Text.Font = GameFont.Medium;
        listing_Rect.Label("设施:");
        Text.Font = GameFont.Small;
        listing_Rect.Label("————————————————");
        branch.FacilityHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(12f);
        Text.Font = GameFont.Medium;
        listing_Rect.Label("建筑:");
        Text.Font = GameFont.Small;
        listing_Rect.Label("————————————————");
        branch.BuildingHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(12f);
        Text.Font = GameFont.Medium;
        listing_Rect.Label("人口管理:");
        Text.Font = GameFont.Small;
        listing_Rect.Label("————————————————");
        branch.PopulationHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(12f);
        Text.Font = GameFont.Medium;
        listing_Rect.Label("任务:");
        Text.Font = GameFont.Small;
        listing_Rect.Label("————————————————");
        branch.TaskHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(12f);
        Text.Font = GameFont.Medium;
        listing_Rect.Label("需求:");
        Text.Font = GameFont.Small;
        listing_Rect.Label("————————————————");
        branch.DemandHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(12f);
        Text.Font = GameFont.Medium;
        listing_Rect.Label("驻派:");
        Text.Font = GameFont.Small;
        listing_Rect.Label("————————————————");
        branch.ResidentHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(12f);
        Text.Font = GameFont.Medium;
        listing_Rect.Label("建材储备:");
        Text.Font = GameFont.Small;
        listing_Rect.Label("————————————————");
        branch.StoresReserveHandler.DrawDevWindow(listing_Rect);

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
}