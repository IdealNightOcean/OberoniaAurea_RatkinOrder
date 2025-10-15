using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

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

    [Unsaved] public int TickHashOffset;

    private string name;
    public string Name => name;
    public string NameFull => RatkinOrder.Name + "-" + name;

    private WorldObject worldObject;
    public WorldObject WorldObject => worldObject;
    public int Tile => worldObject?.Tile ?? -1;

    protected int friendlyExpiredTick = -1;
    private BranchType curType = BranchType.Normal;
    public BranchType CurType => curType;

    private HonorBranchProperties honorProperties;
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

    private int population;
    private int NaturalPopulationCeiling => (int)this.GetStatValue(BranchStatDefOf.OARO_NaturalPopulationCeiling);
    public int Population
    {
        get => population;
        set => population = Math.Max(0, value);
    }

    [Unsaved] public readonly TagStrToBoolCountable EffectTags = new();
    [Unsaved] public readonly BranchStatTransformerHandler TransformerHandler = new();
    [Unsaved] public readonly List<IPostSquadCombatPawnGenerate> PostSquadCombatPawnGenerate = [];
    private CooldownRecordManager cooldownManager;
    public CooldownRecordManager CooldownManager => cooldownManager;

    private BranchSquad squad;
    private BranchMedalHandler medalHandler;
    private BranchFacilityHandler facilityHandler;
    private BranchBuildingHandler buildingHandler;
    private BranchTaskHandler taskHandler;
    private BranchDemandHandler demandHandler;
    private BranchResidentHandler residentHandler;
    private BranchStoresReserveHandler storesReserveHandler;

    public BranchSquad Squad => squad;
    public BranchMedalHandler MedalHandler => medalHandler;
    public BranchFacilityHandler FacilityHandler => facilityHandler;
    public BranchBuildingHandler BuildingHandler => buildingHandler;
    public BranchTaskHandler TaskHandler => taskHandler;
    public BranchDemandHandler DemandHandler => demandHandler;
    public BranchResidentHandler ResidentHandler => residentHandler;
    public BranchStoresReserveHandler StoresReserveHandler => storesReserveHandler;

    private Branch(RatkinOrder ratkinOrder, bool initCtor)
    {
        RatkinOrder = ratkinOrder ?? throw new NullReferenceException(nameof(ratkinOrder));
        TickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
        supplyCeilingCache = new(cacheInterval: 60000, defaultValue: BranchStatDefOf.OARO_BranchSupplyCeiling.baseValue, () => this.GetStatValue(BranchStatDefOf.OARO_BranchSupplyCeiling));

        if (initCtor)
        {
            cooldownManager = new();

            squad = BranchSquad.GenerateSquadForBranch(this) ?? throw new NullReferenceException(nameof(squad));

            medalHandler = new();
            facilityHandler = new(this);
            buildingHandler = new(this);
            taskHandler = new(this);
            demandHandler = new(this);
            residentHandler = new(this);
            storesReserveHandler = new(this);
        }
        loadID = UniqueIDManager.Instance.GetUniqueID("Branch");
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
            branch.worldObject = worldObject;
            branch.name = BranchUtility.GenerateBranchName(ratkinOrder);
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
        Scribe_References.Look(ref worldObject, "worldObject");

        Scribe_Values.Look(ref friendlyExpiredTick, "friendlyExpiredTick", 0);
        Scribe_Values.Look(ref curType, "curType", BranchType.Normal);

        Scribe_Values.Look(ref supply, "supply", 0f);

        Scribe_Deep.Look(ref cooldownManager, "cooldownManager");
        Scribe_Deep.Look(ref squad, "squad", ctorArgs: [this, false]);
        Scribe_Deep.Look(ref medalHandler, "medalHandler");
        Scribe_Deep.Look(ref facilityHandler, "facilityHandler", ctorArgs: this);
        Scribe_Deep.Look(ref buildingHandler, "buildingHandler", ctorArgs: this);
        Scribe_Deep.Look(ref taskHandler, "taskHandler", ctorArgs: this);
        Scribe_Deep.Look(ref demandHandler, "demandHandler", ctorArgs: this);
        Scribe_Deep.Look(ref residentHandler, "residentHandler", ctorArgs: this);
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
        int hourOfDay = GenLocalDate.HourOfDay(worldObject.Tile);

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
    }

    private void TickDay()
    {
        buildingHandler.TickDay();
        residentHandler.TickDay();
        demandHandler.TickDay();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBranchOfType(BranchType type) => (curType & type) == type;

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

    public void Destroy()
    {
        residentHandler.ForceEndAllResidency();
        worldObject?.GetComponent<WorldObjectComp_BranchSite>()?.Notify_BranchDestroyed(this);
    }

    /// <summary>
    /// 每日人口变化
    /// </summary>
    private int GetDailyPopulationDecline()
    {
        int naturalPopulationCeiling = NaturalPopulationCeiling;
        float populationRatio = population / (float)naturalPopulationCeiling;


        return 0;
    }

    private void PostGenerated()
    {
        medalHandler.PostBranchGenerated();

        facilityHandler.PostBranchGenerated();
        buildingHandler.PostBranchGenerated();

        residentHandler.PostBranchGenerated();
        squad.PostBranchGenerated();
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
