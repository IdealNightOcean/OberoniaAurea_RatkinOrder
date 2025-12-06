using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderFundEvent : IExposable
{
    public OrderFundEventDef Def; // 可为null
    public float TodayChange;
    public int DaysLeft;

    public OrderFundEvent() { }

    public OrderFundEvent(OrderFundEventDef def)
    {
        Def = def;
        DaysLeft = def.durationDays;
        TodayChange = def.changeRange.RandomInRange;
    }

    public OrderFundEvent(float change, int durationDays)
    {
        DaysLeft = durationDays;
        TodayChange = change;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref Def, nameof(Def));
        Scribe_Values.Look(ref TodayChange, nameof(TodayChange), 0f);
        Scribe_Values.Look(ref DaysLeft, nameof(DaysLeft), 1);
    }

    public void DayPassed()
    {
        DaysLeft--;
        TodayChange = Def?.changeRange.RandomInRange ?? TodayChange;
    }

    public override string ToString()
    {
        return $"todayChange: {TodayChange}, daysLeft: {DaysLeft}";
    }
}