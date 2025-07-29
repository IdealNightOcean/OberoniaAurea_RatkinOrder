using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderFundEvent : IExposable
{
    public OrderFundEventDef def; // 可为null
    public float todayChange;
    public int daysLeft;

    public OrderFundEvent() { }

    public OrderFundEvent(OrderFundEventDef def)
    {
        this.def = def;
        daysLeft = def.durationDays;
        todayChange = def.changeRange.RandomInRange;
    }

    public OrderFundEvent(float change, int durationDays)
    {
        daysLeft = durationDays;
        todayChange = change;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref todayChange, "todayChange", 0f);
        Scribe_Values.Look(ref daysLeft, "daysLeft", 1);
    }

    public void DayPassed()
    {
        daysLeft--;
        todayChange = def?.changeRange.RandomInRange ?? todayChange;
    }

    public override string ToString()
    {
        return $"todayChange: {todayChange}, daysLeft: {daysLeft}";
    }
}