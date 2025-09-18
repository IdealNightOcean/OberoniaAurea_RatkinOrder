using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Branch : IExposable, ILoadReferenceable, IPostLoadInit
{
    [Unsaved] public readonly BranchManager BranchManager;
    public RatkinOrder RatkinOrder => BranchManager.RatkinOrder;

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
    public BranchType BranchType => curType;

    [Unsaved] public readonly TagStrToBoolCountable EffectTags = new();
    [Unsaved] public readonly BranchStatTransformerHandler TransformerHandler = new();
    [Unsaved] public readonly SimpleUniqueList<IPostSquadCombatPawnGenerate> PostSquadCombatPawnGenerate = new(innerListLookMode: LookMode.Reference);
    private CooldownRecordManager cooldownManager;
    public CooldownRecordManager CooldownManager => cooldownManager;

    private Squad squad;
    private BranchFacilityHandler facilityHandler;
    private BranchBuildingHandler buildingHandler;
    private BranchDemandHandler demandHandler;
    private BranchResidentHandler residentHandler;
    private BranchStoresReserveHandler storesReserveHandler;

    public Squad Squad => squad;
    public BranchFacilityHandler FacilityHandler => facilityHandler;
    public BranchBuildingHandler BuildingHandler => buildingHandler;
    public BranchDemandHandler DemandHandler => demandHandler;
    public BranchResidentHandler ResidentHandler => residentHandler;
    public BranchStoresReserveHandler StoresReserveHandler => storesReserveHandler;

    private Branch(BranchManager branchManager)
    {
        BranchManager = branchManager ?? throw new ArgumentNullException(nameof(branchManager));
        TickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
    }

    private Branch(RatkinOrder order, WorldObject worldObject) : this(order.BranchManager)
    {
        if (worldObject?.GetComponent<WorldObjectComp_BranchSite>()?.SetBranch(this) is true)
        {
            this.worldObject = worldObject;
        }
        else
        {
            throw new ArgumentException("WorldObjectComp_BranchSite already has a branch assigned.", nameof(worldObject));
        }

        EnsureComponentsInit();

        loadID = UniqueIDManager.Instance.GetUniqueID("Branch");
    }

    public static Branch GenerateBranchFor(RatkinOrder order, WorldObject worldObject)
    {
        if (!BranchUtility.CanBeSiteForNewBranch(order, worldObject))
        {
            return null;
        }

        Branch branch;
        try
        {
            branch = new(order, worldObject);
            branch?.PostGenerated();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create a new branch for {order} at {worldObject}: " + ex);
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

        Scribe_Deep.Look(ref cooldownManager, "cooldownManager");
        Scribe_Deep.Look(ref squad, "squad", ctorArgs: [this, false]);
        Scribe_Deep.Look(ref facilityHandler, "facilityHandler", ctorArgs: this);
        Scribe_Deep.Look(ref buildingHandler, "buildingHandler", ctorArgs: this);
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
        squad.TickDay();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBranchOfType(BranchType type) => (curType & type) == type;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBranchType(BranchType type, bool active)
    {
        if (active)
        {
            curType |= type;
        }
        else
        {
            curType &= ~type;
        }
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

    public void RecacheIsHonor() => SetBranchType(BranchType.Honor, EffectTags.HasActiveTag(KeyLibrary_EffectTag.HonorBranch));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInAffectedRange(PlanetTile tile)
    {
        if (tile.Layer != worldObject.Tile.Layer)
        {
            return false;
        }
        return Find.WorldGrid.ApproxDistanceInTiles(worldObject.Tile, tile) <= BranchStatUtility.GetStatValue(this, BranchStatDefOf.OARO_AffectRadius);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DistanceTo(PlanetTile tile)
    {
        if (tile.Layer != worldObject.Tile.Layer)
        {
            return 999999f;
        }
        return Find.WorldGrid.ApproxDistanceInTiles(worldObject.Tile, tile);
    }

    public void Destroy()
    {
        residentHandler.ForceEndAllResidency();
        worldObject?.GetComponent<WorldObjectComp_BranchSite>()?.Notify_BranchDestroyed();
    }

    private void PostGenerated()
    {
        name = BranchUtility.GenerateBranchName(RatkinOrder);

        facilityHandler.PostBranchGenerated();
        buildingHandler.PostBranchGenerated();
        squad.PostBranchGenerated();
    }

    public void PostLoadInit()
    {
        EnsureComponentsInit();

        facilityHandler.PostLoadInit();
        buildingHandler.PostLoadInit();
        residentHandler.PostLoadInit();
        squad.PostLoadInit();

        RecacheIsHonor();
    }

    private void EnsureComponentsInit()
    {
        cooldownManager ??= new();

        squad ??= Squad.GenerateSquadForBranch(this) ?? throw new NullReferenceException(nameof(squad));

        facilityHandler ??= new(this);
        buildingHandler ??= new(this);
        demandHandler ??= new(this);
        residentHandler ??= new(this, initConstruct: true);
        storesReserveHandler ??= new(this);
    }

    public string GetUniqueLoadID() => "Branch_" + loadID;
}
