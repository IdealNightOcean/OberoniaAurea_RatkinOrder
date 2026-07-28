using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIData_SquadSummary : UIDataBase
{
    public Branch Branch { get; protected set; }
    public Map Map { get; protected set; }
    public BranchSquad Squad { get; protected set; }

    public string SquadName => Squad.Name;
    public string BaseSiteName { get; protected set; } = string.Empty;
    public float Distance { get; protected set; } = -1f;
    public float AffectedRange { get; protected set; } = -1f;
    public int MemberCeiling { get; protected set; }
    public int CommanderCeiling { get; protected set; }
    public int CrewCeiling => MemberCeiling + CommanderCeiling;

    public bool IsInAffectedRange => AffectedRange >= Distance;
    public int AllCrewCount => Branch?.Squad.AllCrewCountInt ?? 0;

    public UIData_SquadSummary(Branch branch, Map map)
    {
        this.Branch = branch;
        this.Map = map;
        this.Squad = branch.Squad;
    }

    public void ResetData(Branch branch, Map map)
    {
        this.Branch = branch;
        this.Map = map;
        this.Squad = branch.Squad;
        IsReady = false;
    }

    protected override void RefreshInner()
    {
        BaseSiteName = BranchUtility.GetBranchSiteName(Branch);

        Distance = Branch.DistanceTo(Map.Tile);
        AffectedRange = Branch.GetStatValue(BranchStatDefOf.OARO_AffectRadius);
        MemberCeiling = (int)Branch.Squad.MemberCeiling;
        CommanderCeiling = (int)Branch.Squad.CommanderCeiling;
    }

}
