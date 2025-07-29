using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class EsteemHandler : IExposable, ITickDay
{
    public enum RelationshipKind : byte
    {
        Stranger = 0,
        Acquaintance = 1,
        Friendly = 2,
        Trustworthy = 3,
        Soulmate = 4
    };

    [Unsaved] public RatkinOrder RatkinOrder;

    /*
    认可度
    */
    protected float esteem;
    public float Esteem
    {
        get
        {
            return esteem;
        }
        set
        {
            esteem = Mathf.Clamp(value, 0f, 1f);
        }
    }
    public float CurEsteemSoftCap => EsteemUtility.GetEsteemSoftCap(relationship);
    /*
    关系
    */
    private RelationshipKind relationship = RelationshipKind.Stranger; //当前关系
    public RelationshipKind Relationship => relationship;

    public int lastRelationshipChangeTick = -1;

    private int totalRecommendation;
    private int curRecommendation;

    public int TotalRecommendation => totalRecommendation;
    public int CurRecommendation
    {
        get { return curRecommendation; }
        set { curRecommendation = Mathf.Max(0, value); }
    }

    public EsteemHandler(RatkinOrder ratkinOrder, bool initConstruct)
    {
        this.RatkinOrder = ratkinOrder;
        if (initConstruct)
        {
            relationship = GameComponent_RatkinOrder.Instance?.InitOrderRelationship ?? RelationshipKind.Stranger;
            esteem = EsteemUtility.GetEsteemSoftCap(relationship);
        }
    }

    public void TickDay()
    {
        if (esteem > CurEsteemSoftCap)
        {
            esteem -= 0.01f;
        }
    }

    public void Notify_FactionRelationChanged(FactionRelationKind newRelation)
    {
        if (newRelation == FactionRelationKind.Hostile)
        {
            RelationshipOffset(-1);
        }
    }

    public void RelationshipOffset(int offset)
    {
        if (offset == 0)
        {
            return;
        }
        RelationshipKind newRelationship = EsteemUtility.RelationshipKindArr[Mathf.Clamp((int)relationship + offset, 0, EsteemUtility.RelationshipKindArr.Length - 1)];
        SetRelationship(newRelationship);
    }

    public void SetRelationship(RelationshipKind newRelationship)
    {
        if (newRelationship == relationship)
        {
            return;
        }

    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref esteem, "esteem", 0f);

        Scribe_Values.Look(ref relationship, "relationship", RelationshipKind.Stranger);
        Scribe_Values.Look(ref lastRelationshipChangeTick, "lastRelationshipChangeTick", -1);

        Scribe_Values.Look(ref totalRecommendation, "totalRecommendation", 0);
        Scribe_Values.Look(ref curRecommendation, "curRecommendation", 0);
    }
}
