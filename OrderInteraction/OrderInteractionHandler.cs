using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionHandler : IExposable, IOnRatkinOrderRemoved, IOnBranchDestoryed
{
    public static OrderInteractionHandler Instance { get; private set; }
    public static OrderCodePedestal MainOrderCodePedestal => Instance.mainOrderCodePedestal;
    public static Room RatkinOrderHall => Instance.mainOrderCodePedestal?.CachedRoom;
    public static int OrderHallLevel => Instance.mainOrderCodePedestal?.CachedOrderHallLevel ?? 0;
    public static CooldownRecordManager CooldownManager => Instance.cooldownManager;
    public static TagStrToFloat InteractionRecord => Instance.simpleInteractRecord;
    public static AcceptedBranchDemandHandler AcceptedBranchDemandHandler => Instance.acceptedBranchDemandHandler;
    public static ResidentKnightsManager ResidentKnightsManager => Instance.residentKnightsManager;
    public static AroundKnightGroupsManager AroundKnightGroupsManager => Instance.aroundKnightGroupsManager;
    public static MercyQuestHandler MercyQuestHandler => Instance.mercyQuestHandler;

    public static void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_OrderInteractHandler());


    [Unsaved] private OrderCodePedestal mainOrderCodePedestal;
    private CooldownRecordManager cooldownManager;
    private TagStrToFloat simpleInteractRecord;

    private AcceptedBranchDemandHandler acceptedBranchDemandHandler;
    private ResidentKnightsManager residentKnightsManager;
    private AroundKnightGroupsManager aroundKnightGroupsManager;
    private MercyQuestHandler mercyQuestHandler;


    public OrderInteractionHandler()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;

        EnsureComponentsInit();
    }

    public static void ClearStaticCache() => Instance = null;

    private void EnsureComponentsInit()
    {
        cooldownManager ??= new();
        simpleInteractRecord ??= new(defaultValue: 0f, removeWhenDefault: false);

        acceptedBranchDemandHandler ??= new();
        residentKnightsManager ??= new();
        aroundKnightGroupsManager ??= new();
        mercyQuestHandler ??= new();
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref cooldownManager, "cooldownManager");
        Scribe_Deep.Look(ref simpleInteractRecord, "simpleInteractRecord");

        Scribe_Deep.Look(ref acceptedBranchDemandHandler, "acceptedBranchDemandHandler");
        Scribe_Deep.Look(ref residentKnightsManager, "residentKnightsManager");
        Scribe_Deep.Look(ref aroundKnightGroupsManager, "aroundKnightGroupsManager");
        Scribe_Deep.Look(ref mercyQuestHandler, "mercyQuestHandler");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            EnsureComponentsInit();
        }
    }

    public void TickDay()
    {
        aroundKnightGroupsManager.TickDay();
        mercyQuestHandler.PeriodicTriggerMercyQuest();

    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        acceptedBranchDemandHandler.Notify_RatkinOrderRemoved(order);
        residentKnightsManager.Notify_RatkinOrderRemoved(order);
        aroundKnightGroupsManager.Notify_RatkinOrderRemoved(order);
    }

    public void Notify_BranchDestoryed(Branch branch)
    {
        acceptedBranchDemandHandler.Notify_BranchDestoryed(branch);
        aroundKnightGroupsManager.Notify_BranchDestoryed(branch);
    }

    public bool SetMainOrderCodePedestal(OrderCodePedestal codePedestal, bool replaceCur)
    {
        if (codePedestal is null)
        {
            Log.Error("Cannot set null OrderCodePedestal as map main OrderCodePedestal.");
            return false;
        }
        if (!replaceCur && mainOrderCodePedestal is not null)
        {
            Messages.Message("OARO_MainOrderCodePedestal_RejectReplace".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }
        if (mainOrderCodePedestal == codePedestal)
        {
            return true;
        }
        mainOrderCodePedestal?.Notify_MainReplacedByOther();
        mainOrderCodePedestal = codePedestal;
        return true;
    }

    public void Notify_MainOrderCodePedestalUnset(OrderCodePedestal codePedestal)
    {
        if (mainOrderCodePedestal == codePedestal)
        {
            mainOrderCodePedestal = null;
            Messages.Message("OARO_MainOrderCodePedestal_Unset".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }
}
