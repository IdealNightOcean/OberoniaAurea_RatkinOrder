using OberoniaAurea_Frame;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ReformationManager : IExposable, IPostLoadInit, IDrawDevWindow
{
    [Unsaved] public readonly RatkinOrder RatkinOrder;

    private float reformProgress;
    public float ReformProgress
    {
        get { return reformProgress; }
        set { reformProgress = value; }
    }

    public float FixedReformProgressCost = -1f;

    private HashSet<OrderReformationDef> reformations = [];
    public int ReformationsCount => reformations.Count;

    [Unsaved] public readonly TagStrToBoolCountable EffectTags = new();
    [Unsaved] public readonly BranchStatTransformerHandler TransformerHandler = new();

    public ReformationManager(RatkinOrder ratkinOrder, bool initConstruct)
    {
        RatkinOrder = ratkinOrder;
        if (initConstruct)
        {

        }
    }

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
        if (listing_Rect.ButtonText("EffectTags", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(EffectTags.GetDetailString()));
        }
        if (listing_Rect.ButtonText("StatTransformers", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(TransformerHandler.GetDetailString()));
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

        float reformProgressCost = FixedReformProgressCost > 0f ? FixedReformProgressCost : GetReformProgressCost(def);
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
