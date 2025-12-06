using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public class Branch : IExposable, ILoadReferenceable
{
    [Flags]
    public enum BranchType : byte
    {
        Normal = 0,
        Friendly = 1,
        Honor = 2,
        Mobile = 4
    }

    public RatkinOrder RatkinOrder { get; }
    public BranchManager BranchManager => RatkinOrder.BranchManager;

    private int loadID = -1;
    public int LoadID => loadID;

    [Unsaved] public readonly int TickHashOffset;

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
            return HonorDef?.color ?? RatkinOrder.Color;
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

    public bool HasSupportAuthority;

    [Unsaved] private BranchHonorDef honorDef;
    public BranchHonorDef HonorDef
    {
        get => IsBranchOfType(BranchType.Honor) ? honorDef : null;
        set => honorDef = value;
    }
    private SimpleValueCache<float> supplyCeilingCache;
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

    [Unsaved] private bool isIdleNow = true;
    [Unsaved] private bool isOutdoorNow = true;
    [Unsaved] private string curWorkState = string.Empty;
    [Unsaved] private bool workStateDirty = true;
    public bool IsIdleNow
    {
        get
        {
            if (workStateDirty)
            {
                UpdateWorkState();
                workStateDirty = false;
            }
            return isIdleNow;
        }
    }

    public bool IsOutdoorNow
    {
        get
        {
            if (workStateDirty)
            {
                UpdateWorkState();
                workStateDirty = false;
            }
            return isOutdoorNow;
        }
    }

    public string CurWorkState
    {
        get
        {
            if (workStateDirty)
            {
                UpdateWorkState();
                workStateDirty = false;
            }
            return curWorkState;
        }
    }

    [Unsaved] public readonly TagStrToBoolCountable EffectTags = new();
    [Unsaved] public readonly BranchStatTransformerHandler TransformerHandler = new();
    public List<IPostCombatantGenerate> IPostCombatantGenerate { get; } = [];
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

    public BranchMedalHandler MedalHandler => medalHandler;
    public BranchFacilityHandler FacilityHandler => facilityHandler;
    public BranchBuildingHandler BuildingHandler => buildingHandler;
    public BranchPopulationHandler PopulationHandler => populationHandler;
    public BranchSquad Squad => squad;
    public BranchTaskHandler TaskHandler => taskHandler;
    public BranchDemandHandler DemandHandler => demandHandler;
    public BranchResidentHandler ResidentHandler => residentHandler;
    public BranchStoresReserveHandler StoresReserveHandler => storesReserveHandler;

    public bool IsConstructionBusy => facilityHandler.IsBusy || buildingHandler.IsBusy;

    public Action<BranchInteractionDef, BranchInteractionParms, bool> PostApplyBranchInteraction { get; set; }

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
        }

        TickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
        supplyCeilingCache = new(cacheInterval: 2500,
                                 defaultValue: BranchStatDefOf.OARO_SupplyCeiling.baseValue,
                                 checker: () => this.GetStatValue(BranchStatDefOf.OARO_SupplyCeiling));

        potencyCache = new(cacheInterval: 2500, defaultValue: 1f, GetCurPotency);
        loadID = UniqueIDManager.GetUniqueID(nameof(Branch));
    }

    public static Branch GenerateBranchFor(RatkinOrder ratkinOrder, WorldObject worldObject, bool addToManager = true)
    {
        if (!worldObject.CanBeSiteForNewBranch(ratkinOrder))
        {
            Log.Error($"[OARO] {nameof(worldObject)} cannot be used as a {nameof(BaseSite)} for a new {nameof(Branch)}.");
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

        Scribe_Values.Look(ref HasSupportAuthority, nameof(HasSupportAuthority), defaultValue: false);
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
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_Branch(this));

    public void Tick()
    {
        if (this.IsHashIntervalTick(2500))
        {
            TickHour();

            if (this.IsHashIntervalTick(60000))
            {
                TickDay();
            }
        }
    }

    public void MarkWorkStateDirty() => workStateDirty = true;

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
                Messages.Message("OARO_Mess_BranchBeFriendly".Translate(name, friendlyDaysLeft), baseSite, MessageTypeDefOf.PositiveEvent);
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
            isIdleNow = false;
            isOutdoorNow = taskHandler.CurTask.Def.isOutdoorTask;
            curWorkState = taskHandler.CurTask.Label;
            return;
        }

        if (this.IsOnJointPatrol())
        {
            isIdleNow = false;
            isOutdoorNow = true;
            curWorkState = "OARO_BranchWorkState_JointPatrol".Translate();
            return;
        }

        isIdleNow = true;
        isOutdoorNow = false;
        int hourOfDay = GenLocalDate.HourOfDay(baseSite.Tile);
        if (hourOfDay <= 5 || hourOfDay >= 21)
        {
            curWorkState = "OARO_BranchWorkState_Rest".Translate();
            return;
        }

        curWorkState = "OARO_BranchWorkState_Idle".Translate();

        workStateDirty = false;
    }

    private float GetCurPotency()
    {
        float curPotency = squad.AllCrewCount * 7f
                         * (0.9f + facilityHandler.TotalFacilityLevel * 0.025f + medalHandler.TotalMedalCount * 0.015f)
                         * (IsBranchOfType(BranchType.Honor) ? 1.25f : 1f);

        return curPotency * 0.01f;
    }

    private void PostGenerated()
    {
        int ordinal = BranchUtility.GetBranchOrdinal(this);
        nameCore = BranchUtility.GenerateBranchNameCore(RatkinOrder);
        Rename(ordinal, nameCore);



        medalHandler.PostBranchGenerated();
        facilityHandler.PostBranchGenerated();
        buildingHandler.PostBranchGenerated();

        populationHandler.PostBranchGenerated();
        squad.PostBranchGenerated();

        residentHandler.PostBranchGenerated();

        taskHandler.FocusedTaskType = medalHandler.ProtogenicTaskType;
        supply = Rand.Range(0.4f, 0.8f);
    }

    internal void PostLoadInit()
    {
        medalHandler.PostLoadInit();

        facilityHandler.PostLoadInit();
        buildingHandler.PostLoadInit();
        populationHandler.PostLoadInit();

        residentHandler.PostLoadInit();
        storesReserveHandler.PostLoadInit();
    }

    public string GetUniqueLoadID() => $"{nameof(Branch)}_{loadID}";
    public override string ToString() => $"{nameof(Branch)}_{loadID}";
}