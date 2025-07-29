using Verse;

namespace OberoniaAurea.RatkinOrder;

public class GameComponent_RatkinOrder : GameComponent
{
    public static GameComponent_RatkinOrder Instance { get; private set; }

    public EsteemHandler.RelationshipKind InitOrderRelationship;

    private UniqueIDManager uniqueIDManager;
    private RatkinOrderManager ratkinOrderManager;
    private OrderLetterBox orderLetterBox;

    public GameComponent_RatkinOrder(Game game)
    {
        Instance = this;
    }

    public override void StartedNewGame()
    {
        EnsureComponentsInit();
        RatkinOrderGenerator.StartNewGame();
    }

    private void EnsureComponentsInit()
    {
        uniqueIDManager ??= new UniqueIDManager();
        ratkinOrderManager ??= new RatkinOrderManager();
        orderLetterBox ??= new OrderLetterBox();
    }

    private void PostLoadInit()
    {
        EnsureComponentsInit();

        ratkinOrderManager.PostLoadInit();
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Deep.Look(ref uniqueIDManager, "uniqueIDManager");
        Scribe_Deep.Look(ref ratkinOrderManager, "ratkinOrderManager");
        Scribe_Deep.Look(ref orderLetterBox, "orderLetterBox");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            PostLoadInit();
        }
    }
}
