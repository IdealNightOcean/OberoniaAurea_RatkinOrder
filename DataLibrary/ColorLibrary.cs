using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.DataLibrary;

[StaticConstructorOnStartup]
public static class OARO_ColorLibrary
{
    private const float RgbaByteScale = 1f / 255f;

    public static readonly Color Silver = new(190f * RgbaByteScale, 190f * RgbaByteScale, 190f * RgbaByteScale);

    public static readonly Color CommonOutline = new(118f * RgbaByteScale, 118f * RgbaByteScale, 118f * RgbaByteScale);
    public static readonly Color DivideLine = new(74f * RgbaByteScale, 74f * RgbaByteScale, 74f * RgbaByteScale);

    public static readonly Color DeepDarkBackground = new(21f * RgbaByteScale, 25f * RgbaByteScale, 29f * RgbaByteScale);
    public static readonly Color MediumDarkBackground = new(33f * RgbaByteScale, 33f * RgbaByteScale, 33f * RgbaByteScale);
    public static readonly Color DimDarkBackground = new(42f * RgbaByteScale, 43f * RgbaByteScale, 44f * RgbaByteScale);

    public static readonly Color DeepInactive = new(58f * RgbaByteScale, 58f * RgbaByteScale, 58f * RgbaByteScale);
    public static readonly Color DimInactive = new(153f * RgbaByteScale, 153f * RgbaByteScale, 153f * RgbaByteScale);

    public static readonly Texture2D CyanTex = SolidColorMaterials.NewSolidColorTexture(Color.cyan);
    public static readonly Texture2D GreenTex = SolidColorMaterials.NewSolidColorTexture(Color.green);
    public static readonly Texture2D OrangeTex = SolidColorMaterials.NewSolidColorTexture(ColorLibrary.Orange);
}
