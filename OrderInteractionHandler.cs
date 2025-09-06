using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionHandler : IExposable, IOnRatkinOrderRemoved, IOnBranchDestoryed
{
    public static OrderInteractionHandler Instance { get; private set; }
    public static OrderCodePedestal MainOrderCodePedestal => Instance.mainOrderCodePedestal;
    public static int OrderHallLevel => Instance.mainOrderCodePedestal?.CachedOrderHallLevel ?? 0;
    public static CooldownRecordManager CooldownManager => Instance.cooldownManager;
    public static AcceptedBranchDemandHandler AcceptedBranchDemandHandler => Instance.acceptedBranchDemandHandler;
    public static ResidentKnightHandler ResidentKnightHandler => Instance.residentKnightHandler;
    public static AroundKnightGroupsManager AroundKnightGroupsManager => Instance.aroundKnightGroupsManager;


    [Unsaved] private OrderCodePedestal mainOrderCodePedestal;
    private CooldownRecordManager cooldownManager = new();

    private AcceptedBranchDemandHandler acceptedBranchDemandHandler = new();
    private ResidentKnightHandler residentKnightHandler = new();
    private AroundKnightGroupsManager aroundKnightGroupsManager = new();


    public OrderInteractionHandler()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;
    }

    public static void ClearStaticCache() => Instance = null;

    public void ExposeData()
    {
        Scribe_Deep.Look(ref acceptedBranchDemandHandler, "acceptedBranchDemandHandler");
        Scribe_Deep.Look(ref residentKnightHandler, "residentKnightHandler");
        Scribe_Deep.Look(ref aroundKnightGroupsManager, "aroundKnightGroupsManager");
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        acceptedBranchDemandHandler?.Notify_RatkinOrderRemoved(order);
        residentKnightHandler?.Notify_RatkinOrderRemoved(order);
    }

    public void Notify_BranchDestoryed(Branch branch)
    {
        acceptedBranchDemandHandler?.Notify_BranchDestoryed(branch);
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
