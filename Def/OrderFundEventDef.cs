using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderFundEventDef : Def
{
    public FloatRange changeRange = FloatRange.Zero;
    public bool immediately;
    public int durationDays;
    public bool OnceEvent => immediately || durationDays <= 1;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (immediately && durationDays > 0)
        {
            durationDays = 0;
            yield return "has both an immediately true flag and a positive durationDays value at the same time. Set durationDays to 0.";
        }
    }
}
