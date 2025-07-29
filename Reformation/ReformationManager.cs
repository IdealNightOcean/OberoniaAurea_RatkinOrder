using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ReformationManager : IExposable, IPostLoadInit
{
    [Unsaved] public readonly RatkinOrder RatkinOrder;

    private float reformProgress;
    public float ReformProgress
    {
        get { return reformProgress; }
        set { reformProgress = value; }
    }

    public float fixedReformProgressCost = -1f;

    private HashSet<OrderReformationDef> reformations = [];
    public int ReformationsCount => reformations.Count;

    [Unsaved] public readonly TagStrToBoolCountable EffectTags = new();
    [Unsaved] public readonly BranchStatTransformerHandler TransformerHandler = new();

    public ReformationManager(RatkinOrder ratkinOrder, bool initConstruct)
    {
        this.RatkinOrder = ratkinOrder;
        if (initConstruct)
        {

        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasReformation(OrderReformationDef def)
    {
        return reformations.Contains(def);
    }

    public float GetReformProgressCost(OrderReformationDef def)
    {
        return def.reformProgressCost;
    }

    public AcceptanceReport CanActiveReformation(OrderReformationDef def, bool resultOnly = false)
    {
        if (reformations.Contains(def))
        {
            return resultOnly ? false : "OARO_HasSameReformation".Translate();
        }

        float reformProgressCost = fixedReformProgressCost > 0f ? fixedReformProgressCost : GetReformProgressCost(def);
        if (reformProgress < reformProgressCost)
        {
            return resultOnly ? false : "OARO_InsufficienReformProgresst".Translate(reformProgressCost);
        }

        if (def.prerequisites is not null)
        {
            foreach (OrderReformationDef preDef in def.prerequisites)
            {
                if (!reformations.Contains(preDef))
                {
                    return resultOnly ? false : "OARO_OmissionOfPreReformation".Translate();
                }
            }
        }

        return true;
    }

    private void AddOrPostInitReformation(OrderReformationDef def, bool postInit)
    {
        EffectTags.IncrementTagsValue(def.effectFlags, addIfMiss: true);
        TransformerHandler.AddStatModifiers(def.branchStatModifies);
        if (postInit)
        {
            def.Worker.PostInit();
        }
        else
        {
            def.Worker.PostAdd();
        }
    }

    public void PostLoadInit()
    {
        reformations.Remove(null);
        foreach (OrderReformationDef def in reformations)
        {
            try
            {
                AddOrPostInitReformation(def, postInit: true);
            }
            catch (Exception ex)
            {
                Log.Error($"Fail to init reformation {def.label} after load: {ex}");
                continue;
            }
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref reformProgress, "reformProgress", 0f);
        Scribe_Collections.Look(ref reformations, "reformations", LookMode.Def);
    }
}
