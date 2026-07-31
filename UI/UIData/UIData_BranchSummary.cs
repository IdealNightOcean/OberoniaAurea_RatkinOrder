using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame.DataLibrary;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIData_BranchSummary : UIDataBase
{
    public Branch Branch { get; protected set; }
    public Map Map { get; protected set; }
    public override bool IsValid => Branch is not null && Map is not null;

    public BranchSquad Squad { get; protected set; }
    public string SquadName => Squad?.Name ?? KeyLibrary_Misc.ErrorTipWithColor;

    public string BaseSiteName { get; protected set; } = string.Empty;
    public float Distance { get; protected set; } = -1f;
    public float AffectedRange { get; protected set; } = -1f;
    public int MemberCeiling { get; protected set; }
    public int CommanderCeiling { get; protected set; }
    public int CrewCeiling => MemberCeiling + CommanderCeiling;

    public bool IsInAffectedRange => AffectedRange >= 0f && AffectedRange >= Distance;
    public int AllCrewCount => Squad?.AllCrewCountInt ?? 0;

    public UIData_BranchSummary(Branch branch, Map map)
    {
        this.Branch = branch;
        this.Map = map;
        this.Squad = branch?.Squad;
    }

    public void ResetData(Branch branch, Map map)
    {
        this.Branch = branch;
        this.Map = map;
        this.Squad = branch?.Squad;
        IsReady = false;
    }

    protected override void RefreshInner()
    {
        BaseSiteName = BranchUtility.GetBranchSiteName(this.Branch);

        Distance = this.Branch.DistanceTo(Map.Tile);
        AffectedRange = this.Branch.GetStatValue(BranchStatDefOf.OARO_AffectRadius);
        MemberCeiling = (int)this.Squad.MemberCeiling;
        CommanderCeiling = (int)this.Squad.CommanderCeiling;
    }

}
