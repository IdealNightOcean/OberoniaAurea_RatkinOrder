using OberoniaAurea_Frame;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class GameComponent_RatkinOrder : GameComponent
{
    public static GameComponent_RatkinOrder Instance { get; private set; }

    public EsteemHandler.RelationshipKind InitOrderRelationship;

    private UniqueIDManager uniqueIDManager;

    private CooldownRecordManager cooldownManager;
    public static CooldownRecordManager CooldownManager => Instance.cooldownManager;

    private PlayerDespawnedPawnsTempRetention playerDespawnedPawnsTempRetention;

    private RatkinOrderManager ratkinOrderManager;
    private KnightPawnsManager knightPawnsManager;
    private ResidentPawnsManager residentPawnsManager;
    private OrderStationHandler orderStationHandler;

    private GlobalInteractionManager globalInteractionManager;

    private AIInteractionHandler aiInteractionHandler;

    [Unsaved] private ValueCacheManager valueCacheManager;


    /// <summary>
    /// 全局对话行为管理
    /// 因不保存，应注意重新注册
    /// </summary>
    public Dictionary<Pawn, ITalkAction> TalkActionHandler { get; } = [];

    /// <summary>
    /// <see cref="GameComponent"/>比较特殊，没有找到合适的时机清理<see cref="GameComponent"/>实例，所以不再检测直接替换实例
    /// </summary>
    /// <param name="game"></param>
    public GameComponent_RatkinOrder(Game game)
    {
        // OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance)); 
        if (Instance != this)
        {
            Log.Message($"[OARO] {nameof(GameComponent_RatkinOrder)} 实例已正确切换。".Colorize(Color.cyan));
        }
        Instance = this;
    }

    public static void ClearStaticCache()
    {
        UniqueIDManager.ClearStaticCache();

        PlayerDespawnedPawnsTempRetention.ClearStaticCache();

        RatkinOrderManager.ClearStaticCache();
        KnightPawnsManager.ClearStaticCache();
        ResidentPawnsManager.ClearStaticCache();
        OrderStationHandler.ClearStaticCache();

        GlobalInteractionManager.ClearStaticCache();

        AIInteractionHandler.ClearStaticCache();

        ValueCacheManager.ClearStaticCache();
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref InitOrderRelationship, nameof(InitOrderRelationship));

        Scribe_Deep.Look(ref uniqueIDManager, nameof(uniqueIDManager));
        Scribe_Deep.Look(ref cooldownManager, nameof(cooldownManager));
        Scribe_Deep.Look(ref playerDespawnedPawnsTempRetention, nameof(playerDespawnedPawnsTempRetention));

        Scribe_Deep.Look(ref ratkinOrderManager, nameof(ratkinOrderManager));
        Scribe_Deep.Look(ref knightPawnsManager, nameof(knightPawnsManager));
        Scribe_Deep.Look(ref residentPawnsManager, nameof(residentPawnsManager), ctorArgs: false);
        Scribe_Deep.Look(ref orderStationHandler, nameof(orderStationHandler), ctorArgs: false);

        Scribe_Deep.Look(ref globalInteractionManager, nameof(globalInteractionManager));
    }

    public override void StartedNewGame()
    {
        EnsureComponentsInit();
        globalInteractionManager.EnsureComponentsInit();

        RatkinOrderGenerator.StartNewGame();
        globalInteractionManager.Notify_GameStart();
    }


    /// <summary>
    /// 会在PostLoadInit加载阶段之后调用
    /// </summary>
    public override void LoadedGame()
    {
        EnsureComponentsInit();
        globalInteractionManager.EnsureComponentsInit();
        globalInteractionManager.Notify_GameStart();
    }

    public override void GameComponentTick()
    {
        ratkinOrderManager.Tick();
        residentPawnsManager.Tick();
        globalInteractionManager.Tick();
    }

    /// <summary>
    /// 初始化各个组件，因包含简单单例，要注意每个游戏仅能调用一次
    /// 新游戏：StartedNewGame调用
    /// 加载存档：LoadedGame调用
    /// </summary>
    private void EnsureComponentsInit()
    {
        cooldownManager ??= new CooldownRecordManager();

        try
        {
            uniqueIDManager ??= new UniqueIDManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化唯一ID管理器 ({nameof(UniqueIDManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            UniqueIDManager.ClearStaticCache();
            uniqueIDManager = new UniqueIDManager();
        }

        try
        {
            playerDespawnedPawnsTempRetention ??= new PlayerDespawnedPawnsTempRetention();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化玩家已消失角色临时保留管理器 ({nameof(PlayerDespawnedPawnsTempRetention)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            PlayerDespawnedPawnsTempRetention.ClearStaticCache();
            playerDespawnedPawnsTempRetention = new PlayerDespawnedPawnsTempRetention();
        }

        try
        {
            ratkinOrderManager ??= new RatkinOrderManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化骑士团管理器 ({nameof(RatkinOrderManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            RatkinOrderManager.ClearStaticCache();
            ratkinOrderManager = new RatkinOrderManager();
        }

        try
        {
            knightPawnsManager ??= new KnightPawnsManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化骑士角色管理器 ({nameof(KnightPawnsManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            KnightPawnsManager.ClearStaticCache();
            knightPawnsManager = new KnightPawnsManager();
        }

        try
        {
            residentPawnsManager ??= new ResidentPawnsManager(initCtor: true);
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化常驻人员管理器 ({nameof(ResidentPawnsManager)})",
                typeName: nameof(GlobalInteractionManager),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            ResidentPawnsManager.ClearStaticCache();
            residentPawnsManager = new ResidentPawnsManager(initCtor: true);
        }

        try
        {
            orderStationHandler ??= new OrderStationHandler(initCtor: true);
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化骑士驻地管理器 ({nameof(OrderStationHandler)})",
                typeName: nameof(GlobalInteractionManager),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            OrderStationHandler.ClearStaticCache();
            orderStationHandler = new OrderStationHandler(initCtor: true);
        }

        try
        {
            globalInteractionManager ??= new GlobalInteractionManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化全局交互管理器 ({nameof(GlobalInteractionManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            GlobalInteractionManager.ClearStaticCache();
            globalInteractionManager = new GlobalInteractionManager();
        }

        try
        {
            aiInteractionHandler ??= new AIInteractionHandler();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化AI交互处理器 ({nameof(AIInteractionHandler)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            AIInteractionHandler.ClearStaticCache();
            aiInteractionHandler = new AIInteractionHandler();
        }

        try
        {
            valueCacheManager ??= new ValueCacheManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化值缓存管理器 ({nameof(ValueCacheManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            ValueCacheManager.ClearStaticCache();
            valueCacheManager = new ValueCacheManager();
        }
    }
}