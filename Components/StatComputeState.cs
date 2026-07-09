namespace OberoniaAurea.RatkinOrder;

public struct StatComputeState
{
    /// <summary>
    /// 当前值
    /// </summary>
    public float Value;
    /// <summary>
    /// 是否已收敛（即最终值）
    /// </summary>
    public bool IsConverged;

    public StatComputeState() { }
    public StatComputeState(float value)
    {
        Value = value;
        IsConverged = false;
    }
    public StatComputeState(float value, bool isConverged)
    {
        Value = value;
        IsConverged = isConverged;
    }
}
