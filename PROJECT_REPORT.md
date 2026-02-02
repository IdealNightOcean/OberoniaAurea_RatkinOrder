# OA鼠族骑士团 (OberoniaAurea_RatkinOrder) 项目报告

## 项目概述

OA鼠族骑士团是一个基于 **RimWorld（环世界）** 游戏的大型扩展模组（Mod），为游戏添加了完整的鼠族骑士团派系系统。该模组允许玩家与鼠族骑士团建立关系，管理骑士团分部，招募常驻骑士，执行任务，并与骑士团进行各种互动。模组基于作者自研的 **金鸢尾兰系列框架（OberoniaAurea_Frame）** 构建，具有高度模块化的设计。

本模组的核心玩法围绕骑士团的组织架构展开，包括分部的建设与管理、骑士的招募与培养、任务的接受与完成、以及与其他派系的外交互动。模组引入了完整的骑士等级体系、荣誉系统、传统系统、建筑设施系统等复杂机制，为RimWorld游戏增添了深度的派系管理体验。

项目采用 **CC BY-NC-SA 4.0** 许可证，意味着可以自由分享和演绎，但必须署名、非商业性使用，且衍生作品需采用相同许可证。

## 技术栈与依赖

本项目是一个使用 **C#** 编写的RimWorld模组，目标框架为 **.NET 4.8**，采用传统的MSBuild构建系统。以下是项目所依赖的核心库和框架：

- **RimWorld游戏核心库 (Assembly-CSharp)**：RimWorld游戏的主要程序集，提供游戏基本框架、派系系统、工作机制等核心功能。
- **0Harmony (v2.x)**：RimWorld模组社区广泛使用的补丁库，用于实现代码注入和方法拦截，是大多数现代模组的基础依赖。
- **NewRatkin (OARK)**：鼠族种族扩展模组，提供了鼠族派系的基础设定、本模组的依赖基础，支持岩鼠、旅行鼠等变种鼠族派系。
- **NightOcean**：作者的另一个模组，可能提供额外的功能支持或AI交互功能。
- **OberoniaAurea_Frame**：作者自研的框架库，提供通用的模组开发工具、数据结构、扩展方法等基础设施，本模组的核心架构基于该框架构建。
- **UnityEngine模块**：使用Unity引擎的CoreModule、IMGUIModule和TextRenderingModule用于UI渲染和输入处理。

项目配置为双构建配置：
- **Debug配置**：输出到本地RimWorld Mods目录的Assemblies子目录，不优化代码，包含调试符号
- **Release配置**：输出到相同目录，启用代码优化，用于发布版本

## 项目结构

```
OberoniaAurea_RatkinOrder/
├── AIInteraction/                    # AI交互相关功能
│   ├── AIInteractionHandler.cs       # AI交互处理器
│   ├── AIInteractionUtility.cs       # AI交互工具类
│   └── DecoratePromptUtility.cs      # Prompt装饰工具
├── Ability/                          # 能力组件
│   └── AbilityComp_GiveRatkinHediff.cs  # 给予鼠族状态的效果组件
├── Branch/                           # 分部核心系统
│   ├── Branch.cs                     # 分部基础类
│   ├── BranchContract.cs             # 分部合约系统
│   ├── BranchContractRewardWorker.cs # 合约奖励处理器
│   ├── BranchMedalHandler.cs         # 分部勋章处理器
│   ├── BranchMedalRecord.cs          # 勋章记录
│   ├── BranchPopulationHandler.cs    # 人口处理器
│   ├── BranchSquad.cs                # 分部小队系统
│   ├── BranchStoresReserveHandler.cs # 储备处理器
│   └── UnderConstructionRecord.cs    # 在建记录
├── BranchBuilding/                   # 分部建筑系统
│   ├── BranchBuilding.cs             # 建筑基础类
│   ├── BranchBuildingComp.cs         # 建筑组件基类
│   ├── BranchBuildingHandler.cs      # 建筑处理器
│   ├── BranchBuilding_CommanderOffice.cs  # 指挥官办公室
│   ├── BranchBuilding_OberoniaConferenceHall.cs  # 金鸢尾兰洽谈所
│   ├── BranchBuilding_Trader.cs      # 交易建筑
│   └── BranchBuildingConstructChecker.cs  # 建造检查器
├── BranchDemand/                     # 分部需求系统
│   ├── BranchDemand.cs               # 需求基础类
│   ├── BranchDemandHandler.cs        # 需求处理器
│   ├── BranchDemandWeighter.cs       # 需求权重计算器
│   ├── BranchDemandWeighter_*.cs     # 各类需求权重策略
│   └── BranchDemand_Critical.cs      # 关键需求
├── BranchFacility/                   # 分部设施系统
│   ├── BranchFacilityHandler.cs      # 设施处理器
│   ├── BranchFacilityLevel.cs        # 设施等级
│   └── BranchFacilityLevelStage.cs   # 设施等级阶段
├── BranchInteraction/                # 分部交互系统
│   ├── BranchInteractionWorker.cs    # 交互工作器基类
│   ├── BranchInteractionParms.cs     # 交互参数
│   ├── CaravanInteraction/           # 商队交互
│   ├── MapInteraction/               # 地图交互
│   └── TargetlessInteraction/        # 无目标交互
├── BranchResident/                   # 常驻骑士系统
│   ├── BranchResident.cs             # 常驻骑士基础类
│   ├── BranchResidentHandler.cs      # 常驻骑士处理器
│   ├── BranchResident_Deployment.cs  # 部署系统
│   └── BranchResident_CaravanMedicalAssistance.cs  # 医疗援助
├── BranchStat/                       # 分部属性系统
│   ├── BranchStatPart_*.cs           # 各类属性修正器
│   ├── BranchStatTransformer.cs      # 属性转换器
│   └── BranchStatWorker.cs           # 属性处理器
├── BranchTask/                       # 分部任务系统
│   ├── BranchTask.cs                 # 任务基础类
│   ├── BranchTaskHandler.cs          # 任务处理器
│   ├── BranchTaskType.cs             # 任务类型
│   └── BranchTask_JurisdictionDuty.cs  # 司法职责任务
├── BranchTradition/                  # 分部传统系统
│   ├── BranchTradition.cs            # 传统基础类
│   ├── BranchTraditionHandler.cs     # 传统处理器
│   └── BranchTraditionStage.cs       # 传统阶段
├── Components/                       # 通用组件
│   ├── KnightRecord.cs               # 骑士记录
│   ├── QuestEffectTag.cs             # 任务效果标签
│   ├── TrackedFloatValue.cs          # 追踪浮点值
│   └── TrackedIntValue.cs            # 追踪整数值
├── DataLibrary/                      # 数据库
│   ├── EnumArraryLibrary.cs          # 枚举数组库
│   ├── IconLibrary.cs                # 图标库
│   ├── KeyLibrary_*.cs               # 各类键值库
├── Def/                              # 定义类（数据定义，通过XML实例化）
│   ├── BranchBuildingDef.cs          # 建筑数据定义（继承自BranchConstructionDef）
│   ├── BranchConstructionDef.cs      # 建造定义基类（抽象类，继承自Def）
│   ├── BranchContractDef.cs          # 合约数据定义（继承自Def）
│   ├── BranchDemandDef.cs            # 需求数据定义（继承自Def）
│   ├── BranchFacilityDef.cs          # 设施数据定义（继承自BranchConstructionDef）
│   ├── BranchHonorDef.cs             # 荣誉数据定义（继承自Def）
│   ├── BranchInteractionDef.cs       # 分部交互定义（继承自InteractionDefBase）
│   ├── BranchMedalDef.cs             # 勋章数据定义（继承自Def）
│   ├── BranchResidentDef.cs          # 常驻骑士数据定义（继承自Def）
│   ├── BranchStatDef.cs              # 属性数据定义（继承自Def）
│   ├── BranchTaskDef.cs              # 任务数据定义（继承自Def）
│   ├── BranchTraditionDef.cs         # 传统数据定义（继承自Def）
│   ├── CertainDateLetterDef.cs       # 特定日期信件（继承自SpecialLetterDefBase）
│   ├── InteractionDefBase.cs         # 交互定义基类（抽象类，继承自Def）
│   ├── JointPatrolCaravanHelpDef.cs  # 联合巡逻援助定义（继承自JointPatrolInteractionDef）
│   ├── JointPatrolIncidentDef.cs     # 联合巡逻事件定义（继承自JointPatrolInteractionDef）
│   ├── JointPatrolInteractionDef.cs  # 联合巡逻交互定义基类（抽象类，继承自Def）
│   ├── MercyQuestDef.cs              # 善行任务数据定义（继承自Def）
│   ├── OrderFundEventDef.cs          # 资金事件定义（继承自Def）
│   ├── OrderInteractionDef.cs        # 骑士团交互定义（继承自InteractionDefBase）
│   ├── OrderLetterDef.cs             # 骑士团信件定义（继承自Def）
│   ├── OrderReformationDef.cs        # 自新定义（继承自Def）
│   ├── RatkinOrderDef.cs             # 骑士团数据定义（继承自Def）
│   ├── ResidentKnightAcademicDef.cs  # 常驻骑士课业定义（继承自Def）
│   ├── ResidentKnightRoleDef.cs      # 常驻骑士角色定义（继承自Def）
│   └── SpecialGameLetterDef.cs       # 特殊游戏信件定义基类（包含DailyOrderLetterDef、SpecialGameLetterDef）
├── DefModExtension/                  # 定义扩展（为现有Def添加额外属性）
│   ├── CriticalDemand_Extension.cs   # 关键需求扩展
│   ├── OrderInteraction_*.cs         # 交互扩展
│   └── RatkinOrderFactionExtension.cs  # 派系扩展
├── DefOf/                            # 定义静态访问器（通过静态字段引用Def实例）
│   ├── ModDefOf.cs                   # 模组定义访问器
│   ├── Branch*.cs                    # 分部定义访问器
│   ├── Order*.cs                     # 骑士团定义访问器
│   └── RimWorldDefOf.cs              # RimWorld定义访问器
├── GlobalInteraction/                # 全局交互系统
│   ├── GlobalInteractionManager.cs   # 全局交互管理器
│   ├── AcceptedBranchDemand.cs       # 已接受需求
│   ├── MercyQuestHandler.cs          # 善行任务处理器
│   ├── AroundKnightGroup.cs          # 周围骑士组
│   └── ResidentKnight/               # 常驻骑士模块
│       ├── ResidentKnightsManager.cs # 常驻骑士管理器
│       ├── ResidentKnightRecord.cs   # 骑士记录
│       └── ResidentKnightRoleWorker.cs  # 骑士角色处理器
├── HarmonyPatch/                     # Harmony补丁
│   ├── ModHarmonyPatch.cs            # 模组补丁入口
│   ├── Game_ClearCaches_Patch.cs     # 缓存清理补丁
│   └── *_Patch.cs                    # 各类功能补丁
├── Hediff/                           # 状态效果
│   ├── Hediff_BranchMedal.cs         # 分部勋章效果
│   ├── Hediff_Honor*.cs              # 荣誉相关效果
│   ├── Hediff_RecruitKnight.cs       # 招募骑士效果
│   └── Hediff_ResidentAcademicBuff.cs  # 课业加成效果
├── IncidentWorker/                   # 事件工作器
│   └── IncidentWorker_RaidNobilityTerritory.cs  # 贵族领地袭击
├── Interface/                        # 接口定义（大量使用接口实现解耦，较为少见的设计）
│   ├── IAttachments.cs               # 附件接口（管理物品附件）
│   ├── IBranchRelated.cs             # 分部相关接口（IOnBranchDestroyed、ISingleBranchRelated）
│   ├── IJointPatrolCaravanHelpSite.cs # 联合巡逻援助站点接口
│   ├── IPostCombatantGenerate.cs     # 单位生成后接口（用于自定义骑士生成逻辑）
│   ├── IRatkinOrderRelated.cs        # 骑士团相关接口（IOnRatkinOrderRemoved、ISingleRatkinOrderRelated）
│   ├── IThingRequester.cs            # 物品请求接口（请求交付物品）
│   ├── ITalkAction.cs                # 对话行为接口（自定义对话逻辑）
│   └── ITicks.cs                     # 定时接口（ITickDay、ITickHour等）
├── Job/                              # 工作驱动
│   ├── JobDriver_BookcaseReading.cs  # 读书
│   ├── JobDriver_RepairHowitzer.cs   # 修理榴弹炮
│   └── *.cs                          # 其他工作类型
├── JointPatrol/                      # 联合巡逻系统
│   ├── JointPatrolManager.cs         # 巡逻管理器
│   ├── JointBranchRecord.cs          # 联合分支记录
│   └── JointPatrolCaravanHelp/       # 商队援助
│       ├── JointPatrolCaravanHelpWorker.cs  # 援助工作器
│       └── JointPatrolCaravanHelpWorker_*.cs  # 各类援助
├── Letter/                           # 信件系统
│   ├── ChoiceLetter_*.cs             # 选择类信件
│   └── OrderLetter_*.cs              # 骑士团信件
├── Lord/                             # 任务系统
│   └── LordJob_ExitMapBestForJointPatrol.cs  # 联合巡逻任务
├── MercyQuest/                       # 善行任务系统
│   ├── MercyQuestDef.cs              # 善行任务定义
│   ├── MercyQuestHandler.cs          # 善行任务处理器
│   └── MercyQuestParentFactionFinder.cs  # 派系查找器
├── OrderInteraction/                 # 骑士团交互
│   └── OrderInteractionWorker_*.cs   # 各类交互工作器
├── OrderLetter/                      # 骑士团信件
│   └── OrderLetter_*.cs              # 各类信件
├── Quest/                            # 任务系统
│   ├── Quest_*.cs                    # 各类任务
│   ├── QuestNode_*.cs                # 任务节点
│   ├── QuestPart_*.cs                # 任务部分
│   └── QuestClique/                  # 任务群系统
│       └── Quest_CliquesManager.cs   # 任务群管理器
├── Reformation/                      # 自新系统
│   └── ReformationManager.cs         # 自新管理器
├── Thing/                            # 物品
│   └── BombardSupportMaker.cs        # 炮火支援制造器
├── Utility/                          # 工具类（大量静态工具类，提供通用功能）
│   ├── ModUtility.cs                 # 模组工具类（安全销毁、日志、异常处理等）
│   ├── BranchUtility.cs              # 分部工具类（验证、范围检测等）
│   ├── BranchDemandUtility.cs        # 需求工具类（需求过滤、验证等）
│   ├── BranchStatUtility.cs          # 属性工具类（属性计算、解释生成等）
│   ├── BranchSupportUtility.cs       # 支援工具类（支援等级计算等）
│   ├── BranchConstructUtility.cs     # 建造工具类（建造检查等）
│   ├── KnightGenerateUtility.cs      # 骑士生成工具类（生成骑士Pawn）
│   ├── KnightPersonalityUtility.cs   # 骑士人设工具类（人设管理）
│   ├── GlobalInteractionUtility.cs   # 全局交互工具类（交互逻辑）
│   ├── RelationshipUtility.cs        # 关系工具类（派系关系等级转换）
│   ├── EsteemUtility.cs              # 声望工具类（声望计算）
│   ├── AcademicUtility.cs            # 学术工具类（学术相关）
│   ├── OrderHallUtility.cs           # 大厅工具类（大厅功能）
│   ├── OrderLetterUtility.cs         # 信件工具类（信件处理）
│   ├── QuestUtility.cs               # 任务工具类（任务逻辑）
│   ├── RecommendationUtility.cs      # 推荐工具类（推荐系统）
│   ├── PawnUtility.cs                # 单位工具类（单位操作）
│   ├── MapUtility.cs                 # 地图工具类（地图相关）
│   ├── TalkActionUtility.cs          # 对话行为工具类
│   ├── TickUtility.cs                # 时间工具类（时间转换、哈希间隔）
│   ├── WindowUtility.cs              # 窗口工具类（窗口操作）
│   └── DebugRatkinOrders.cs          # 调试工具类（DebugAction调试菜单）
├── Window/                           # 窗口界面系统
│   ├── MainTabWindow_RatkinOrder.cs  # 骑士团总览主标签页窗口
│   ├── OrderWindowBase.cs            # 骑士团窗口基类（统一风格和交互）
│   ├── Window_RatkinOrder.cs         # 骑士团总览窗口
│   ├── Window_OrderHall.cs           # 大厅窗口（管理骑士团设施等级）
│   ├── Window_Branch.cs              # 分部详情窗口
│   ├── Window_BranchList.cs          # 分部列表窗口
│   ├── Window_BranchDemand.cs        # 分部需求窗口
│   ├── Window_BranchTask.cs          # 分部任务窗口
│   ├── Window_BranchSquad.cs         # 分部小队窗口
│   ├── Window_OrderLetterBox.cs      # 信箱窗口
│   ├── Window_LetterBoxSetting.cs    # 信箱设置窗口
│   ├── Window_QuestClique.cs         # 关键需求任务派别窗口
│   ├── Window_ResidentKnight_AcademicArrange.cs  # 常驻骑士课业安排窗口
│   ├── Window_ResidentKnight_RankUpgrade.cs      # 常驻骑士阶位升级窗口
│   ├── BranchTaskEntryDrawer.cs      # 任务条目绘制器
│   ├── ResidentKnightEntryDrawer.cs  # 常驻骑士条目绘制器
│   ├── Dialog_BranchTrade.cs         # 分部交易对话框
│   ├── Dialog_NodeTreeWithRatkinOrderInfo.cs    # 节点树对话框
│   ├── UICacheComp/                  # UI缓存组件（提升渲染性能）
│   │   ├── BranchSummaryUICache.cs   # 分部摘要UI缓存
│   │   ├── BranchInfoUICache.cs      # 分部信息UI缓存
│   │   ├── BranchBuildingDefSummaryUICache.cs   # 建筑摘要UI缓存
│   │   ├── BranchFacilityStageSummaryUICache.cs # 设施阶段UI缓存
│   │   └── SquadInfoUICache.cs       # 小队信息UI缓存
│   └── DevWindow/                    # 开发者调试窗口
│       ├── DevWindowBase.cs          # 调试窗口基类
│       ├── DevWindow_Branch.cs       # 分部调试窗口
│       ├── DevWindow_BranchManager.cs  # 分部管理器调试窗口
│       ├── DevWindow_JointPatrolManager.cs       # 联合巡逻调试窗口
│       └── DevWindow_AllOrders.cs    # 全骑士团调试窗口
├── GameComponent_RatkinOrder.cs      # 游戏组件入口
├── KnightPawnsManager.cs             # 骑士单位管理器
├── ModMain.cs                        # 模组主类
├── RatkinOrderManager.cs             # 骑士团管理器
├── BranchManager.cs                  # 分部管理器
├── UniqueIDManager.cs                # 唯一ID管理器
└── OberoniaAurea_RatkinOrder.csproj  # 项目文件
```

## 核心系统模块

### 骑士团管理 (RatkinOrderManager)

作为整个模组的核心管理器，负责骑士团的整体运作。该系统管理骑士团与玩家的关系等级、处理骑士团的整体状态、协调各个分部之间的资源分配。系统维护着骑士团的声誉、经济状况和战略方向，是玩家与骑士团互动的核心枢纽。骑士团管理器还负责处理骑士团级别的决策，如是否接受新的分部、是否与其他派系开战等重大事项。

### 分部系统 (Branch)

分部是骑士团在各地区的分支组织，玩家可以在不同地点建立和发展分部。每个分部拥有独立的资源储备、人口规模、设施等级和传统体系。分部系统包括分部建筑（BranchBuilding）、分部设施（BranchFacility）、分部人口（BranchPopulation）、分部储备（BranchStoresReserve）等子模块。分部可以接受合约、完成需求、训练骑士，并与其他分部或派系进行互动。

### 常驻骑士系统 (ResidentKnight)

常驻骑士是骑士团派驻到玩家殖民地的限时骑士，与临时招募的佣兵不同。每个常驻骑士都有明确的角色定位（如文书、看护、巡逻、哨兵）和等级体系（Regular→Elite→Honor→Crown）。系统通过ResidentKnightsManager统一管理所有分部中的常驻骑士，处理骑士的招募、部署、角色分配、冥想修炼和离职等生命周期事件。常驻骑士在派驻期满后会离开殖民地并返回骑士团，期间玩家可以为其分配特定角色以获得不同的属性加成。系统还包含人格式（KnightPersonality）系统，影响骑士的个性化特征表现。

### 联合巡逻系统 (JointPatrol)

联合巡逻是骑士团与其他派系合作进行的巡逻活动，允许不同派系的单位共同执行任务。该系统包括联合巡逻管理器（JointPatrolManager）、联合巡逻商队援助（JointPatrolCaravanHelp）两个主要模块。玩家可以与鼠族派系合作进行狩猎、矿物勘探、技能支援等活动，获得资源、装备或声望奖励。

### 任务与任务群系统 (Quest & QuestClique)

模组实现了复杂的任务系统，包括善行任务（MercyQuest）、分支任务（BranchTask）、骑士求助任务（InDistressKnight）等多种类型。任务群系统（QuestClique）允许将多个相关任务组合在一起，形成更丰富的叙事体验。每个任务类型都有专门的定义类、工作器和奖励处理器，支持灵活的任务配置和扩展。

### 交互系统 (Interaction)

交互系统分为三个层级：
- **骑士团交互 (OrderInteraction)**：玩家与骑士团总部之间的整体互动
- **分部交互 (BranchInteraction)**：玩家与各分部之间的具体互动
- **联合巡逻交互 (JointPatrolInteraction)**：联合巡逻过程中的协作互动

每种交互类型都有专门的定义文件（Def）和工作器（Worker），支持商队贸易、军事支援、医疗援助、军事采购等多种交互场景。

### 信件系统 (Letter)

信件系统处理骑士团与玩家之间的书面沟通，包括普通信件、选择类信件（需要玩家做出决定）和特殊信件。OrderLetterBox负责管理所有未读信件，SpecialLetterManager处理特殊事件触发的信件。信件系统与任务系统紧密集成，许多任务通过信件形式发放和结算。

### 自新系统 (Reformation)

自新系统允许玩家通过多种方式影响骑士团的发展方向。系统维护骑士团的自新进度（ReformationProgress），玩家可以通过完成特定任务、参与联合巡逻、提升分部等级等方式推进自新，解锁新的骑士团能力和传统。

### 传统系统 (BranchTradition)

传统系统为每个分部提供独特的发展路径和特色能力。分部可以逐步解锁不同的传统阶段，每个阶段提供特定的属性加成或特殊功能。传统系统增加了分部的个性化差异，使每个分部都有独特的发展方向和战斗风格。

### 荣誉系统 (Honor)

荣誉系统为骑士个人和分部提供荣誉等级和相应奖励。不同类型的荣誉（如教导骑士、律令骑士、狩猎骑士等）对应不同的游戏机制和加成效果。荣誉系统与常驻骑士的角色分配紧密关联，影响骑士的工作效率和能力表现。

### 合约系统 (BranchContract)

合约系统允许分部接受外部委托任务，完成后可获得资源、声望或其他奖励。合约类型多样，包括防卫任务、支援任务、调查任务等。每种合约都有完成条件、时间限制和奖励机制，增加了游戏的策略深度。

### AI交互系统 (AIInteraction)

AI交互系统是一个可选的高级功能，允许通过外部AI服务（如DeepSeek、OpenAI）生成信件内容。系统提供可配置的Prompt模板和API参数，支持生成个性化的信件回复。该功能默认关闭，需要在模组设置中手动启用并配置API密钥。

## 主要管理器列表

| 管理器 | 职责 |
|--------|------|
| RatkinOrderManager | 骑士团整体管理 |
| BranchManager | 分部管理 |
| KnightPawnsManager | 骑士单位管理 |
| GlobalInteractionManager | 全局交互管理 |
| ResidentKnightsManager | 常驻骑士管理 |
| JointPatrolManager | 联合巡逻管理 |
| Quest_CliquesManager | 任务群管理 |
| ReformationManager | 自新管理 |
| BranchFacilityHandler | 设施管理 |
| BranchBuildingHandler | 建筑管理 |
| BranchDemandHandler | 需求管理 |
| BranchTaskHandler | 任务管理 |
| AcceptedBranchDemandHandler | 已接受需求管理 |
| MercyQuestHandler | 善行任务管理 |
| OrderLetterBox | 信件管理 |
| SpecialLetterManager | 特殊信件管理 |
| UniqueIDManager | 唯一ID管理 |
| AIInteractionHandler | AI交互处理 |

## 派系定义

模组定义了以下派系：
- **Rakinia**：鼠族派系，骑士团的主要合作对象
- **OARO_SubRakinia_Neutral**：次级中立鼠族派系
- **Rakinia_TravelRatkin**：旅行鼠族派系（需RatkinFaction.GeneExpand模组）
- **Rakinia_RockRatkin**：岩鼠派系（需RatkinFaction.GeneExpand模组）

## 模组设置

模组提供丰富的设置选项，主要包括：

**基础设置**：
- 普通需求消息显示开关
- 关键需求消息显示开关
- 最大同时接受需求数
- 每个分部最大同时存在合约数

**信件管理**：
- 是否启用信件上限
- 收件箱最大存储信件数
- 是否启用信件过期时间
- 信件最长保留天数

**巡逻与任务**：
- 每种巡逻互动类型最大累积次数
- 善行求助是否要求强制决定
- 是否自动续约常驻骑士

**AI功能**（可选）：
- 是否启用AI相关内容
- AI服务URL（默认：https://api.siliconflow.cn/v1/chat/completions）
- AI模型名称（默认：deepseek-ai/DeepSeek-V3.2）
- API密钥
- AI信件生成概率
- AI Prompt模板

## 构建与安装

### 构建要求

- Visual Studio 2019 或更高版本
- .NET Framework 4.8
- RimWorld游戏安装及Mods目录
- 依赖库文件（RimWorldReference目录下的dll文件）

### 构建配置

项目使用标准的MSBuild配置，输出路径已配置为本地RimWorld Mods目录：
```
E:\ProgramFiles\Steam\steamapps\common\RimWorld\Mods\[OA]Ratkin Knight Order\1.6\Assemblies\
```

### 安装方法

1. 构建项目生成dll文件
2. 将输出目录下的所有文件复制到RimWorld Mods目录中的模组文件夹
3. 确保依赖模组已安装（NewRatkin为必需依赖）
4. 在RimWorld中启用模组

### 依赖模组

- **NewRatkin (OARK)**：必需依赖，提供鼠族种族基础
- **RatkinFaction.GeneExpand**（可选）：提供旅行鼠族和岩鼠派系支持
- **NightOcean**（可选）：提供AI交互功能支持

## 代码规范与特点

项目遵循作者制定的代码规范：
- 主流程为单线程，多线程仅用于模糊查询等只读场景
- 禁止对共享数据加锁，不使用线程安全容器
- 现有for循环不得替换为foreach
- null检查仅在变量首次解引用时执行一次
- 允许async/await用于I/O操作，禁止通过Task.Run启动多线程
- 允许fire-and-forget异步调用
- 禁用dynamic和运行时类型解析
- 不删除注释掉的代码
- 输出优先为完整可编译的代码块

项目大量使用泛型集合、扩展方法和LINQ，代码风格现代且一致。游戏组件采用懒加载机制，带有完善的异常处理和缓存清理机制。

### 接口设计模式

本模组大量使用接口（Interface）进行解耦，这是一种相对少见的RimWorld模组设计方式。通过接口实现了以下功能：

- **生命周期管理**：通过IOnBranchDestroyed、IOnRatkinOrderRemoved等接口实现分布式的事件通知
- **定时任务**：通过ITickDay、ITickHour等接口实现不同频率的定时逻辑
- **自定义行为**：通过ITalkAction、IThingRequester等接口扩展交互行为
- **生成后处理**：通过IPostCombatantGenerate接口自定义单位生成逻辑

这种方式虽然增加了代码复杂度，但大大提高了模块间的解耦程度，便于功能扩展和维护。

### 界面系统 (Window)

本模组实现了完整的界面系统，为玩家提供丰富的交互界面。主要包括以下界面：

**主界面**：
- **MainTabWindow_RatkinOrder**：骑士团总览主标签页窗口，作为模组的主入口界面
- **Window_RatkinOrder**：骑士团总览窗口，展示骑士团整体状态和关键信息

**分部管理界面**：
- **Window_BranchList**：分部列表窗口，浏览所有已建立的分部
- **Window_Branch**：分部详情窗口，查看单个分部的详细信息
- **Window_BranchDemand**：分部需求窗口，接受和处理分部需求
- **Window_BranchTask**：分部任务窗口，管理分部任务
- **Window_BranchSquad**：分部小队窗口，管理骑士小队

**常驻骑士界面**：
- **Window_ResidentKnight_AcademicArrange**：课业安排窗口，安排常驻骑士的课业进度
- **Window_ResidentKnight_RankUpgrade**：等级升级窗口，提升骑士等级

**骑士团整体界面**：
- **Window_OrderHall**：骑士大厅窗口，管理骑士团设施等级
- **Window_OrderLetterBox**：信箱窗口，查看骑士团来信
- **Window_LetterBoxSetting**：信箱设置窗口
- **Window_QuestClique**：任务群窗口，查看任务群组

**界面设计特点**：
- **OrderWindowBase**：所有骑士团窗口的基类，统一了窗口风格（禁用缩放、拖拽、背景绘制等）
- **UI缓存组件 (UICacheComp)**：通过BranchSummaryUICache、BranchInfoUICache等缓存类提升界面渲染性能，避免重复计算
- **条目绘制器**：BranchTaskEntryDrawer、ResidentKnightEntryDrawer等类负责复杂条目的绘制逻辑
- **开发者窗口**：提供DevWindow_*系列调试窗口，便于开发测试

界面系统采用典型的RimWorld窗口架构，每个窗口负责特定功能，通过WindowLayer.Dialog层级显示，提供统一的用户体验。

| 文件 | 说明 |
|------|------|
| ModMain.cs | 模组入口类，继承自Mod，包含设置界面 |
| GameComponent_RatkinOrder.cs | 游戏组件入口，管理所有核心管理器 |
| RatkinOrderManager.cs | 骑士团管理器，处理骑士团整体逻辑 |
| BranchManager.cs | 分部管理器，处理分部创建、升级等 |
| KnightPawnsManager.cs | 骑士单位管理器，处理骑士生成和管理 |
| ModDefOf.cs | 模组定义静态类，定义所有Def常量 |
| OberoniaAureaRatkinOrder.cs | 模组设置类，处理用户配置 |

## 扩展能力

本模组设计为可扩展框架，允许通过以下方式进行二次开发：

1. **添加新的分部类型**：在XML定义文件中实例化BranchBuildingDef，配置建筑属性、功能类、效果标志等参数
2. **添加新的交互类型**：继承BranchInteractionWorker类（需实现只接受BranchInteractionDef参数的构造函数），在XML中定义BranchInteractionDef并指定workerClass
3. **添加新的任务类型**：继承QuestNode_Root_*系列基类构建自定义任务流程，在XML中定义对应的QuestScriptDef
4. **添加新的荣誉类型**：在XML中定义BranchHonorDef实例，配置荣誉Buff、课业、印记和图标资源
5. **添加新的设施类型**：在XML中实例化BranchFacilityDef，定义四个等级的设施效果（poor/normal/good/excellent）

模组的定义系统（Def System）和Worker系统为功能扩展提供了清晰的分离架构：Def负责数据配置，Worker负责功能实现。
