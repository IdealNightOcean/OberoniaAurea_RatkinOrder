using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class GlobalInteractionManager : IExposable, IOnRatkinOrderRemoved, IOnBranchDestroyed
{
    public static GlobalInteractionManager Instance { get; private set; }
    public static CooldownRecordManager CooldownManager => Instance.cooldownManager;
    public static TagStrToFloat InteractionRecord => Instance.simpleInteractRecord;

    public static void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_GlobalOrderInteractHandler());

    [Unsaved] private readonly int tickHashOffset;

    private CooldownRecordManager cooldownManager;
    private TagStrToFloat simpleInteractRecord;

    private OrderStationHandler orderStationHandler;
    private AcceptedBranchDemandHandler acceptedBranchDemandHandler;

    private ResidentPawnsManager residentPawnsManager;
    private AroundKnightGroupsManager aroundKnightGroupsManager;
    private MercyQuestHandler mercyQuestHandler;

    public GlobalInteractionManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;

        cooldownManager = new();
        simpleInteractRecord = new();

        tickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
    }

    internal void EnsureComponentsInit()
    {
        try
        {
            orderStationHandler ??= new OrderStationHandler(initCtor: true);
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "initializing OrderHallHandler",
                typeName: nameof(GlobalInteractionManager),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            OrderStationHandler.ClearStaticCache();
            orderStationHandler = new OrderStationHandler(initCtor: true);
        }

        try
        {
            acceptedBranchDemandHandler ??= new AcceptedBranchDemandHandler();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "initializing AcceptedBranchDemandHandler",
                typeName: nameof(GlobalInteractionManager),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            AcceptedBranchDemandHandler.ClearStaticCache();
            acceptedBranchDemandHandler = new AcceptedBranchDemandHandler();
        }

        try
        {
            residentPawnsManager ??= new ResidentPawnsManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "initializing ResidentKnightsManager",
                typeName: nameof(GlobalInteractionManager),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            ResidentPawnsManager.ClearStaticCache();
            residentPawnsManager = new ResidentPawnsManager();
        }

        try
        {
            aroundKnightGroupsManager ??= new AroundKnightGroupsManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "initializing AroundKnightGroupsManager",
                typeName: nameof(GlobalInteractionManager),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            AroundKnightGroupsManager.ClearStaticCache();
            aroundKnightGroupsManager = new AroundKnightGroupsManager();
        }

        try
        {
            mercyQuestHandler ??= new MercyQuestHandler();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "initializing MercyQuestHandler",
                typeName: nameof(GlobalInteractionManager),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            MercyQuestHandler.ClearStaticCache();
            mercyQuestHandler = new MercyQuestHandler();
        }
    }

    public static void ClearStaticCache()
    {
        Instance = null;

        AcceptedBranchDemandHandler.ClearStaticCache();
        OrderStationHandler.ClearStaticCache();
        ResidentPawnsManager.ClearStaticCache();
        AroundKnightGroupsManager.ClearStaticCache();
        MercyQuestHandler.ClearStaticCache();
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref cooldownManager, nameof(cooldownManager));
        Scribe_Deep.Look(ref simpleInteractRecord, nameof(simpleInteractRecord));

        Scribe_Deep.Look(ref orderStationHandler, nameof(orderStationHandler), ctorArgs: false);
        Scribe_Deep.Look(ref acceptedBranchDemandHandler, nameof(acceptedBranchDemandHandler));
        Scribe_Deep.Look(ref residentPawnsManager, nameof(residentPawnsManager));
        Scribe_Deep.Look(ref aroundKnightGroupsManager, nameof(aroundKnightGroupsManager));
        Scribe_Deep.Look(ref mercyQuestHandler, nameof(mercyQuestHandler));
    }

    public void Tick()
    {
        if (TickUtility.IsHashIntervalTick(tickHashOffset, interval: 60000))
        {
            aroundKnightGroupsManager.TickDay();
            residentPawnsManager.TickDay();
            mercyQuestHandler.PeriodicTriggerMercyQuest();
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        acceptedBranchDemandHandler.Notify_RatkinOrderRemoved(order);
        residentPawnsManager.Notify_RatkinOrderRemoved(order);
        aroundKnightGroupsManager.Notify_RatkinOrderRemoved(order);
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        acceptedBranchDemandHandler.Notify_BranchDestroyed(branch);
        residentPawnsManager.Notify_BranchDestroyed(branch);
        aroundKnightGroupsManager.Notify_BranchDestroyed(branch);
    }
}
