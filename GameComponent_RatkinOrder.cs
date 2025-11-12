using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class GameComponent_RatkinOrder : GameComponent
{
    public static GameComponent_RatkinOrder Instance { get; private set; }

    public EsteemHandler.RelationshipKind InitOrderRelationship;

    private UniqueIDManager uniqueIDManager;

    private RatkinOrderManager ratkinOrderManager;
    private OrderLetterBox orderLetterBox;
    private GlobalInteractionManager globalInteractionManager;

    /// <summary>
    /// 全局对话行为管理
    /// 因不保存，应注意重新注册
    /// </summary>
    [Unsaved] public Dictionary<Pawn, ITalkAction> TalkActionHandler = [];

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

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref InitOrderRelationship, "InitOrderRelationship");

        Scribe_Deep.Look(ref uniqueIDManager, "uniqueIDManager");

        Scribe_Deep.Look(ref ratkinOrderManager, "ratkinOrderManager");
        Scribe_Deep.Look(ref orderLetterBox, "orderLetterBox");
        Scribe_Deep.Look(ref globalInteractionManager, "globalInteractionManager");
    }

    public override void StartedNewGame()
    {
        EnsureComponentsInit();
        RatkinOrderGenerator.StartNewGame();
    }


    /// <summary>
    /// 会在PostLoadInit加载阶段之后调用
    /// </summary>
    public override void LoadedGame()
    {
        EnsureComponentsInit();
    }

    public override void GameComponentTick()
    {
        RatkinOrderManager.Tick();
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
            Log.Error($"Unexpected error when initializing UniqueIDManager: {ex.Message}");
            UniqueIDManager.ClearStaticCache();
            uniqueIDManager = new UniqueIDManager();
        }

        try
        {
            ratkinOrderManager ??= new RatkinOrderManager();
        }
        catch (System.Exception ex)
        {
            Log.Error($"Unexpected error when initializing RatkinOrderManager: {ex.Message}");
            RatkinOrderManager.ClearStaticCache();
            ratkinOrderManager = new RatkinOrderManager();
        }

        try
        {
            orderLetterBox ??= new OrderLetterBox();
        }
        catch (System.Exception ex)
        {
            Log.Error($"Unexpected error when initializing OrderLetterBox: {ex.Message}");
            OrderLetterBox.ClearStaticCache();
            orderLetterBox = new OrderLetterBox();
        }

        try
        {
            globalInteractionManager ??= new GlobalInteractionManager();
        }
        catch (System.Exception ex)
        {
            Log.Error($"Unexpected error when initializing GlobalInteractionManager: {ex.Message}");
            GlobalInteractionManager.ClearStaticCache();
            globalInteractionManager = new GlobalInteractionManager();
        }
    }
}
