using System;
using System.Runtime.CompilerServices;

using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.EsteemHandler;

namespace OberoniaAurea.RatkinOrder;

public static class EsteemUtility
{
    /*
     关系枚举数组
    */
    public static readonly RelationshipKind[] RelationshipKindArr = (RelationshipKind[])Enum.GetValues(typeof(RelationshipKind));

    /*
     认可度软上限
    */
    private static readonly float[] EsteemSoftCap = [0.29f, 0.49f, 0.69f, 0.89f, 1f];
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetEsteemSoftCap(RelationshipKind relationship)
    {
        return EsteemSoftCap[Mathf.Clamp((int)relationship, 0, EsteemSoftCap.Length - 1)];
    }

    /*
     认可度称号和描述
    */
    private static readonly Color gold = new(1f, 0.843f, 0f);
    private static readonly (string, Color)[] EsteemTitles =
    [
        ("OARO_EsteemStranger",Color.white),
        ("OARO_EsteemAcquaintance",Color.cyan),
        ("OARO_EsteemNewFriend",Color.green),
        ("OARO_EsteemFriend",Color.green),
        ("OARO_EsteemGoodFriend",Color.yellow),
        ("OARO_EsteemTrustworthy",Color.yellow),
        ("OARO_EsteemSoulmate",gold),
    ];
    private static readonly (string, Color)[] EsteemDescs =
    [
        ("OARO_EsteemStrangerDesc",Color.white),
        ("OARO_EsteemAcquaintanceDesc",Color.cyan),
        ("OARO_EsteemNewFriendDesc",Color.green),
        ("OARO_EsteemFriendDesc",Color.green),
        ("OARO_EsteemGoodFriendDesc",Color.yellow),
        ("OARO_EsteemTrustworthyDesc",Color.yellow),
        ("OARO_EsteemSoulmateDesc",gold),
    ];
    public static string GetEsteemTitle(float esteem)
    {
        int index = 0;
        if (esteem >= 0.1f)
        {
            if (esteem >= 1f)
            {
                index = EsteemTitles.Length - 1;
            }
            else
            {
                index = Mathf.Clamp(Mathf.FloorToInt((esteem - 0.1f) / 0.2f) + 1, 0, EsteemTitles.Length - 1);
            }
        }

        (string, Color) esteemTitle = EsteemTitles[index];
        return esteemTitle.Item1.Translate().Colorize(esteemTitle.Item2);
    }
    public static string GetEsteemDesc(float esteem, RatkinOrder order)
    {
        int index = 0;
        if (esteem >= 0.1f)
        {
            if (esteem >= 1f)
            {
                index = EsteemDescs.Length - 1;
            }
            else
            {
                index = Mathf.Clamp(Mathf.FloorToInt((esteem - 0.1f) / 0.2f) + 1, 0, EsteemDescs.Length - 1);
            }
        }

        (string, Color) esteemDesc = EsteemDescs[index];
        return esteemDesc.Item1.Translate(order.Name).Colorize(esteemDesc.Item2);
    }

    /*
     关系称号和描述
    */
    private static readonly (string, Color)[] RelationshipKindLabels =
    [
        ("OARO_RelationStranger",Color.white),
        ("OARO_RelationAcquaintance",Color.cyan),
        ("OARO_RelationFriendly",Color.green),
        ("OARO_RelationTrustworthy",Color.green),
        ("OARO_RelationSoulmate",Color.green),
    ];
    private static readonly (string, Color)[] RelationshipKindDescs =
{
        ("OARO_RelationStrangerDesc",Color.white),
        ("OARO_RelationAcquaintanceDesc",Color.cyan),
        ("OARO_RelationFriendlyDesc",Color.green),
        ("OARO_RelationTrustworthyDesc",Color.green),
        ("OARO_RelationSoulmateDesc",Color.green),
    };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetRelationshipKindLabel(RelationshipKind relationship)
    {
        int index = Mathf.Clamp((int)relationship, 0, RelationshipKindLabels.Length - 1);
        (string, Color) relationshipLabel = RelationshipKindLabels[index];
        return relationshipLabel.Item1.Translate().Colorize(relationshipLabel.Item2);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetRelationshipKindDesc(RelationshipKind relationship, RatkinOrder order)
    {
        int index = Mathf.Clamp((int)relationship, 0, RelationshipKindDescs.Length - 1);
        (string, Color) relationshipLabel = RelationshipKindDescs[index];
        return relationshipLabel.Item1.Translate(order.Name).Colorize(relationshipLabel.Item2);
    }
}
