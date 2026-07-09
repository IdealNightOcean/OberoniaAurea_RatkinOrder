using System;

namespace OberoniaAurea.RatkinOrder;

public class StatRequestData<TDef, UEntity> where TDef : OAROStatDefBase
{
    public UEntity Target { get; set; }
    public TDef StatDef { get; set; }
    public StatRequestData() { }

    public StatRequestData(UEntity target, TDef statDef)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        StatDef = statDef ?? throw new ArgumentNullException(nameof(statDef));
    }
}
