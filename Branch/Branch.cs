using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士团分部
/// </summary>
public class Branch : IExposable, ILoadReferenceable
{
    [Flags]
    public enum BranchType : byte
    {
        /// <summary>
        /// 普通
        /// </summary>
        Normal = 0,
        /// <summary>
        /// 友好
        /// </summary>
        Friendly = 1,
        /// <summary>
        /// 荣誉
        /// </summary>
        Honor = 2,
        /// <summary>
        /// 机动
        /// </summary>
        Mobile = 4
    }

    public enum WorkStateType : byte
    {
        /// <summary>
        /// 空闲
        /// </summary>
        Idle,
        /// <summary>
        /// 驻地任务
        /// </summary>
        OnBaseTask,
        /// <summary>
        /// 外出任务
        /// </summary>
        AbroadTask
    }

    public RatkinOrder RatkinOrder { get; }
    public BranchManager BranchManager => RatkinOrder.BranchManager;

    private int loadID = -1;
    public int LoadID => loadID;

    [Unsaved] private readonly int tickHashOffset;

    private int ordinal;
    private string nameCore = string.Empty;
    private string name = string.Empty;

    public string NameCore => nameCore;
    public string Name => name;
    public Color Color
    {
        get
        {
            if (IsBranchOfType(BranchType.Friendly))
            {
                return Color.green;
            }
            return Color.white;
        }
    }
    public string NameColored => name.Colorize(Color);

    private WorldObject baseSite;
    public WorldObject BaseSite => baseSite;
    public int Tile => baseSite?.Tile ?? -1;

    private BranchType curType = BranchType.Normal;
    public BranchType CurType => curType;

    protected int friendlyDaysLeft = -1;
    public int FriendlyDaysLeft => friendlyDaysLeft;

    private bool hasSupportAuthority;
    public bool HasSupportAuthority
    {
        get => hasSupportAuthority;
        set => hasSupportAuthority = value;
    }

    private bool commanderVisitable;
    public bool CommanderVisitable
    {
        get => commanderVisitable;
        set => commanderVisitable = value;
    }

    public BranchHonorDef HonorDef { get; protected set; }
    public KnightChivalryDef HonorChivalry => HonorDef?.chivalry;

    [Unsaved] private SimpleValueCache<float> supplyCeilingCache;
    private float supply;
    public float Supply
    {
        get => supply;
        set => supply = Mathf.Clamp(value, 0f, supplyCeilingCache.GetCachedResult());
    }
    public string SupplyState
    {
        get
        {
            return Supply switch
            {
                < 0.2f => "OARO_BranchSupply_Lack".Translate().Colorize(ColorLibrary.Orange),
                < 0.8f => "OARO_BranchSupply_Just".Translate().Colorize(Color.yellow),
                _ => "OARO_BranchSupply_Enough".Translate().Colorize(Color.green)
            };
        }
    }

    [Unsaved] private SimpleValueCache<float> potencyCache;
    public float Potency => potencyCache.GetCachedResult();

    [Unsaved] private WorkStateType curWorkState = WorkStateType.Idle;
    [Unsaved] private string curWorkStateDesc = string.Empty;
    private bool WorkStateDirty { get; set; }

    public WorkStateType CurWorkState
    {
        get
        {
            if (WorkStateDirty)
            {
                UpdateWorkState();
                WorkStateDirty = false;
            }
            return curWorkState;
        }
    }
    public string CurWorkStateDesc
    {
        get
        {
            if (WorkStateDirty)
            {
                UpdateWorkState();
                WorkStateDirty = false;
            }
            return curWorkStateDesc;
        }
    }

    private string greetingDescCache = string.Empty;
    private int nextGreetingDescCacheTick = -1;
    public string GreetingDesc
    {
        get
        {
            if (Find.TickManager.TicksGame > nextGreetingDescCacheTick)
            {
                UpdateGreetingDesc();
            }
            return greetingDescCache;
        }
    }

    public TagStrToBoolCountable EffectTags { get; } = new();
    public StatTransformerHandler<BranchStatDef> TransformerHandler { get; } = new();
    public List<IPostCombatantGenerate> IPostCombatantGenerate { get; } = [];
    /// <summary>
    /// 分部交互应用后触发，供 UI 窗口订阅以刷新缓存
    /// </summary>
    public EventDispatcher<Action<BranchInteractionDef, BranchInteractionParms, bool>> PostApplyBranchInteraction { get; } = new();

    private CooldownRecordManager cooldownManager;
    public CooldownRecordManager CooldownManager => cooldownManager;

    private BranchMedalHandler medalHandler;
    private BranchFacilityHandler facilityHandler;
    private BranchBuildingHandler buildingHandler;
    private BranchPopulationHandler populationHandler;
    private BranchSquad squad;
    private BranchTaskHandler taskHandler;
    private BranchDemandHandler demandHandler;
    private BranchResidentHandler residentHandler;
    private BranchStoresReserveHandler storesReserveHandler;
    private BranchTraditionHandler traditionHandler;

    public BranchMedalHandler MedalHandler => medalHandler;
    public BranchFacilityHandler FacilityHandler => facilityHandler;
    public BranchBuildingHandler BuildingHandler => buildingHandler;
    public BranchPopulationHandler PopulationHandler => populationHandler;
    public BranchSquad Squad => squad;
    public BranchTaskHandler TaskHandler => taskHandler;
    public BranchDemandHandler DemandHandler => demandHandler;
    public BranchResidentHandler ResidentHandler => residentHandler;
    public BranchStoresReserveHandler StoresReserveHandler => storesReserveHandler;
    public BranchTraditionHandler TraditionHandler => traditionHandler;

    public bool IsConstructionBusy => facilityHandler.IsBusy || buildingHandler.IsBusy;

    private Branch(RatkinOrder ratkinOrder, bool initCtor)
    {
        RatkinOrder = ratkinOrder ?? throw new NullReferenceException(nameof(ratkinOrder));

        if (initCtor)
        {
            cooldownManager = new();

            medalHandler = new();
            facilityHandler = new(this);
            buildingHandler = new(this);
            populationHandler = new(this);
            squad = new(this);
            taskHandler = new(this);
            demandHandler = new(this);
            residentHandler = new(this, initCtor: true);
            storesReserveHandler = new(this);
            traditionHandler = new(this);
        }

        tickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
        supplyCeilingCache = new(cacheInterval: 2500,
                                 defaultValue: BranchStatDefOf.OARO_SupplyCeiling.baseValue,
                                 checker: () => this.GetStatValue(BranchStatDefOf.OARO_SupplyCeiling));

        potencyCache = new(cacheInterval: 2500, defaultValue: 1f, GetCurPotency);
        TransformerHandler.OnZeroFactorUnmerged += OnZeroFactorUnmerged;

        loadID = UniqueIDManager.GetUniqueID(nameof(Branch));
    }

    public static Branch GenerateBranchFor(RatkinOrder ratkinOrder, WorldObject worldObject, bool addToManager = true)
    {
        if (!worldObject.CanBeSiteForNewBranch(ratkinOrder))
        {
            Log.Error($"[OARO] {nameof(worldObject)} 不能用作新 {nameof(Branch)} 的 {nameof(BaseSite)}。");
            return null;
        }

        Branch branch;
        try
        {
            branch = new(ratkinOrder, initCtor: true);
            worldObject.GetComponent<WorldObjectComp_BranchSite>().SetOrderBranch(branch);
            branch.baseSite = worldObject;
            branch.PostGenerated();
            if (addToManager)
            {
                ratkinOrder.BranchManager.AddBranch(branch);
            }
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"generating a new branch for {ratkinOrder} at {worldObject}",
                typeName: nameof(Branch),
                methodName: nameof(GenerateBranchFor),
                needStackTrace: true);
            return null;
        }

        return branch;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref loadID, nameof(loadID), -1);

        Scribe_References.Look(ref baseSite, nameof(baseSite));

        Scribe_Values.Look(ref ordinal, nameof(ordinal), 0);
        Scribe_Values.Look(ref nameCore, nameof(nameCore), string.Empty);
        Scribe_Values.Look(ref name, nameof(name), string.Empty);

        Scribe_Values.Look(ref commanderVisitable, nameof(commanderVisitable), defaultValue: false);
        Scribe_Values.Look(ref hasSupportAuthority, nameof(hasSupportAuthority), defaultValue: false);
        Scribe_Values.Look(ref friendlyDaysLeft, nameof(friendlyDaysLeft), 0);
        Scribe_Values.Look(ref curType, nameof(curType), BranchType.Normal);

        Scribe_Values.Look(ref supply, nameof(supply), 0f);

        Scribe_Deep.Look(ref cooldownManager, nameof(cooldownManager));
        Scribe_Deep.Look(ref medalHandler, nameof(medalHandler));
        Scribe_Deep.Look(ref facilityHandler, nameof(facilityHandler), ctorArgs: this);
        Scribe_Deep.Look(ref buildingHandler, nameof(buildingHandler), ctorArgs: this);
        Scribe_Deep.Look(ref squad, nameof(squad), ctorArgs: this);
        Scribe_Deep.Look(ref populationHandler, nameof(populationHandler), ctorArgs: this);
        Scribe_Deep.Look(ref taskHandler, nameof(taskHandler), ctorArgs: this);
        Scribe_Deep.Look(ref demandHandler, nameof(demandHandler), ctorArgs: this);
        Scribe_Deep.Look(ref residentHandler, nameof(residentHandler), ctorArgs: [this, false]);
        Scribe_Deep.Look(ref storesReserveHandler, nameof(storesReserveHandler), ctorArgs: this);
        Scribe_Deep.Look(ref traditionHandler, nameof(traditionHandler), ctorArgs: this);
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_Branch(this));

    public void Tick()
    {
        if (TickUtility.IsHashIntervalTick(tickHashOffset, 2500))
        {
            TickHour();

            if (TickUtility.IsHashIntervalTick(tickHashOffset, 60000))
            {
                TickDay();
            }
        }
    }

    public void MarkWorkStateDirty() => WorkStateDirty = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBranchOfType(BranchType type) => (curType & type) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBranchType(BranchType type, bool active)
    {
        if (active) { curType |= type; }
        else { curType &= ~type; }
    }

    public void SetFriendly(bool active, int durationDays = -1, bool showMessage = true)
    {
        bool activeNow = IsBranchOfType(BranchType.Friendly);
        if (active)
        {
            if (!activeNow)
            {
                SetBranchType(BranchType.Friendly, true);
                BranchManager.FriendlyBranchesCount.MarkDirty();
            }
            if (durationDays < 0)
            {
                durationDays = BranchUtility.GetDefaultFriendlyDurationDays(this);
            }

            friendlyDaysLeft = durationDays > friendlyDaysLeft ? durationDays : friendlyDaysLeft;
            if (showMessage)
            {
                Messages.Message(
                    text: "OARO_Mess_BranchBeFriendly".Translate(name.Named(KeyLibrary_FormatArgName.BranchName), friendlyDaysLeft.Named(KeyLibrary_FormatArgName.Count)),
                    lookTargets: baseSite,
                    def: MessageTypeDefOf.PositiveEvent);
            }
        }
        else if (activeNow)
        {
            SetBranchType(BranchType.Friendly, false);
            BranchManager.FriendlyBranchesCount.MarkDirty();
            friendlyDaysLeft = -1;
            if (showMessage)
            {
                Messages.Message("OARO_Mess_BranchBeNonFriendly".Translate(name), baseSite, MessageTypeDefOf.NegativeEvent);
            }
        }
    }

    public void SetHonorDef(BranchHonorDef honorDef, bool replaceCur = false)
    {
        if (!replaceCur && HonorDef is not null)
        {
            Log.Error($"[OARO] 设置 {nameof(BranchHonorDef)} 失败：{nameof(HonorDef)} 已存在且 {nameof(replaceCur)} 为 false。");
            return;
        }
        SetBranchType(BranchType.Honor, active: true);
        HonorDef = honorDef;
    }

    public void Rename(int ordinal, string nameCore)
    {
        this.ordinal = ordinal;
        this.nameCore = nameCore;
        int unitsDigit = ordinal % 10;
        GrammarRequest grammarRequest = new()
        {
            Includes = { OARO_RulePackDefOf.OARO_NameBuilder_BranchName }
        };
        grammarRequest.Constants.Add("unitsDigit", unitsDigit.ToString());
        grammarRequest.Rules.Add(new Rule_String("ordinal", ordinal.ToString()));
        grammarRequest.Rules.Add(new Rule_String("nameCore", nameCore));
        name = GrammarResolver.Resolve("r_name", grammarRequest);

        squad.Rename(ordinal, nameCore);
    }

    public void Destroy()
    {
        residentHandler.ForceEndAllResidency();
        baseSite?.GetComponent<WorldObjectComp_BranchSite>()?.Notify_BranchDestroyed(this);
    }

    public void PostCombatantGenerate(Pawn p, KnightRecord record)
    {
        if (IPostCombatantGenerate is null || IPostCombatantGenerate.Count == 0)
        {
            return;
        }

        for (int i = 0; i < IPostCombatantGenerate.Count; i++)
        {
            try
            {
                IPostCombatantGenerate[i].PostCombatantGenerate(p, record);
            }
            catch (Exception ex)
            {
                string processorTypeName = IPostCombatantGenerate[i]?.GetType()?.FullName ?? "UnknownProcessor";
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"execute post-combatant-generate processor: {processorTypeName}",
                    typeName: nameof(Branch),
                    methodName: nameof(PostCombatantGenerate),
                    needStackTrace: true);
            }
        }
    }

    /// <summary>
    /// 触发 PostApplyBranchInteraction 事件，仅供交互 Worker 调用
    /// </summary>
    public void OnPostApplyBranchInteraction(BranchInteractionDef def, BranchInteractionParms parms, bool succeeded)
    {
        PostApplyBranchInteraction.Raise(handler => handler(def, parms, succeeded));
    }

    private void TickHour()
    {
        int hourOfDay = GenLocalDate.HourOfDay(baseSite.Tile);

        facilityHandler.TickHour();
        buildingHandler.TickHour();

        if (!IsConstructionBusy)
        {
            storesReserveHandler.TickHour(hourOfDay);
        }

        squad.TickHour(hourOfDay);

        if (!CooldownManager.IsInCooldown(KeyLibrary_CDRecord.BranchWorkState))
        {
            MarkWorkStateDirty();
        }
    }

    private void TickDay()
    {
        if (friendlyDaysLeft > 0 && (friendlyDaysLeft--) <= 0)
        {
            SetFriendly(false);
        }

        Supply += this.GetStatValue(BranchStatDefOf.OARO_SupplyRecoveryRate);

        buildingHandler.TickDay();
        populationHandler.TickDay();
        demandHandler.TickDay();
        residentHandler.TickDay();
    }

    private void UpdateWorkState()
    {
        CooldownManager.RegisterRecord(KeyLibrary_CDRecord.BranchWorkState, cdTicks: 6 * 2500);

        if (taskHandler.HasTask)
        {
            curWorkState = taskHandler.CurTask.Def.isAbroadTask ? WorkStateType.AbroadTask : WorkStateType.OnBaseTask;
            curWorkStateDesc = taskHandler.CurTask.Label;
            return;
        }

        if (this.IsOnJointPatrol())
        {
            curWorkState = WorkStateType.AbroadTask;
            curWorkStateDesc = "OARO_BranchWorkState_JointPatrol".Translate();
            return;
        }

        curWorkState = WorkStateType.Idle;
        int hourOfDay = GenLocalDate.HourOfDay(baseSite.Tile);
        if (hourOfDay <= 5 || hourOfDay >= 21)
        {
            curWorkStateDesc = "OARO_BranchWorkState_Rest".Translate();
        }
        else
        {
            curWorkStateDesc = "OARO_BranchWorkState_Idle".Translate();
        }

        WorkStateDirty = false;
    }

    private float GetCurPotency()
    {
        return this.GetStatValue(BranchStatDefOf.OARO_BranchPotency, baseValueOverride: squad.AllCrewCount * 7f);
    }

    private void OnZeroFactorUnmerged(IEnumerable<BranchStatDef> statDefs)
    {
        foreach (BranchStatDef stat in statDefs)
        {
            this.RecacheBranchStat(stat);
        }
    }

    private void UpdateGreetingDesc()
    {
        nextGreetingDescCacheTick = Find.TickManager.TicksGame + 15000;

        GrammarRequest grammarRequest = new()
        {
            Includes = { OARO_RulePackDefOf.OARO_Maker_BranchGreetingDesc }
        };

        grammarRequest.Rules.AddRange(ModUtility.RulesForRatkinOrder(KeyLibrary_FormatArgName.ORDER, RatkinOrder));
        grammarRequest.Rules.AddRange(ModUtility.RulesForBranch(KeyLibrary_FormatArgName.BRANCH, this, alsoAddOrderRule: false));
        grammarRequest.Constants.Add("hourOfDay", GenLocalDate.HourOfDay(baseSite.Tile).ToString());
        grammarRequest.Constants.Add("population", populationHandler.Population.ToString());
        grammarRequest.Constants.Add("populationRatio", populationHandler.PopulationRatio.ToString("F2"));

        string buildingParagraph = buildingHandler.AllBuildings.Where(b => b.HasGreetingParagraph).RandomElementWithFallback(fallback: null)?.GreetingParagraph ?? string.Empty;
        grammarRequest.Rules.Add(new Rule_String(nameof(buildingParagraph), buildingParagraph));

        if (Rand.Bool)
        {
            grammarRequest.Constants.Add("relationShip", "None");
        }
        else if (IsBranchOfType(BranchType.Friendly))

        {
            grammarRequest.Constants.Add("relationShip", "FriendlyBranch");
        }
        else
        {
            grammarRequest.Constants.Add("relationShip", RatkinOrder.Relationship.ToString());

        }

        greetingDescCache = GrammarResolver.Resolve("r_text", grammarRequest);
    }

    private void PostGenerated()
    {
        int ordinal = GetBranchOrdinal(this);
        nameCore = GenerateBranchNameCore(RatkinOrder);
        Rename(ordinal, nameCore);

        medalHandler.PostBranchGenerated();
        facilityHandler.PostBranchGenerated();
        buildingHandler.PostBranchGenerated();

        populationHandler.PostBranchGenerated();
        squad.PostBranchGenerated();

        residentHandler.PostBranchGenerated();

        taskHandler.FocusedTaskChivalry = medalHandler.PrimaryChivalry;
        supply = Rand.Range(0.4f, 0.8f);
    }

    internal void PostLoadInit()
    {
        traditionHandler ??= new(this);

        medalHandler.PostLoadInit();

        facilityHandler.PostLoadInit();
        buildingHandler.PostLoadInit();
        populationHandler.PostLoadInit();

        demandHandler.PostLoadInit();

        residentHandler.PostLoadInit();
        storesReserveHandler.PostLoadInit();

        traditionHandler.PostLoadInit();
    }

    public string GetUniqueLoadID() => $"{nameof(Branch)}_{loadID}";
    public override string ToString() => $"{nameof(Branch)}_{loadID}";

    /// <summary>
    /// 分部的名称序号生成器
    /// </summary>
    /// <returns>1~999的尽量不重复的随机数</returns>
    private static int GetBranchOrdinal(Branch branch)
    {
        int m = 999;
        int a = 445;
        int c = 700001;
        unchecked
        {
            int ordinal = 31 * branch.LoadID + branch.RatkinOrder.LoadID;
            ordinal ^= (ordinal >> 16);
            ordinal = (a * ordinal + c) % m + 1;
            return ordinal > 0 ? ordinal : ordinal + m;
        }
    }

    private static string GenerateBranchNameCore(RatkinOrder ratkinOrder)
    {
        GrammarRequest grammarRequest = new()
        {
            Includes = { ratkinOrder.Def.branchNameCoreSelecter }
        };

        return NameGenerator.GenerateName(grammarRequest, IsUniqueName, false, rootKeyword: "r_name");

        bool IsUniqueName(string name)
        {
            return !ratkinOrder.BranchManager.AllBranches.Select(b => b.NameCore).Contains(name);
        }
    }
}