using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class GlobalInteractionManager : IExposable, IOnRatkinOrderRemoved, IOnBranchDestroyed
{
    public static GlobalInteractionManager Instance { get; private set; }
    public static CooldownRecordManager CooldownManager => Instance.cooldownManager;
    public static TagStrToFloat InteractionRecord => Instance.simpleInteractRecord;

    public static void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_GlobalOrderInteractHandler());

    private CooldownRecordManager cooldownManager;
    private TagStrToFloat simpleInteractRecord;

    private OrderHallHandler orderHallHandler;
    private AcceptedBranchDemandHandler acceptedBranchDemandHandler;

    private ResidentKnightsManager residentKnightsManager;
    private AroundKnightGroupsManager aroundKnightGroupsManager;
    private MercyQuestHandler mercyQuestHandler;

    public GlobalInteractionManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;

        cooldownManager = new();
        simpleInteractRecord = new();
    }

    public void StratNewGame()
    {
        orderHallHandler = new();
        acceptedBranchDemandHandler = new();
        residentKnightsManager = new();
        aroundKnightGroupsManager = new();
        mercyQuestHandler = new();
    }

    public void LoadedGame()
    {
        EnsureComponentsInit();
    }

    private void EnsureComponentsInit()
    {
        try
        {
            orderHallHandler ??= new OrderHallHandler();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "initializing OrderHallHandler",
                typeName: nameof(GlobalInteractionManager),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            OrderHallHandler.ClearStaticCache();
            orderHallHandler = new OrderHallHandler();
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
            residentKnightsManager ??= new ResidentKnightsManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "initializing ResidentKnightsManager",
                typeName: nameof(GlobalInteractionManager),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            ResidentKnightsManager.ClearStaticCache();
            residentKnightsManager = new ResidentKnightsManager();
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
        OrderHallHandler.ClearStaticCache();
        ResidentKnightsManager.ClearStaticCache();
        AroundKnightGroupsManager.ClearStaticCache();
        MercyQuestHandler.ClearStaticCache();
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref cooldownManager, "cooldownManager");
        Scribe_Deep.Look(ref simpleInteractRecord, "simpleInteractRecord");

        Scribe_Deep.Look(ref orderHallHandler, "orderHallHandler");
        Scribe_Deep.Look(ref acceptedBranchDemandHandler, "acceptedBranchDemandHandler");
        Scribe_Deep.Look(ref residentKnightsManager, "residentKnightsManager");
        Scribe_Deep.Look(ref aroundKnightGroupsManager, "aroundKnightGroupsManager");
        Scribe_Deep.Look(ref mercyQuestHandler, "mercyQuestHandler");
    }

    public void TickDay()
    {
        aroundKnightGroupsManager.TickDay();
        residentKnightsManager.TickDay();
        mercyQuestHandler.PeriodicTriggerMercyQuest();
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        acceptedBranchDemandHandler.Notify_RatkinOrderRemoved(order);
        residentKnightsManager.Notify_RatkinOrderRemoved(order);
        aroundKnightGroupsManager.Notify_RatkinOrderRemoved(order);
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        acceptedBranchDemandHandler.Notify_BranchDestroyed(branch);
        residentKnightsManager.Notify_BranchDestroyed(branch);
        aroundKnightGroupsManager.Notify_BranchDestroyed(branch);
    }
}
