using Verse;
using static OberoniaAurea.RatkinOrder.BranchMedalRecord;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuilding_MemorialExtension : DefModExtension
{
    public BranchMedalType medalType = BranchMedalType.None;
    public bool requireAllTypesOfMedals = false;
    public short medalCount = 1;
}