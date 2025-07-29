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
    [Unsaved] public readonly RatkinOrder RatkinOrder;

    private int loadID = -1;

    private WorldObject worldObject;
    public WorldObject WorldObject => worldObject;
    public int Tile => worldObject?.Tile ?? -1;

    protected int friendlyExpiredTick = -1;
    private BranchType curType = BranchType.Normal;
    public BranchType BranchType => curType;

    [Unsaved] public readonly TagStrToBoolCountable EffectTags = new();
    [Unsaved] public readonly BranchStatTransformerHandler TransformerHandler = new();
    [Unsaved] public readonly SimpleUniqueList<IPostSquadCombatPawnGenerate> PostSquadCombatPawnGenerate = new(innerListLookMode: LookMode.Reference);

    private Squad squad;
    private BranchFacilityHandler facilityHandler;
    private BranchBuildingHandler buildingHandler;
    private BranchResidentHandler residentHandler;
    private BranchStoresReserveHandler storesReserveHandler;

    public Squad Squad => squad;
    public BranchFacilityHandler FacilityHandler => facilityHandler;
    public BranchBuildingHandler BuildingHandler => buildingHandler;
    public BranchResidentHandler ResidentHandler => residentHandler;
    public BranchStoresReserveHandler StoresReserveHandler => storesReserveHandler;

    private Branch(BranchManager branchManager)
    {
        BranchManager = branchManager ?? throw new ArgumentNullException(nameof(branchManager));
        RatkinOrder = branchManager.RatkinOrder ?? throw new NullReferenceException(nameof(RatkinOrder));
    }

    private Branch(RatkinOrder order, WorldObject worldObject)
    {
        RatkinOrder = order ?? throw new ArgumentNullException(nameof(order));
        BranchManager = order.BranchManager ?? throw new NullReferenceException(nameof(BranchManager));

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

    public void TickHour()
    {
        int hourOfDay = GenLocalDate.HourOfDay(worldObject.Tile);
        buildingHandler.TickHour(hourOfDay);

        if (!buildingHandler.IsBusy && !facilityHandler.IsBusy)
        {
            storesReserveHandler.TickHour(hourOfDay);
        }

        if (friendlyExpiredTick > 0 && (friendlyExpiredTick -= 2500) <= 0)
        {
            SetFriendly(false);
        }
    }

    public void TickDay()
    {
        buildingHandler.TickDay();
        residentHandler.TickDay();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBranchOfType(BranchType type)
    {
        return (curType & type) == type;
    }

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

    public void SetFriendly(bool friendly, int durationTick = 0)
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

    public void RecacheIsHonor()
    {
        SetBranchType(BranchType.Honor, EffectTags.HasActiveTag("HonorBranch"));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInAffectedRange(int tile)
    {
        return Find.WorldGrid.ApproxDistanceInTiles(worldObject.Tile, tile) <= BranchStatUtility.GetStatValue(this, BranchStatDefOf.OARO_AffectRadius);
    }

    public float DistanceTo(int tile)
    {
        return Find.WorldGrid.ApproxDistanceInTiles(worldObject.Tile, tile);
    }

    public void Destroy()
    {
        squad = null;
        worldObject?.GetComponent<WorldObjectComp_BranchSite>()?.Notify_BranchDestroyed();
    }

    private void PostGenerated()
    {
        facilityHandler.PostBranchGenerated();
        buildingHandler.PostBranchGenerated();
    }

    public void PostLoadInit()
    {
        EnsureComponentsInit();

        facilityHandler.PostLoadInit();
        buildingHandler.PostLoadInit();
        residentHandler.PostLoadInit();

        RecacheIsHonor();
    }

    private void EnsureComponentsInit()
    {
        squad ??= Squad.GenerateSquadForBranch(this) ?? throw new NullReferenceException(nameof(squad));
        facilityHandler ??= new(this);
        buildingHandler ??= new(this);
        residentHandler ??= new(this, initConstruct: true);
        storesReserveHandler ??= new(this);
    }

    public string GetUniqueLoadID()
    {
        return "Branch_" + loadID;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref loadID, "loadID", -1);

        Scribe_References.Look(ref worldObject, "worldObject");

        Scribe_Values.Look(ref friendlyExpiredTick, "friendlyExpiredTick", 0);
        Scribe_Values.Look(ref curType, "curType", BranchType.Normal);

        Scribe_Deep.Look(ref squad, "squad", ctorArgs: [this, false]);
        Scribe_Deep.Look(ref facilityHandler, "facilityHandler", ctorArgs: this);
        Scribe_Deep.Look(ref buildingHandler, "buildingHandler", ctorArgs: this);
        Scribe_Deep.Look(ref residentHandler, "residentHandler", ctorArgs: [this, false]);
        Scribe_Deep.Look(ref storesReserveHandler, "storesReserveHandler", ctorArgs: this);
    }
}
