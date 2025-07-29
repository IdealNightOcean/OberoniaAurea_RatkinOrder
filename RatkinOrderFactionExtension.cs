using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrderFactionExtension : DefModExtension
{
    public RatkinOrderDef ratkinOrderDef;

    public override IEnumerable<string> ConfigErrors()
    {
        if (ratkinOrderDef is null)
        {
            yield return "RatkinOrderFactionExtension has a null ratkinOrderDef.";
        }
    }
}