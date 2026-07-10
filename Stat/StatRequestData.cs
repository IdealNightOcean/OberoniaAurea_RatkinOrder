namespace OberoniaAurea.RatkinOrder;

public class StatRequestData<TDef, UEntity> where TDef : OAROStatDefBase
{
    public UEntity Target { get; set; }
    public TDef StatDef { get; set; }
    public StatRequestData() { }

    public StatRequestData(UEntity target)
    {
        Target = target;
    }

    public StatRequestData(UEntity target, TDef statDef)
    {
        Target = target;
        StatDef = statDef;
    }
}
