using OberoniaAurea.RatkinOrder.Utility;
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
    private OrderStationHandler orderStationHandler;

    private ResidentPawnsManager residentPawnsManager;
    private ResidentRoleManager residentRoleManager;
    private MentorshipManager mentorshipManager;

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
        // OberoniaAurea_Frame.Utility.OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance)); 
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
        OrderStationHandler.ClearStaticCache();

        ResidentPawnsManager.ClearStaticCache();
        ResidentRoleManager.ClearStaticCache();
        MentorshipManager.ClearStaticCache();

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
        Scribe_Deep.Look(ref orderStationHandler, nameof(orderStationHandler), ctorArgs: false);

        Scribe_Deep.Look(ref residentPawnsManager, nameof(residentPawnsManager));
        Scribe_Deep.Look(ref residentRoleManager, nameof(residentRoleManager));
        Scribe_Deep.Look(ref mentorshipManager, nameof(mentorshipManager));

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
        try
        {
            ratkinOrderManager.Tick();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"骑士团管理器Tick ({nameof(RatkinOrderManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(GameComponentTick),
                needStackTrace: true);
        }

        try
        {
            residentPawnsManager.Tick();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"常驻人员管理器Tick ({nameof(ResidentPawnsManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(GameComponentTick),
                needStackTrace: true);
        }

        try
        {
            residentRoleManager.Tick();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"常驻人员职位管理器Tick ({nameof(ResidentRoleManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(GameComponentTick),
                needStackTrace: true);
        }

        try
        {
            mentorshipManager.Tick();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"教导关系管理器Tick ({nameof(MentorshipManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(GameComponentTick),
                needStackTrace: true);
        }

        try
        {
            globalInteractionManager.Tick();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"全局交互管理器Tick ({nameof(GlobalInteractionManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(GameComponentTick),
                needStackTrace: true);
        }
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
            orderStationHandler ??= new OrderStationHandler(initCtor: true);
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化骑士驻地管理器 ({nameof(OrderStationHandler)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            OrderStationHandler.ClearStaticCache();
            orderStationHandler = new OrderStationHandler(initCtor: true);
        }

        try
        {
            residentPawnsManager ??= new ResidentPawnsManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化常驻人员管理器 ({nameof(ResidentPawnsManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            ResidentPawnsManager.ClearStaticCache();
            residentPawnsManager = new ResidentPawnsManager();
        }

        try
        {
            residentRoleManager ??= new ResidentRoleManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化常驻人员职位管理器 ({nameof(ResidentRoleManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            ResidentRoleManager.ClearStaticCache();
            residentRoleManager = new ResidentRoleManager();
        }

        try
        {
            mentorshipManager ??= new MentorshipManager();
        }
        catch (System.Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"初始化常驻人员教导关系管理器 ({nameof(MentorshipManager)})",
                typeName: nameof(GameComponent_RatkinOrder),
                methodName: nameof(EnsureComponentsInit),
                needStackTrace: true);
            MentorshipManager.ClearStaticCache();
            mentorshipManager = new MentorshipManager();
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