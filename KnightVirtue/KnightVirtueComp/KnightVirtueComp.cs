using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp
{
    public KnightVirtueWithComps Parent { get; private set; }

    protected KnightVirtueCompProperties props;

    public ResidentKnight Knight => Parent.knight;
    public Pawn Pawn => Parent.knight.Pawn;

    public KnightVirtueDef Def => Parent.Def;

    /// <summary>
    /// comp组件初始化时生效
    /// </summary>
    public virtual void Initialize(KnightVirtueWithComps parent, KnightVirtueCompProperties props)
    {
        this.Parent = parent;
        this.props = props;
    }

    /// <summary>
    /// 仅在添加美德时生效
    /// </summary>
    public virtual void PostAdd() { }
    /// <summary>
    /// 在添加美德和重新载入存档时生效，添加美德时调用顺序在<see cref="PostAdd"/>之后
    /// </summary>
    public virtual void PostActive() { }

    public virtual void PostRemove() { }

    public virtual void OnRefreshBuffStage(HediffStageModifierBuilder buffStageBuilder) { }

    public virtual void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo) { }

    public virtual void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt) { }

    public virtual void Notify_Stimulate(Pawn recipient) { }

    public virtual void CompExposeData() { }
}