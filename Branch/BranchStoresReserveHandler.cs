using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStoresReserveHandler : IExposable, ITickHourOfDay
{
    [Unsaved] private readonly Branch branch;

    private static readonly float[] CostRateReduceArr = [0.02f, 0.01f, 0.005f, 0.0025f];
    private const int StoresReserveCeiling = 3;

    private List<ReserveRecord> storesReserves = new(StoresReserveCeiling);
    public IReadOnlyList<ReserveRecord> StoresReserves => storesReserves;

    public ReserveRecord PrimaryReserves => storesReserves.FirstOrFallback(null);
    public float PrimaryCostRateReduce => PrimaryReserves?.CostRateReduce ?? 0f;

    internal BranchStoresReserveHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref storesReserves, "storesReserves", LookMode.Deep);
    }

    internal void PostLoadInit()
    {
        if (storesReserves.RemoveAll(r => r.Target is null) > 0)
        {
            Log.Error($"Some Reserves of {branch} were null after loading and have been removed.");
        }
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        if (storesReserves.Count > 0)
        {
            for (int i = 0; i < storesReserves.Count; i++)
            {
                listing_Rect.SubLabel(storesReserves[i].ToString(), 0.8f);
            }
        }
        else
        {
            listing_Rect.SubLabel("None", 0.8f);
        }
    }

    public void SetPrimaryReserves(IStoresReserveDef def, bool inSpecialSlot = false)
    {
        if (storesReserves.Count == 0)
        {
            storesReserves.Add(ReserveRecord.GenrateNewRecord(def, inSpecialSlot));
        }

        if (storesReserves[0].Target == def)
        {
            storesReserves[0].InSpecialSlot = inSpecialSlot;
            return;
        }

        for (int i = 1; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].Target == def)
            {
                storesReserves[i].InSpecialSlot = inSpecialSlot;
                storesReserves.Swap(0, i);
                return;
            }
        }

        storesReserves.Insert(0, ReserveRecord.GenrateNewRecord(def, inSpecialSlot));
    }

    public void AddNewReserve(IStoresReserveDef def, bool inSpecialSlot = false)
    {
        if (def is null)
        {
            return;
        }

        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].Target == def)
            {
                return;
            }
        }

        storesReserves.Add(ReserveRecord.GenrateNewRecord(def, inSpecialSlot));
    }

    /// <summary>
    /// 应该只在分部无任何建设时执行
    /// </summary>
    public void TickHour(int hourOfDay)
    {
        if (hourOfDay == 5)
        {

            float maxReduce = 0.3f;// ThoroughPreparation ? 0.4f : 0.3f;
            float reduceMulti = 1f;// ThoroughPreparation ? 2f : 1f;
            for (int i = 0; i < storesReserves.Count; i++)
            {
                float costRateReduce = storesReserves[i].CostRateReduce - (CostRateReduceArr[Mathf.Min(i, 3)] * reduceMulti);
                storesReserves[i].CostRateReduce = Mathf.Min(costRateReduce, maxReduce);
            }
        }
        else if (hourOfDay == 17)
        {
            if (storesReserves.Count < StoresReserveCeiling && Rand.Chance(0.1f))
            {
                TryCreateNewReserves();
            }
        }
    }

    public void RemoveReserve(IStoresReserveDef def)
    {
        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].Target == def)
            {
                storesReserves.RemoveAt(i);
                break;
            }
        }
    }

    public void Notify_BranchConstructStarted(IStoresReserveDef def)
    {
        RemoveReserve(def);
    }

    public float GetReserveCostReduce(IStoresReserveDef def)
    {
        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].Target == def)
            {
                return storesReserves[i].CostRateReduce;
            }
        }
        return 0f;
    }

    private void TryCreateNewReserves()
    {
        BranchBuildingHandler buildingHandler = branch.BuildingHandler;

        if (!buildingHandler.IsNormalBuildingFullyCompleted && buildingHandler.HasUnusedNormalSlots)
        {
            if (TryAddBuildingReserve(isSpecial: false))
            {
                return;
            }
        }

        BranchFacilityHandler facilityHandler = branch.FacilityHandler;
        if (!facilityHandler.IsFacilityFullyCompleted)
        {
            (BranchFacilityDef fDef, BranchFacilityLevel fLevel) = facilityHandler.Facilities.MinBy(kv => kv.Value);
            if (fDef is not null && fLevel < BranchFacilityLevel.Excellent)
            {
                AddNewReserve(fDef);
                return;
            }
        }

        if (buildingHandler.SpecialBuilding is null)
        {
            TryAddBuildingReserve(isSpecial: true);
        }

        bool TryAddBuildingReserve(bool isSpecial)
        {
            List<BranchBuildingDef> allDefs = DefDatabase<BranchBuildingDef>.AllDefsListForReading;
            ConcurrentBag<BranchBuildingDef> potentialBuildings = [];
            ParallelOptions options = new() { MaxDegreeOfParallelism = 8 }; // 最多使用8个线程
            Parallel.For(0, allDefs.Count, options, (i, state) =>
            {
                if (potentialBuildings.Count >= 10)
                {
                    state.Stop();
                    return;
                }

                if (allDefs[i].isSpecial != isSpecial)
                {
                    return;
                }

                BranchBuildingConstructParameter constructParam = new()
                {
                    Branch = branch,
                    BuildingDef = allDefs[i],
                    InSpecialSlot = false
                };
                if (buildingHandler.CanConstructBuilding(constructParam, resultOnly: true))
                {
                    potentialBuildings.Add(allDefs[i]);
                    if (potentialBuildings.Count >= 10)
                    {
                        state.Stop();
                    }
                }
            });

            if (potentialBuildings.Count > 0)
            {
                BranchBuildingDef targetDef = potentialBuildings.RandomElement();
                AddNewReserve(targetDef, inSpecialSlot: false);
                potentialBuildings = null;
                return true;
            }
            return false;
        }
    }

    // *-----------------------------------------------------------------------* //
    //  相关类中类丨类中接口
    // *-----------------------------------------------------------------------* //

    /// <summary>
    /// 用于标记BranchFacilityDef和BranchBuildingDef
    /// </summary>
    public interface IStoresReserveDef;

    public abstract class ReserveRecord : IExposable
    {
        public abstract IStoresReserveDef Target { get; }
        protected bool inSpecialSlot;
        public virtual bool InSpecialSlot
        {
            get => inSpecialSlot;
            set => inSpecialSlot = value;
        }

        public float CostRateReduce;

        public ReserveRecord() { }
        public ReserveRecord(bool inSpecialSlot, float costRateReduce)
        {
            this.inSpecialSlot = inSpecialSlot;
            CostRateReduce = costRateReduce;
        }

        public static ReserveRecord GenrateNewRecord(IStoresReserveDef def, bool inSpecialSlot = false, float costRateReduce = 0f)
        {
            if (def is BranchBuildingDef buildingDef)
            {
                return new BuildingReserveRecord(buildingDef, inSpecialSlot, costRateReduce);
            }

            if (def is BranchFacilityDef facilityDef)
            {
                return new FacilityReserveRecord(facilityDef, inSpecialSlot, costRateReduce);
            }

            Log.Error("");
            return null;
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref CostRateReduce, "CostRateReduce", 0f);
            Scribe_Values.Look(ref inSpecialSlot, "inSpecialSlot", defaultValue: false);
        }
    }

    public class BuildingReserveRecord : ReserveRecord
    {
        private BranchBuildingDef target;
        public override IStoresReserveDef Target => target;

        public override bool InSpecialSlot
        {
            get => inSpecialSlot || target.isSpecial;
            set => inSpecialSlot = value;
        }

        public BuildingReserveRecord() { }
        public BuildingReserveRecord(BranchBuildingDef def, bool inSpecialSlot, float costRateReduce) : base(inSpecialSlot, costRateReduce)
        {
            target = def;
            InSpecialSlot = def.isSpecial || inSpecialSlot;
        }

        public override string ToString() => $"{target.label} - InSpecialSlot: {InSpecialSlot} - CostRateReduce: {CostRateReduce}";

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref target, "target");
        }
    }

    public class FacilityReserveRecord : ReserveRecord
    {
        private BranchFacilityDef target;
        public override IStoresReserveDef Target => target;

        public FacilityReserveRecord() { }
        public FacilityReserveRecord(BranchFacilityDef def, bool inSpecialSlot, float costRateReduce) : base(inSpecialSlot, costRateReduce)
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