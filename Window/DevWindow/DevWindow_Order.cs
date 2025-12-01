using OberoniaAurea_Frame;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class DevWindow_Order : DevWindowBase
{

    private readonly RatkinOrder ratkinOrder;

    public DevWindow_Order(RatkinOrder ratkinOrder) : base()
    {
        this.ratkinOrder = ratkinOrder;
        optionalTitle = ratkinOrder.Name;
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
            RatkinOrderManager.OpenDevWindow();
            return;
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label($"ID: {ratkinOrder.LoadID}");
        listing_Rect.Label($"名称: {ratkinOrder.Name}");
        listing_Rect.Label($"所属派系: {ratkinOrder.Faction.Name} ({ratkinOrder.Faction})");
        Text.Font = GameFont.Small;

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("认可丨关系丨推荐:");
        Text.Font = GameFont.Small;
        ratkinOrder.EsteemHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("资金:");
        Text.Font = GameFont.Small;
        ratkinOrder.FundHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("自新:");
        Text.Font = GameFont.Small;
        ratkinOrder.ReformationManager.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        if (listing_Rect.ButtonText("效果标志 EffectTags", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(ratkinOrder.EffectTags.GetDetailString()));
        }
        if (listing_Rect.ButtonText("修正 StatTransformers", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(ratkinOrder.TransformerHandler.GetDetailString()));
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("分部总览:");
        Text.Font = GameFont.Small;
        if (listing_Rect.ButtonText("Dev窗口 - 分部总览", null, 0.8f))
        {
            Close();
            EndContents();
            ratkinOrder.BranchManager.OpenDevWindow();
            return;
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("联合巡逻:");
        Text.Font = GameFont.Small;
        if (listing_Rect.ButtonText("Dev窗口 - 联巡总览", null, 0.8f))
        {
            Close();
            EndContents();
            ratkinOrder.JointPatrolManager.OpenDevWindow();
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
}