using OberoniaAurea.RatkinOrder.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 自新管理
/// </summary>
public class ReformationManager(RatkinOrder ratkinOrder) : IExposable
{
    public RatkinOrder RatkinOrder { get; } = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
    public List<IPostCombatantGenerate> IPostCombatantGenerate { get; } = [];

    private float reformProgress;
    public float ReformProgress
    {
        get { return reformProgress; }
        set { reformProgress = value > 0f ? value : 0f; }
    }

    public float FixedReformProgressCost = -1f;

    private HashSet<OrderReformationDef> reformations = [];
    public int ReformationsCount => reformations.Count;

    public void PostOrderGenerated() { }

    public void ExposeData()
    {
        Scribe_Values.Look(ref reformProgress, "reformProgress", 0f);
        Scribe_Values.Look(ref FixedReformProgressCost, "FixedReformProgressCost", -1f);

        Scribe_Collections.Look(ref reformations, "reformations", LookMode.Def);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"自新点数: {reformProgress}");
        if (listing_Rect.ButtonText("自新点数 +10"))
        {
            ReformProgress += 10f;
        }
        if (listing_Rect.ButtonText("自新点数 -10"))
        {
            ReformProgress -= 10f;
        }
        listing_Rect.Label($"激活自新数量: {ReformationsCount}");
        listing_Rect.Label($"下次自新固定花费: {FixedReformProgressCost}");

        if (listing_Rect.ButtonText("自新详细信息", null, 0.8f))
        {
            Find.WindowStack.Add(OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(GetAllActiveReformationString()));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasReformation(OrderReformationDef def) => reformations.Contains(def);

    public float GetReformProgressCost(OrderReformationDef def)
    {
        if (FixedReformProgressCost > 0f)
        {
            return FixedReformProgressCost;
        }
        float reformProgressCost = def.reformProgressCost;

        float memorialCostReduce = Mathf.Clamp(RatkinOrder.EffectTags.GetTagCount($"MemorialReformationCostReduce_{def.reformationType}") * 0.05f, 0f, 0.5f);
        reformProgressCost *= (1f - memorialCostReduce);

        return reformProgressCost;
    }

    public AcceptanceReport CanActiveReformation(OrderReformationDef def, bool resultOnly = false)
    {
        if (reformations.Contains(def))
        {
            return resultOnly ? false : "OARO_HasSameReformation".Translate();
        }

        float reformProgressCost = GetReformProgressCost(def);
        if (reformProgress < reformProgressCost)
        {
            return resultOnly ? false : "OARO_Insufficien_ReformProgresst".Translate(reformProgressCost);
        }

        if (def.prerequisites is not null)
        {
            foreach (OrderReformationDef preDef in def.prerequisites)
            {
                if (!reformations.Contains(preDef))
                {
                    return resultOnly ? false : "OARO_Omission_PreReformation".Translate();
                }
            }
        }

        return true;
    }

    public void PostCombatantGenerate(Pawn p, KnightRecord record)
    {
        if (IPostCombatantGenerate is null || IPostCombatantGenerate.Count == 0)
        {
            return;
        }

        for (int i = 0; i < IPostCombatantGenerate.Count; i++)
        {
            try
            {
                IPostCombatantGenerate[i].PostCombatantGenerate(p, record);
            }
            catch (Exception ex)
            {
                string processorTypeName = IPostCombatantGenerate[i]?.GetType()?.FullName ?? "UnknownProcessor";
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"执行战斗人员生成后处理器: {processorTypeName}",
                    typeName: nameof(ReformationManager),
                    methodName: nameof(PostCombatantGenerate),
                    needStackTrace: true);
            }
        }
    }

    private void ActiveReformation(OrderReformationDef def)
    {
        RatkinOrder.EffectTags.IncrementTagsValue(def.effectFlags, addIfMiss: true);
        RatkinOrder.TransformerHandler.MergeStatOffsets(def.branchStatOffsets, addIfMiss: true);
        RatkinOrder.TransformerHandler.MergeStatFactors(def.branchStatFactors, addIfMiss: true);
        def.Worker.PostActive(RatkinOrder);
    }

    internal void PostLoadInit()
    {
        if (reformations.Remove(null))
        {
            Log.Error($"[OARO] {ratkinOrder} 的自新在加载后为null，已被移除。");
        }
        foreach (OrderReformationDef def in reformations)
        {
            try
            {
                ActiveReformation(def);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"加载后重新激活自新 {def?.label ?? "Unknown"}",
                    typeName: nameof(ReformationManager),
                    methodName: nameof(PostLoadInit),
                    needStackTrace: true);
            }
        }
    }

    private string GetAllActiveReformationString()
    {
        if (reformations.NullOrEmpty())
        {
            return "None";
        }

        StringBuilder sb = new();
        int i = 0;
        foreach (OrderReformationDef item in reformations)
        {
            sb.AppendInNewLine($"{++i}. {item.label}");
        }

        return sb.ToString();
    }
}
