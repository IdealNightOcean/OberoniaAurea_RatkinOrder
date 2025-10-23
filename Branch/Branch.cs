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

    [Unsaved] public readonly RatkinOrder RatkinOrder;
    public BranchManager BranchManager => RatkinOrder.BranchManager;

    private int loadID = -1;
    public int LoadID => loadID;

    [Unsaved] public readonly int TickHashOffset;

    private int ordinal;
    private string nameCore = string.Empty;
    private string name = string.Empty;

    public string NameCore => nameCore;
    public string Name => name;

    private WorldObject baseSite;
    public WorldObject BaseSite => baseSite;
    public int Tile => baseSite?.Tile ?? -1;

    private BranchType curType = BranchType.Normal;
    public BranchType CurType => curType;

    protected int friendlyExpiredTick = -1;
    public int FriendlyExpiredTick => friendlyExpiredTick;

    public bool HasSupportAuthority;

    [Unsaved] private HonorBranchProperties honorProperties;
    public HonorBranchProperties HonorProperties
    {
        get => IsBranchOfType(BranchType.Honor) ? honorProperties : null;
        set => honorProperties = value;
    }
    private SimpleValueCache<float> supplyCeilingCache;
    private float supply;
    public float Supply
    {
        get => supply;
        set => supply = Mathf.Clamp(value, 0f, supplyCeilingCache.GetCachedResult());
    }

    [Unsaved] private bool isIdleNow = true;
    [Unsaved] private bool isOutdoorNow = true;
    [Unsaved] private string curWorkState = string.Empty;
    [Unsaved] public bool WorkStateDirty = true;
    public bool IsIdleNow
    {
        get
        {
            if (WorkStateDirty)
            {
                UpdateWorkState();
                WorkStateDirty = false;
            }
            return isIdleNow;
        }
    }

    public bool IsOutdoorNow
    {
        get
        {
            if (WorkStateDirty)
            {
                UpdateWorkState();
                WorkStateDirty = false;
            }
            return isOutdoorNow;
        }
    }

    public string CurWorkState
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

    [Unsaved] public readonly TagStrToBoolCountable EffectTags = new();
    [Unsaved] public readonly BranchStatTransformerHandler TransformerHandler = new();
    [Unsaved] public readonly List<IPostSquadCombatPawnGenerate> PostSquadCombatPawnGenerate = [];
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

    private Branch(RatkinOrder ratkinOrder, bool initCtor)
    {
        RatkinOrder = ratkinOrder ?? throw new NullReferenceException(nameof(ratkinOrder));
        TickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
        supplyCeilingCache = new(cacheInterval: 60000, defaultValue: BranchStatDefOf.OARO_SupplyCeiling.baseValue, () => this.GetStatValue(BranchStatDefOf.OARO_SupplyCeiling));

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
        loadID = UniqueIDManager.GetUniqueID("Branch");
    }

    public static Branch GenerateBranchFor(RatkinOrder ratkinOrder, WorldObject worldObject, bool addToManager = true)
    {
        if (!BranchUtility.CanBeSiteForNewBranch(ratkinOrder, worldObject))
        {
            return null;
        }

        Branch branch;
        try
        {
            branch = new(ratkinOrder, initCtor: true);
            worldObject.GetComponent<WorldObjectComp_BranchSite>().InitOrderBranch(branch);
            branch.baseSite = worldObject;
            branch.PostGenerated();
            if (addToManager)
            {
                ratkinOrder.BranchManager.AddBranch(branch);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create a new branch for {ratkinOrder} at {worldObject}: " + ex);
            return null;
        }

        return branch;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref loadID, "loadID", -1);
        Scribe_Values.Look(ref name, "name");
        Scribe_References.Look(ref baseSite, "baseSite");

        Scribe_Values.Look(ref ordinal, "ordinal", 0);
        Scribe_Values.Look(ref nameCore, "nameCore", string.Empty);
        Scribe_Values.Look(ref name, "name", string.Empty);

        Scribe_Values.Look(ref HasSupportAuthority, "HasSupportAuthority", defaultValue: false);
        Scribe_Values.Look(ref friendlyExpiredTick, "friendlyExpiredTick", 0);
        Scribe_Values.Look(ref curType, "curType", BranchType.Normal);

        Scribe_Values.Look(ref supply, "supply", 0f);

        Scribe_Deep.Look(ref cooldownManager, "cooldownManager");
        Scribe_Deep.Look(ref medalHandler, "medalHandler");
        Scribe_Deep.Look(ref facilityHandler, "facilityHandler", ctorArgs: this);
        Scribe_Deep.Look(ref buildingHandler, "buildingHandler", ctorArgs: this);
        Scribe_Deep.Look(ref squad, "squad", ctorArgs: this);
        Scribe_Deep.Look(ref populationHandler, "populationHandler", ctorArgs: this);
        Scribe_Deep.Look(ref taskHandler, "taskHandler", ctorArgs: this);
        Scribe_Deep.Look(ref demandHandler, "demandHandler", ctorArgs: this);
        Scribe_Deep.Look(ref residentHandler, "residentHandler", ctorArgs: [this, false]);
        Scribe_Deep.Look(ref storesReserveHandler, "storesReserveHandler", ctorArgs: this);
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

    private void TickHour()
    {
        int hourOfDay = GenLocalDate.HourOfDay(baseSite.Tile);

        facilityHandler.TickHour();
        buildingHandler.TickHour(hourOfDay);

        if (!buildingHandler.IsBusy && !facilityHandler.IsBusy)
        {
            storesReserveHandler.TickHour(hourOfDay);
        }

        if (friendlyExpiredTick > 0 && (friendlyExpiredTick -= 2500) <= 0)
        {
            SetFriendly(false);
        }

        squad.TickHour(hourOfDay);

        if (!CooldownManager.IsInCooldown(KeyLibrary_CDRecord.BranchWorkState))
        {
            WorkStateDirty = true;
        }
    }

    private void TickDay()
    {
        buildingHandler.TickDay();
        populationHandler.TickDay();
        demandHandler.TickDay();
        residentHandler.TickDay();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBranchOfType(BranchType type) => (curType & type) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBranchType(BranchType type, bool active)
    {
        if (active) { curType |= type; }
        else { curType &= ~type; }
    }

    public void SetFriendly(bool friendly, int durationTick = 40 * 60000, bool showMessage = true)
    {
        if (friendly)
        {
            if (friendlyExpiredTick > 0)
            {
                friendlyExpiredTick += durationTick;
            }
            else
            {
                SetBranchType(BranchType.Friendly, true);
                friendlyExpiredTick = Find.TickManager.TicksGame + durationTick;
            }
        }
        else
        {
            SetBranchType(BranchType.Friendly, false);
            friendlyExpiredTick = -1;
        }
    }

    public void Rename(int ordinal, string nameCore)
    {
        this.ordinal = ordinal;
        this.nameCore = nameCore;
        int unitsDigit = ordinal % 10;
        GrammarRequest grammarRequest = new()
        {
            Includes = { OARO_ModDefOf.OARO_NameBuilder_BranchName }
        };
        grammarRequest.Constants.Add("unitsDigit", unitsDigit.ToString());
        grammarRequest.Rules.Add(new Rule_String("ordinal", ordinal.ToString()));
        grammarRequest.Rules.Add(new Rule_String("nameCore", nameCore));
        name = GrammarResolver.Resolve("r_name", grammarRequest);

        squad.Rename(ordinal, nameCore);
    }

    private void UpdateWorkState()
    {
        CooldownManager.RegisterRecord(KeyLibrary_CDRecord.BranchWorkState, cdTicks: 6 * 2500);

        if (taskHandler.HasTask)
        {
            isIdleNow = false;
            isOutdoorNow = taskHandler.CurTask.Def.isOutdoorTask;
            curWorkState = taskHandler.TaskLabel;
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
    }

    public void Destroy()
    {
        residentHandler.ForceEndAllResidency();
        baseSite?.GetComponent<WorldObjectComp_BranchSite>()?.Notify_BranchDestroyed(this);
    }

    private void PostGenerated()
    {
        int ordinal = BranchUtility.GetBranchOrdinal(loadID, RatkinOrder.LoadID);
        nameCore = BranchUtility.GenerateBranchNameCore(RatkinOrder);
        Rename(ordinal, nameCore);

        medalHandler.PostBranchGenerated();
        facilityHandler.PostBranchGenerated();
        buildingHandler.PostBranchGenerated();

        populationHandler.PostBranchGenerated();
        squad.PostBranchGenerated();

        residentHandler.PostBranchGenerated();

        curWorkState = "OARO_SquadState_Idle".Translate();
    }

    internal void PostLoadInit()
    {
        medalHandler.PostLoadInit();

        facilityHandler.PostLoadInit();
        buildingHandler.PostLoadInit();

        residentHandler.PostLoadInit();
        storesReserveHandler.PostLoadInit();
    }

    public string GetUniqueLoadID() => "Branch_" + loadID;
    public override string ToString() => "Branch_" + loadID;
}
