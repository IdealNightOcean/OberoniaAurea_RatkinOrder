
namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 常驻骑士移除原因
/// </summary>
public enum ResidentKnightRemovalReason
{
    /// <summary>
    /// 未知原因
    /// </summary>
    Unknown,
    /// <summary>
    /// 无效记录
    /// </summary>
    Invalid,
    /// <summary>
    /// 雇佣期满
    /// </summary>
    Overtime,
    /// <summary>
    /// 玩家主动移除
    /// </summary>
    Player,
    /// <summary>
    /// 失效并超出宽限期
    /// </summary>
    Overdue,
    /// <summary>
    /// 所属骑士团被销毁
    /// </summary>
    ConvertToColonist,
}
