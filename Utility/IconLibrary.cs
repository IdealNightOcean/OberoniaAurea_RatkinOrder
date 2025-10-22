using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class IconLibrary
{
    public static readonly Texture2D Medal_Courage = ContentFinder<Texture2D>.Get("UI/Medal/OARO_Medal_Courage");
    public static readonly Texture2D Medal_Tenacity = ContentFinder<Texture2D>.Get("UI/Medal/OARO_Medal_Tenacity");
    public static readonly Texture2D Medal_Rescue = ContentFinder<Texture2D>.Get("UI/Medal/OARO_Medal_Rescue");
    public static readonly Texture2D Medal_Justice = ContentFinder<Texture2D>.Get("UI/Medal/OARO_Medal_Justice");
}
