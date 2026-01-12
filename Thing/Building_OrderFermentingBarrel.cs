using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Building_OrderFermentingBarrel : Building
{
    private static readonly Vector2 BarSize = new(0.55f, 0.1f);
    private static readonly Color BarZeroProgressColor = new(0.4f, 0.27f, 0.22f);
    private static readonly Color BarFermentedColor = new(0.9f, 0.85f, 0.2f);
    private static readonly Material BarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f));

    private int rawCount;
    private float progress;
    [Unsaved] private Material barFilledCachedMat;

    [Unsaved] private FermentingBarrelExtension modEx_FermentingBarrel;
    public FermentingBarrelExtension ModEx_FermentingBarrel => modEx_FermentingBarrel ??= def.GetModExtension<FermentingBarrelExtension>();

    public float Progress
    {
        get
        {
            return progress;
        }
        set
        {
            if (value != progress)
            {
                progress = Mathf.Clamp01(value);
                barFilledCachedMat = null;
            }
        }
    }
    private Material BarFilledMat
    {
        get
        {
            if (barFilledCachedMat is null)
            {
                barFilledCachedMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.Lerp(BarZeroProgressColor, BarFermentedColor, Progress));
            }
            return barFilledCachedMat;
        }
    }

    public int SpaceLeftForRaw
    {
        get
        {
            if (!Fermented)
            {
                return ModEx_FermentingBarrel.rawCount - rawCount;
            }
            return 0;
        }
    }

    private bool BarEmpty => rawCount <= 0;
    public int ProductCount
    {
        get
        {
            if (BarEmpty)
            {
                return 0;
            }
            return ModEx_FermentingBarrel.productCount * rawCount / ModEx_FermentingBarrel.rawCount;
        }
    }
    public bool Fermented => rawCount > 0 && Progress >= 1f;

    [Unsaved] private CompTemperatureRuinable temperatureRuinableComp;
    public CompTemperatureRuinable TemperatureRuinableComp => temperatureRuinableComp ??= GetComp<CompTemperatureRuinable>();

    private float CurrentTempProgressSpeedFactor
    {
        get
        {
            float minSafeTemperature = TemperatureRuinableComp.Props.minSafeTemperature;
            float ambientTemperature = AmbientTemperature;
            if (ambientTemperature < minSafeTemperature)
            {
                return 0.1f;
            }
            if (ambientTemperature < ModEx_FermentingBarrel.idealFermentingTemperature)
            {
                return GenMath.LerpDouble(minSafeTemperature, ModEx_FermentingBarrel.idealFermentingTemperature, 0.1f, 1f, ambientTemperature);
            }
            return 1f;
        }
    }
    private float ProgressPerTickAtCurrentTemp => 1f / ModEx_FermentingBarrel.fermentationDuration * CurrentTempProgressSpeedFactor;
    private int EstimatedTicksLeft => Mathf.Max(Mathf.RoundToInt((1f - Progress) / ProgressPerTickAtCurrentTemp), 0);

    public override void TickRare()
    {
        base.TickRare();
        if (!BarEmpty)
        {
            Progress = Mathf.Min(Progress + 250f * ProgressPerTickAtCurrentTemp, 1f);
        }
    }

    public void AddRawMaterial(Thing raw)
    {
        int numToAdd = Mathf.Min(raw.stackCount, ModEx_FermentingBarrel.rawCount - rawCount);
        if (numToAdd > 0)
        {
            AddRawMaterial(numToAdd);
            raw.SplitOff(numToAdd).Destroy();
        }
    }

    public void AddRawMaterial(int count)
    {
        TemperatureRuinableComp.Reset();
        if (Fermented)
        {
            Log.Warning("尝试向装满产品的桶添加原料。殖民者应先取出产品。");
            return;
        }
        int numToAdd = Mathf.Min(count, ModEx_FermentingBarrel.rawCount - rawCount);
        if (numToAdd > 0)
        {
            Progress = GenMath.WeightedAverage(0f, numToAdd, Progress, rawCount);
            rawCount += numToAdd;
        }
    }

    protected override void ReceiveCompSignal(string signal)
    {
        if (signal == "RuinedByTemperature")
        {
            Reset();
        }
    }

    private void Reset()
    {
        rawCount = 0;
        Progress = 0f;
    }

    public override string GetInspectString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append(base.GetInspectString());
        if (stringBuilder.Length != 0)
        {
            stringBuilder.AppendLine();
        }

        if (!BarEmpty && !TemperatureRuinableComp.Ruined)
        {
            if (Fermented)
            {
                stringBuilder.AppendLine("OARO_BarrelContainsProduct".Translate(ModEx_FermentingBarrel.product.label, ProductCount, ModEx_FermentingBarrel.productCount));
            }
            else
            {
                stringBuilder.AppendLine("OARO_BarrelContainsRaw".Translate(ModEx_FermentingBarrel.rawMaterial.label, rawCount, ModEx_FermentingBarrel.rawCount));
            }
        }
        if (!BarEmpty)
        {
            if (Fermented)
            {
                stringBuilder.AppendLine("Fermented".Translate());
            }
            else
            {
                stringBuilder.AppendLine("FermentationProgress".Translate(Progress.ToStringPercent(), EstimatedTicksLeft.ToStringTicksToPeriod()));
                if (CurrentTempProgressSpeedFactor != 1f)
                {
                    stringBuilder.AppendLine("FermentationBarrelOutOfIdealTemperature".Translate(CurrentTempProgressSpeedFactor.ToStringPercent()));
                }
            }
        }
        stringBuilder.AppendLine("Temperature".Translate() + ": " + AmbientTemperature.ToStringTemperature("F0"));
        stringBuilder.AppendLine("IdealFermentingTemperature".Translate() + ": " + ModEx_FermentingBarrel.idealFermentingTemperature.ToStringTemperature("F0") + " ~ " + TemperatureRuinableComp.Props.maxSafeTemperature.ToStringTemperature("F0"));
        return stringBuilder.ToString().TrimEndNewlines();
    }

    public Thing TakeOutProduct()
    {
        if (!Fermented)
        {
            Log.Warning("尝试获取产品但尚未发酵完成。");
            return null;
        }
        Thing thing = ThingMaker.MakeThing(ModEx_FermentingBarrel.product);
        thing.stackCount = ProductCount;
        Reset();
        BroadcastCompSignal("OARO_BarrelProduced");
        return thing;
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        if (!BarEmpty)
        {
            Vector3 center = drawLoc;
            center.y += 1f / 26f;
            center.z += 0.25f;
            GenDraw.FillableBarRequest r = default;
            r.center = center;
            r.size = BarSize;
            r.fillPercent = (float)rawCount / ModEx_FermentingBarrel.rawCount;
            r.filledMat = BarFilledMat;
            r.unfilledMat = BarUnfilledMat;
            r.margin = 0.1f;
            r.rotation = Rot4.North;
            GenDraw.DrawFillableBar(r);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }
        if (!DebugSettings.ShowDevGizmos)
        {
            yield break;
        }
        if (!BarEmpty)
        {
            Command_Action command_Finish = new()
            {
                defaultLabel = "DEV: Set progress to 1",
                action = delegate
                    {
                        Progress = 1f;
                    }
            };
            yield return command_Finish;
        }
        if (SpaceLeftForRaw > 0)
        {
            Command_Action command_DevFill = new()
            {
                defaultLabel = "DEV: Fill",
                action = delegate
                    {
                        rawCount = ModEx_FermentingBarrel.rawCount;
                    }
            };
            yield return command_DevFill;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref rawCount, "rawCount", 0);
        Scribe_Values.Look(ref progress, "progress", 0f);
    }
}