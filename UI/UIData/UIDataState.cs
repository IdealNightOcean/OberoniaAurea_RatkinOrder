namespace OberoniaAurea.RatkinOrder.UI;

/// <summary>
/// UI数据状态
/// </summary>
public enum UIDataState
{
    /// <summary>
    /// 0 -数据脏，需要重新刷新
    /// </summary>
    Dirty = 0,

    /// <summary>
    /// 1 - 检测无效，不可绘制
    /// </summary>
    Invalid = 1,

    /// <summary>
    /// 2 - 检测为空，可绘制空占位
    /// </summary>
    Empty = 2,

    /// <summary>
    /// 3 - 检测有效，正常绘制
    /// </summary>
    Ready = 3
}