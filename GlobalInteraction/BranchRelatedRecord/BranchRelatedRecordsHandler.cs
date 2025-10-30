using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 子类需在静态构造中调用GlobalInteractionManager.AddBRRHandlerResetAction注册重置方法
/// </summary>
public abstract class BranchRelatedRecordsHandler<T> : IExposable, IOnRatkinOrderRemoved, IOnBranchDestroyed where T : BranchRelatedRecord
{
    protected List<T> records = [];
    public IReadOnlyList<T> Records => records;

    public virtual void ExposeData()
    {
        Scribe_Collections.Look(ref records, "records", LookMode.Deep);
        if (records.RemoveAll(r => r is null || r.Branch is null) > 0)
        {
            Log.Error($"Some {typeof(T)} were null or invalided after loading and have been removed.");
        }
    }

    public void AddRecord(T record) => records.Add(record);
    public bool RemoveRecord(T record) => records.Remove(record);

    public virtual void Notify_RatkinOrderRemoved(RatkinOrder order) => records.RemoveAll(r => r is null || r.Branch is null || r.Branch.RatkinOrder == order);

    public virtual void Notify_BranchDestroyed(Branch branch) => records.RemoveAll(r => r is null || r.Branch is null || r.Branch == branch);

    public T GetFirstRecordOfBranch(Branch branch)
    {
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].Branch == branch)
            {
                return records[i];
            }
        }
        return null;
    }

    public IEnumerable<T> GetAllRecordsOfBranch(Branch branch)
    {
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].Branch == branch)
            {
                yield return records[i];
            }
        }
    }
}