using RimWorld;
using Verse;
using static OberoniaAurea.RatkinOrder.EsteemHandler;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrder : IExposable, ILoadReferenceable, IPostLoadInit, ITickHour
{
    public int loadID = -1;

    private Def def;
    public Def Def => def;

    private Faction faction;
    public Faction Faction => faction;

    private string name;
    public string Name => name;

    private EsteemHandler esteemHandler;
    public EsteemHandler EsteemHandler => esteemHandler;
    public float Esteem => esteemHandler.Esteem;
    public float CurRecommendation => esteemHandler.CurRecommendation;
    public RelationshipKind Relationship => esteemHandler.Relationship;


    private FundHandler fundHandler;
    public FundHandler FundHandler => fundHandler;
    public float Funds => fundHandler.Funds;


    private ReformationManager reformationManager;
    public ReformationManager ReformationManager => reformationManager;
    public float ReformProgress => reformationManager.ReformProgress;


    private BranchManager branchManager;
    public BranchManager BranchManager => branchManager;


    private SquadManager squadManager;
    public SquadManager SquadManager => squadManager;

    private RatkinOrder() { }
    public RatkinOrder(Def def, Faction faction)
    {
        this.def = def;
        this.faction = faction;

        EnsureComponentsInit();

        loadID = UniqueIDManager.Instance.GetUniqueID("RatkinOrder");
    }

    public void TickHour()
    {
        branchManager.TickHour();
    }

    public void PostLoadInit()
    {
        EnsureComponentsInit();

        branchManager.PostLoadInit();
        squadManager.PostLoadInit();
    }

    public string GetUniqueLoadID()
    {
        return "RatkinOrder_" + loadID;
    }

    private void EnsureComponentsInit()
    {
        esteemHandler ??= new EsteemHandler(this, initConstruct: true);
        fundHandler ??= new FundHandler(this);
        branchManager ??= new BranchManager(this);
        squadManager ??= new SquadManager(this, initConstruct: true);
        reformationManager ??= new ReformationManager(this, initConstruct: true);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref loadID, "loadID", -1);
        Scribe_References.Look(ref faction, "faction");

        Scribe_Deep.Look(ref esteemHandler, "esteemHandler", ctorArgs: [this, false]);
        Scribe_Deep.Look(ref fundHandler, "fundHandler", ctorArgs: this);
        Scribe_Deep.Look(ref reformationManager, "reformationManager", ctorArgs: [this, false]);
        Scribe_Deep.Look(ref branchManager, "branchManager", ctorArgs: this);
        Scribe_Deep.Look(ref squadManager, "squadManager", ctorArgs: [this, false]);
    }
}
