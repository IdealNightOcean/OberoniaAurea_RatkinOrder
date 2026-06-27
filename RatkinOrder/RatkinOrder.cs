using OberoniaAurea_Frame;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrder : IExposable, ILoadReferenceable
{
    private int loadID = -1;
    public int LoadID => loadID;

    public bool HasRemoved { get; private set; }

    [Unsaved] private readonly int tickHashOffset;
    private int curYearPassed = -1;

    private RatkinOrderDef def;
    public RatkinOrderDef Def => def;

    private Faction faction;
    public Faction Faction => faction;

    [Unsaved] private Color? color;
    public Color Color => color ??= (def.color ?? faction?.Color ?? Color.white);

    private string name;
    public string Name
    {
        get => name ?? def.label;
        set => name = value;
    }
    public string NameColored => Name.Colorize(Color);

    private CooldownRecordManager cooldownManager;
    public CooldownRecordManager CooldownManager => cooldownManager;
    public TagStrToBoolCountable EffectTags { get; } = new();
    public BranchStatTransformerHandler TransformerHandler { get; } = new();

    /// <summary>
    /// 骑士团交互应用后触发，供 UI 窗口订阅以刷新缓存
    /// </summary>
    /// 
    public EventDispatcher<Action<OrderInteractionDef, Map, bool>> PostApplyOrderInteraction { get; } = new();

    private EsteemHandler esteemHandler;
    private FundHandler fundHandler;
    private ReformationManager reformationManager;
    private BranchManager branchManager;
    private JointPatrolManager jointPatrolManager;

    public EsteemHandler EsteemHandler => esteemHandler;
    public FundHandler FundHandler => fundHandler;
    public ReformationManager ReformationManager => reformationManager;
    public BranchManager BranchManager => branchManager;
    public JointPatrolManager JointPatrolManager => jointPatrolManager;

    public int Esteem => esteemHandler.Esteem;
    public EsteemHandler.RelationshipKind Relationship => esteemHandler.Relationship;
    public float Funds => fundHandler.Funds;
    public float ReformProgress => reformationManager.ReformProgress;

    private RatkinOrder()
    {
        tickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
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
        jointPatrolManager = new JointPatrolManager(this);

        loadID = UniqueIDManager.GetUniqueID(nameof(RatkinOrder));
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref loadID, nameof(loadID), -1);
        Scribe_Values.Look(ref curYearPassed, nameof(curYearPassed), -1);
        Scribe_Defs.Look(ref def, nameof(def));
        Scribe_References.Look(ref faction, nameof(faction));
        Scribe_Values.Look(ref name, nameof(name));

        Scribe_Deep.Look(ref cooldownManager, nameof(cooldownManager));
        Scribe_Deep.Look(ref esteemHandler, nameof(esteemHandler), ctorArgs: this);
        Scribe_Deep.Look(ref fundHandler, nameof(fundHandler), ctorArgs: this);
        Scribe_Deep.Look(ref reformationManager, nameof(reformationManager), ctorArgs: this);
        Scribe_Deep.Look(ref branchManager, nameof(branchManager), ctorArgs: this);
        Scribe_Deep.Look(ref jointPatrolManager, nameof(jointPatrolManager), ctorArgs: this);
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_Order(this));

    public void Tick()
    {
        branchManager.Tick();

        if (TickUtility.IsHashIntervalTick(tickHashOffset, 1000))
        {
            jointPatrolManager.TickLong();

            if (TickUtility.IsHashIntervalTick(tickHashOffset, 60000))
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
        HasRemoved = true;
        branchManager.Notify_MyOrderRemoved();
        jointPatrolManager.Notify_MyOrderRemoved();
    }

    /// <summary>
    /// 触发 PostApplyOrderInteraction 事件，仅供交互 Worker 调用
    /// </summary>
    public void OnPostApplyOrderInteraction(OrderInteractionDef def, Map map, bool succeeded)
    {
        PostApplyOrderInteraction.Raise(handler => handler(def, map, succeeded));
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
        jointPatrolManager.PostLoadInit();
    }

    public string GetUniqueLoadID() => $"{nameof(RatkinOrder)}_{loadID}";
    public override string ToString() => $"{nameof(RatkinOrder)}_{loadID}";
}