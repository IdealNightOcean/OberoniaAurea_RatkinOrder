namespace OberoniaAurea.RatkinOrder.UI;

public class UIData_KnightVirtue : UIDataBase
{
    public ResidentKnight Knight { get; }
    public KnightVirtue Virtue { get; }

    public KnightVirtueDef VirtueDef => Virtue.Def;

    public UIData_KnightVirtue(ResidentKnight knight, KnightVirtue virtue)
    {
        this.Knight = knight;
        this.Virtue = virtue;
    }

    protected override UIDataState RefreshInner()
    {
        if (this.Knight is null || this.Virtue is null)
            return UIDataState.Empty;

        return UIDataState.Ready;
    }
}