using OberoniaAurea_Frame;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ReformationManager(RatkinOrder ratkinOrder) : IExposable, IPostLoadInit, IDrawDevWindow
{
    [Unsaved] public readonly RatkinOrder RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));

    private float reformProgress;
    public float ReformProgress
    {
        get { return reformProgress; }
        set { reformProgress = value; }
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
        listing_Rect.Label($"ReformProgress: {reformProgress}");
        listing_Rect.Label($"ReformationsCount: {ReformationsCount}");
        listing_Rect.Label($"FixedReformProgressCost: {FixedReformProgressCost}");

        if (listing_Rect.ButtonText("Reformations", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(GetAllActiveReformationString()));
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

    private void ActiveReformation(OrderReformationDef def)
    {
        RatkinOrder.EffectTags.IncrementTagsValue(def.effectFlags, addIfMiss: true);
        RatkinOrder.TransformerHandler.AddStatModifiers(def.branchStatModifies);
        def.Worker.PostActive(RatkinOrder);
    }

    public void PostLoadInit()
    {
        reformations.Remove(null);
        foreach (OrderReformationDef def in reformations)
        {
            try
            {
                ActiveReformation(def);
            }
            catch (Exception ex)
            {
                Log.Error($"Fail to reactive reformation {def.label} after load: {ex}");
                continue;
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
