using OberoniaAurea_Frame;
using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrder : IExposable, ILoadReferenceable, IPostLoadInit
{
    private int loadID = -1;
    public int LoadID => loadID;

    [Unsaved] public int TickHashOffset;
    private int curYearPassed = -1;

    private RatkinOrderDef def;
    public RatkinOrderDef Def => def;

    private Faction faction;
    public Faction Faction => faction;

    private string name;
    public string Name
    {
        get => name ?? def.label;
        set => name = value;
    }

    [Unsaved] private Color? color;
    public Color Color => color ??= (def.color ?? faction.color ?? Color.white);
    public string NameColored => Name.Colorize(Color);


    private CooldownRecordManager cooldownManager;
    public CooldownRecordManager CooldownManager => cooldownManager;

    // 认可度 | 关系 | 推荐信
    private EsteemHandler esteemHandler;
    public EsteemHandler EsteemHandler => esteemHandler;
    public int Esteem => esteemHandler.Esteem;
    public OrderRelationshipKind Relationship => esteemHandler.Relationship;

    //资金
    private FundHandler fundHandler;
    public FundHandler FundHandler => fundHandler;
    public float Funds => fundHandler.Funds;

    //自新
    private ReformationManager reformationManager;
    public ReformationManager ReformationManager => reformationManager;
    public float ReformProgress => reformationManager.ReformProgress;

    //分部管理
    private BranchManager branchManager;
    public BranchManager BranchManager => branchManager;

    //分队部分管理（分队为分部的子组件，生命周期和分部强相关）
    private SquadManager squadManager;
    public SquadManager SquadManager => squadManager;

    private RatkinOrder()
    {
        TickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
    }

    public RatkinOrder(RatkinOrderDef def, Faction faction) : this()
    {
        this.def = def;
        this.faction = faction;
        curYearPassed = TickUtility.YearPassed();

        EnsureComponentsInit();

        loadID = UniqueIDManager.Instance.GetUniqueID("RatkinOrder");
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref loadID, "loadID", -1);
        Scribe_Values.Look(ref curYearPassed, "curYearPassed", -1);
        Scribe_Defs.Look(ref def, "def");
        Scribe_References.Look(ref faction, "faction");
        Scribe_Values.Look(ref name, "name");

        Scribe_Deep.Look(ref cooldownManager, "cooldownManager");
        Scribe_Deep.Look(ref esteemHandler, "esteemHandler", ctorArgs: this);
        Scribe_Deep.Look(ref fundHandler, "fundHandler", ctorArgs: this);
        Scribe_Deep.Look(ref reformationManager, "reformationManager", ctorArgs: this);
        Scribe_Deep.Look(ref branchManager, "branchManager", ctorArgs: this);
        Scribe_Deep.Look(ref squadManager, "squadManager", ctorArgs: [this, false]);
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_Order(this));

    public void Tick()
    {
        branchManager.Tick();

        if (this.IsHashIntervalTick(1000))
        {
            squadManager.TickLong();

            if (this.IsHashIntervalTick(60000))
            {
                fundHandler.TickDay();
                branchManager.TickDay();

                if (TickUtility.YearPassed() > curYearPassed)
                {
                    curYearPassed = TickUtility.YearPassed();
                }
            }
        }
    }

    public void OnRemoved()
    {
        branchManager.Notify_MyOrderRemoved();
        squadManager.Notify_MyOrderRemoved();
    }

    public void PostGenerated()
    {
        esteemHandler.PostOrderGenerated();
        fundHandler.PostOrderGenerated();
        reformationManager.PostOrderGenerated();
    }

    public void PostLoadInit()
    {
        EnsureComponentsInit();

        branchManager.PostLoadInit();
        squadManager.PostLoadInit();
    }

    public string GetUniqueLoadID() => "RatkinOrder_" + loadID;

    private void EnsureComponentsInit()
    {
        cooldownManager ??= new();
        esteemHandler ??= new EsteemHandler(this);
        fundHandler ??= new FundHandler(this);
        reformationManager ??= new ReformationManager(this);
        branchManager ??= new BranchManager(this);
        squadManager ??= new SquadManager(this, initConstruct: true);
    }
}
