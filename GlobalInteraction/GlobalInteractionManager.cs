using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class GlobalInteractionManager : IExposable, IOnRatkinOrderRemoved, IOnBranchDestroyed
{
    public static GlobalInteractionManager Instance { get; private set; }

    public static TagStrToFloat InteractionRecord => Instance.simpleInteractRecord;

    public static void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_GlobalOrderInteractHandler());

    [Unsaved] private readonly int tickHashOffset;


    private TagStrToFloat simpleInteractRecord;
    private AcceptedBranchDemandHandler acceptedBranchDemandHandler;

    private AroundKnightGroupsManager aroundKnightGroupsManager;
    private MercyQuestHandler mercyQuestHandler;

    private OrderLetterBox orderLetterBox;
    private SpecialLetterManager specialLetterManager;

    public GlobalInteractionManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;

        simpleInteractRecord = new();

        tickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
    }

    internal void EnsureComponentsInit()
    {
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

        try
        {
            orderLetterBox ??= new OrderLetterBox();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化骑士信箱 ({nameof(OrderLetterBox)})",
                typeName: nameof(GlobalInteractionManager),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            OrderLetterBox.ClearStaticCache();
            orderLetterBox = new OrderLetterBox();
        }

        specialLetterManager ??= new(initCtor: true);
    }

    public static void ClearStaticCache()
    {
        Instance = null;

        AcceptedBranchDemandHandler.ClearStaticCache();

        AroundKnightGroupsManager.ClearStaticCache();
        MercyQuestHandler.ClearStaticCache();

        OrderLetterBox.ClearStaticCache();
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref simpleInteractRecord, nameof(simpleInteractRecord));
        Scribe_Deep.Look(ref acceptedBranchDemandHandler, nameof(acceptedBranchDemandHandler));
        Scribe_Deep.Look(ref aroundKnightGroupsManager, nameof(aroundKnightGroupsManager));
        Scribe_Deep.Look(ref mercyQuestHandler, nameof(mercyQuestHandler));

        Scribe_Deep.Look(ref orderLetterBox, nameof(orderLetterBox));
        Scribe_Deep.Look(ref specialLetterManager, nameof(specialLetterManager), ctorArgs: false);
    }

    public void Notify_GameStart()
    {
        specialLetterManager.Notify_GameStart();
    }

    public void Tick()
    {
        if (TickUtility.IsHashIntervalTick(tickHashOffset, interval: 60000))
        {
            aroundKnightGroupsManager.TickDay();
            mercyQuestHandler.PeriodicTriggerMercyQuest();
        }

        orderLetterBox.Tick();
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        acceptedBranchDemandHandler.Notify_RatkinOrderRemoved(order);
        aroundKnightGroupsManager.Notify_RatkinOrderRemoved(order);
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        acceptedBranchDemandHandler.Notify_BranchDestroyed(branch);
        aroundKnightGroupsManager.Notify_BranchDestroyed(branch);
    }
}
