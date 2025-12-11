using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class GameComponent_RatkinOrder : GameComponent
{
    public static GameComponent_RatkinOrder Instance { get; private set; }

    public EsteemHandler.RelationshipKind InitOrderRelationship;

    private UniqueIDManager uniqueIDManager;

    private KnightPawnsManager knightPawnsManager;
    private RatkinOrderManager ratkinOrderManager;
    private OrderLetterBox orderLetterBox;
    private GlobalInteractionManager globalInteractionManager;

    /// <summary>
    /// 全局对话行为管理
    /// 因不保存，应注意重新注册
    /// </summary>
    public Dictionary<Pawn, ITalkAction> TalkActionHandler { get; } = [];

    /// <summary>
    /// GameComp比较特殊，没有找到合适的时机清理GameComp实例，所以不再检测直接替换实例
    /// </summary>
    /// <param name="game"></param>
    public GameComponent_RatkinOrder(Game game)
    {
        // OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance)); 
        if (Instance != this)
        {
            Log.Message("GameComponent_RatkinOrder Instance switched.".Colorize(Color.cyan));
        }
        Instance = this;
    }

    public static void ClearStaticCache()
    {
        UniqueIDManager.ClearStaticCache();

        RatkinOrderManager.ClearStaticCache();
        KnightPawnsManager.ClearStaticCache();
        OrderLetterBox.ClearStaticCache();
        GlobalInteractionManager.ClearStaticCache();
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref InitOrderRelationship, nameof(InitOrderRelationship));

        Scribe_Deep.Look(ref uniqueIDManager, nameof(uniqueIDManager));

        Scribe_Deep.Look(ref ratkinOrderManager, nameof(ratkinOrderManager));
        Scribe_Deep.Look(ref knightPawnsManager, nameof(knightPawnsManager));
        Scribe_Deep.Look(ref orderLetterBox, nameof(orderLetterBox));
        Scribe_Deep.Look(ref globalInteractionManager, nameof(globalInteractionManager));
    }

    public override void StartedNewGame()
    {
        EnsureComponentsInit();
        globalInteractionManager.StratNewGame();
        RatkinOrderGenerator.StartNewGame();
    }


    /// <summary>
    /// 会在PostLoadInit加载阶段之后调用
    /// </summary>
    public override void LoadedGame()
    {
        EnsureComponentsInit();
        globalInteractionManager.LoadedGame();
    }

    public override void GameComponentTick()
    {
        ratkinOrderManager.Tick();
    }

    /// <summary>
    /// 初始化各个组件，因包含简单单例，要注意每个游戏仅能调用一次
    /// 新游戏：StartedNewGame调用
    /// 加载存档：LoadedGame调用
    /// </summary>
    private void EnsureComponentsInit()
    {
        try
        {
            uniqueIDManager ??= new UniqueIDManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "initializing UniqueIDManager",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            UniqueIDManager.ClearStaticCache();
            uniqueIDManager = new UniqueIDManager();
        }

        try
        {
            knightPawnsManager ??= new KnightPawnsManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "initializing KnightPawnsManager",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            KnightPawnsManager.ClearStaticCache();
            knightPawnsManager = new KnightPawnsManager();
        }

        try
        {
            ratkinOrderManager ??= new RatkinOrderManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "initializing RatkinOrderManager",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            RatkinOrderManager.ClearStaticCache();
            ratkinOrderManager = new RatkinOrderManager();
        }

        try
        {
            orderLetterBox ??= new OrderLetterBox();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "initializing OrderLetterBox",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            OrderLetterBox.ClearStaticCache();
            orderLetterBox = new OrderLetterBox();
        }

        try
        {
            globalInteractionManager ??= new GlobalInteractionManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "initializing GlobalInteractionManager",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            GlobalInteractionManager.ClearStaticCache();
            globalInteractionManager = new GlobalInteractionManager();
        }
    }
}