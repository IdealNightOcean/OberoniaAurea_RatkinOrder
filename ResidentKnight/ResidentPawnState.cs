namespace OberoniaAurea.RatkinOrder;

public enum ResidentPawnState
{
    /// <summary>
    /// 正常状态
    /// </summary>
    Normal,
    /// <summary>
    /// 已失效 / 无法行动
    /// </summary>
    Disabled,
    /// <summary>
    /// 等待移除（队列中）
    /// </summary>
    PendingRemoval,
    /// <summary>
    /// 等待转为殖民者
    /// </summary>
    PendingConvertToColonist,
    /// <summary>
    /// 准备退休
    /// </summary>
    ReadyResignation,
    /// <summary>
    /// 可立即移除
    /// </summary>
    ForceRemove
}
