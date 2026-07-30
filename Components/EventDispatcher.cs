using OberoniaAurea.RatkinOrder.Utility;
using System;
using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 一个简单事件分发器
/// 用于注册、注销和触发事件
/// </summary>
/// <remarks>
/// <para>- 该分发器单个事件调用出现异常时不会中断调用链</para>
/// <para>- 线程不安全</para>
/// </remarks>
public class EventDispatcher<TDelegate> where TDelegate : Delegate
{
    private readonly List<TDelegate> handlers = [];

    public bool Register(TDelegate handler)
    {
        if (handler is null)
            return false;
        if (handlers.Contains(handler))
            return false;

        handlers.Add(handler);
        return true;
    }

    public bool Deregister(TDelegate handler) => handlers.Remove(handler);

    /// <summary>
    /// 触发事件，遍历调用所有已注册的处理器
    /// </summary>
    /// <remarks>
    /// <para>- 外部调用者可以在调用过程中被修改内部委托列表，此时会导致异常</para>
    /// <para>- 线程不安全</para>
    /// </remarks>
    public void Raise(Action<TDelegate> invoker)
    {
        if (invoker is null || handlers.Count == 0)
            return;

        foreach (TDelegate handler in handlers)
        {
            try
            {
                invoker(handler);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: "事件触发异常",
                    typeName: nameof(EventDispatcher<TDelegate>),
                    methodName: nameof(Raise),
                    needStackTrace: true);
            }
        }
    }

    /// <summary>
    /// 触发事件，通过快照遍历调用所有已注册的处理器，允许在调用过程中修改处理器列表
    /// </summary>
    /// <remarks>
    /// <para>- 线程不安全</para>
    /// </remarks>
    public void RaiseSafe(Action<TDelegate> invoker)
    {
        if (invoker is null || handlers.Count == 0)
            return;

        TDelegate[] snapshot = [.. handlers];
        foreach (TDelegate handler in snapshot)
        {
            try
            {
                invoker(handler);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: "事件触发异常",
                    typeName: nameof(EventDispatcher<TDelegate>),
                    methodName: nameof(Raise),
                    needStackTrace: true);
            }
        }
    }
}