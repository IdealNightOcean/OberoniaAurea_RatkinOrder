using Verse;

namespace OberoniaAurea.RatkinOrder;

public class FermentingBarrelExtension : DefModExtension
{
    public ThingDef rawMaterial;
    public int rawCount;
    public ThingDef product;
    public int productCount;

    public int fermentationDuration = 360000;
    public float idealFermentingTemperature = 7f;
}
