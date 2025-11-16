using Verse;

namespace OberoniaAurea.RatkinOrder;

public partial class BranchStoresReserveHandler
{
    public abstract class ReserveRecord : IExposable
    {
        public abstract BranchConstructionDef Target { get; }

        public float CostRateReduce;

        public ReserveRecord() { }
        public ReserveRecord(float costRateReduce)
        {
            CostRateReduce = costRateReduce;
        }

        public static ReserveRecord GenrateNewRecord(BranchConstructionDef def, float costRateReduce = 0f)
        {
            if (def is BranchBuildingDef buildingDef)
            {
                return new BuildingReserveRecord(buildingDef, costRateReduce);
            }

            if (def is BranchFacilityDef facilityDef)
            {
                return new FacilityReserveRecord(facilityDef, costRateReduce);
            }

            Log.Error("");
            return null;
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref CostRateReduce, "CostRateReduce", 0f);
        }
    }

    public class BuildingReserveRecord : ReserveRecord
    {
        private BranchBuildingDef target;
        public override BranchConstructionDef Target => target;

        public BuildingReserveRecord() { }
        public BuildingReserveRecord(BranchBuildingDef def, float costRateReduce) : base(costRateReduce)
        {
            target = def;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref target, "target");
        }
    }

    public class FacilityReserveRecord : ReserveRecord
    {
        private BranchFacilityDef target;
        public override BranchConstructionDef Target => target;

        public FacilityReserveRecord() { }
        public FacilityReserveRecord(BranchFacilityDef def, float costRateReduce) : base(costRateReduce)
        {
            target = def;
        }
        public override string ToString() => $"{target.label} - CostRateReduce: {CostRateReduce}";
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref target, "target");
        }
    }
}