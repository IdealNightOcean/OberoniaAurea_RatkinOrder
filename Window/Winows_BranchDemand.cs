using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Winows_BranchDemand : OrderWindowBase
{
    private enum TabType
    {
        All,
        Accepted,
        Friendly,
        Near
    }

    public override Vector2 InitialSize => new(1360f, 930f);


    public override void DoWindowContents(Rect inRect)
    {
        Rect mainRect = OARO_WindowUtility.CenterRect(inRect, 1339f, 908f);
        Rect mainInnerRect = mainRect.ContractedBy(3f);

        Rect reusedRect;
        Rect leftMainRect = new(mainInnerRect.x + 60f, mainInnerRect.y + 210f, 801f, 624f);
        reusedRect = new(leftMainRect.x, leftMainRect.y - 36f, 350f, 36f);


        reusedRect = new(leftMainRect.xMax - 140f, leftMainRect.y - 36f, 140f, 36f);


        Rect leftTextRect = new(leftMainRect.x + 8f, reusedRect.y - (12f + 48f), 450f, 48f);




        DrawLeftRect(leftMainRect);

    }

    public void DrawLeftRect(Rect inRect) { }
}


public class BranchDemandDrawer
{
    public BranchDemand demand;
    public Branch branch;
    public BranchSummaryUICache branchUICache;



}
