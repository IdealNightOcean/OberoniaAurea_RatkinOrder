using OberoniaAurea_Frame;
using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrder : IExposable, ILoadReferenceable
{
    private int loadID = -1;
    public int LoadID => loadID;

    [Unsaved] public readonly int TickHashOffset;
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
    [Unsaved] public readonly TagStrToBoolCountable EffectTags = new();
    [Unsaved] public readonly BranchStatTransformerHandler TransformerHandler = new();

    // 认可度 | 关系 | 推荐信
    private EsteemHandler esteemHandler;
    public EsteemHandler EsteemHandler => esteemHandler;
    public int Esteem => esteemHandler.Esteem;
    public EsteemHandler.RelationshipKind Relationship => esteemHandler.Relationship;

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

    private RatkinOrder()
    {
        TickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
    }

    public RatkinOrder(RatkinOrderDef def, Faction faction) : this()
    {
        this.def = def;
        this.faction = faction;
        curYearPassed = TickUtility.YearPassed();

        cooldownManager = new();
        esteemHandler = new EsteemHandler(this);
        fundHandler = new FundHandler(this);
        reformationManager = new ReformationManager(this);
        branchManager = new BranchManager(this);

        loadID = UniqueIDManager.GetUniqueID("RatkinOrder");
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
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_Order(this));

    public void Tick()
    {
        branchManager.Tick();

        if (this.IsHashIntervalTick(1000))
        {
            branchManager.TickLong();

            if (this.IsHashIntervalTick(60000))
            {
                fundHandler.DailySettlement();
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
    }

    internal void PostGenerated()
    {
        esteemHandler.PostOrderGenerated();
        fundHandler.PostOrderGenerated();
        reformationManager.PostOrderGenerated();
    }

    internal void PostLoadInit()
    {
        branchManager.PostLoadInit();
    }

    public string GetUniqueLoadID() => "RatkinOrder_" + loadID;
    public override string ToString() => "RatkinOrder_" + loadID;


}