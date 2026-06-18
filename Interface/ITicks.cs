namespace OberoniaAurea.RatkinOrder;

public interface ITickInterval
{
    /// <summary>
    /// 每tick调用一次
    /// </summary>
    void TickInterval(int delta);
}

public interface ITickHour
{
    /// <summary>
    /// 每2500 tick调用一次
    /// </summary>
    void TickHour();
}

public interface ITickHourOfDay
{
    /// <summary>
    /// 每2500 tick调用一次
    /// </summary>
    void TickHour(int hourOfDay);
}

public interface ITickHour<T>
{
    /// <summary>
    /// 每2500 tick调用一次
    /// </summary>
    void TickHour(T parent);
}

public interface ITickDay
{
    /// <summary>
    /// 每60000 tick调用一次
    /// </summary>
    void TickDay();
}
public interface ITickDay<T>
{
    /// <summary>
    /// 每60000 tick调用一次
    /// </summary>
    void TickDay(T parent);
}