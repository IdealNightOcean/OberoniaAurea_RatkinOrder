namespace OberoniaAurea.RatkinOrder.UI;

public class UIData_KnightVirtue : UIDataBase
{
    public ResidentKnight Knight { get; }
    public KnightVirtue Virtue { get; }

    public KnightVirtueDef VirtueDef => Virtue.Def;

    public override bool IsValid => Knight is not null && Virtue is not null;

    public UIData_KnightVirtue(ResidentKnight knight, KnightVirtue virtue)
    {
        this.Knight = knight;
        this.Virtue = virtue;
    }

    protected override void RefreshInner() { }
}
