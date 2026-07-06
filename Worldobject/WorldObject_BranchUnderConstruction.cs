using RimWorld;
using RimWorld.Planet;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_BranchUnderConstruction : WorldObject
{
    private RatkinOrder ratkinOrder;

    private int completedTick;

    public override string Label => base.Label + $" ( {ratkinOrder?.Name} )";

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref ratkinOrder, "ratkinOrder");
        Scribe_Values.Look(ref completedTick, "completedTick", 0);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && !ratkinOrder.IsValid())
        {
            this.SafeDestroy();
        }
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new(base.GetInspectString());
        sb.AppendInNewLine(ratkinOrder?.Name);
        sb.AppendInNewLine("WaitTime".Translate((completedTick - Find.TickManager.TicksGame).ToStringTicksToPeriod()));
        return sb.ToString();
    }

    public void StartConstruction(RatkinOrder ratkinOrder, int duration)
    {
        this.ratkinOrder = ratkinOrder;
        completedTick = Find.TickManager.TicksGame + duration;
        SetFaction(ratkinOrder.Faction);
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (Find.TickManager.TicksGame >= completedTick)
        {
            Completed();
        }
    }

    private void Completed()
    {
        PlanetTile tile = Tile;
        this.SafeDestroy();

        if (!ratkinOrder.IsValid())
        {
            return;
        }

        WorldObject branchSite = WorldObjectMaker.MakeWorldObject(OARO_WorldObjectDefOf.OARO_WO_BranchSite);
        branchSite.Tile = tile;
        branchSite.SetFaction(ratkinOrder.Faction);
        Branch branch = Branch.GenerateBranchFor(ratkinOrder, branchSite, addToManager: true);
        if (branch.IsValid())
        {
            Find.WorldObjects.Add(branchSite);
            OrderLetterUtility.ReceiveLetter(
                label: "OARO_NewBranchConstructedLabel".Translate(),
                text: "OARO_NewBranchConstructedText".Translate(branch.RatkinOrder.NameColored.Named(OARO_KeyLibrary_FormatArgName.OrderName), branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName)),
                def: OrderLetterDefOf.OARO_OfficialLetter,
                relatedOrder: branch.RatkinOrder,
                relatedBranch: branch,
                sender: branch.Name,
                relatedLetterType: OrderLetter.RelatedLetterType.Positive);
        }
        else
        {
            branchSite.SafeDestroy();
        }
    }
}