using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class GameComponent_RatkinOrder : GameComponent
{
    public static GameComponent_RatkinOrder Instance { get; private set; }

    public OrderRelationshipKind InitOrderRelationship;

    private UniqueIDManager uniqueIDManager;

    private RatkinOrderManager ratkinOrderManager;
    private OrderLetterBox orderLetterBox;
    private OrderInteractionHandler orderInteractionHandler;

    [Unsaved] public Dictionary<Pawn, ITalkAction> TalkActionHandler = [];

    public GameComponent_RatkinOrder(Game game)
    {
        // GameComp比较特殊，没有找到合适的时机清理GameComp实例，所以不再检测直接替换
        // OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance)); 
        if (Instance != this)
        {
            Log.Message("GameComponent_RatkinOrder Instance switched.".Colorize(Color.cyan));
        }
        Instance = this;
    }

    public override void StartedNewGame()
    {
        EnsureComponentsInit();
        RatkinOrderGenerator.StartNewGame();
    }

    public override void GameComponentTick()
    {
        ratkinOrderManager.Tick();
    }

    private void EnsureComponentsInit()
    {
        try
        {
            uniqueIDManager ??= new UniqueIDManager();
        }
        catch
        {
            UniqueIDManager.ClearStaticCache();
            uniqueIDManager = new UniqueIDManager();
        }

        try
        {
            ratkinOrderManager ??= new RatkinOrderManager();
        }
        catch
        {
            RatkinOrderManager.ClearStaticCache();
            ratkinOrderManager = new RatkinOrderManager();
        }

        try
        {
            orderLetterBox ??= new OrderLetterBox();
        }
        catch
        {
            OrderLetterBox.ClearStaticCache();
            orderLetterBox = new OrderLetterBox();
        }

        try
        {
            orderInteractionHandler ??= new OrderInteractionHandler();
        }
        catch
        {
            OrderInteractionHandler.ClearStaticCache();
            orderInteractionHandler = new OrderInteractionHandler();
        }
    }

    private void PostLoadInit()
    {
        EnsureComponentsInit();

        ratkinOrderManager.PostLoadInit();
    }

    public override void GameComponentOnGUI()
    {
        base.GameComponentOnGUI();
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Deep.Look(ref uniqueIDManager, "uniqueIDManager");

        Scribe_Deep.Look(ref ratkinOrderManager, "ratkinOrderManager");
        Scribe_Deep.Look(ref orderLetterBox, "orderLetterBox");
        Scribe_Deep.Look(ref orderInteractionHandler, "orderInteractionHandler");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            PostLoadInit();
        }
    }
}
