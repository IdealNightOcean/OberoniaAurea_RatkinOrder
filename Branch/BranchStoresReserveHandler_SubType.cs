using Verse;

namespace OberoniaAurea.RatkinOrder;

public partial class BranchStoresReserveHandler
{
    public abstract class ReserveRecord : IExposable
    {
        protected float costRateReduce;

        public abstract BranchConstructionDef Target { get; }
        /// <summary>
        /// 花费减免（负数）
        /// </summary>
        public float CostRateReduce
        {
            get => costRateReduce;
            set => costRateReduce = value;
        }


        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref costRateReduce, nameof(costRateReduce), defaultValue: 0f);
        }
    }

    public class ReserveRecord<T> : ReserveRecord where T : BranchConstructionDef, new()
    {
        private T target;
        public override BranchConstructionDef Target => target;

        public ReserveRecord(T target)
        {
            this.target = target;
        }

        public ReserveRecord(T target, float costRateReduce)
        {
            this.target = target;
            this.costRateReduce = costRateReduce;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<T>(ref target, nameof(target));
        }
    }
}