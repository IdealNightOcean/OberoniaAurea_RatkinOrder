using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct QuestEffectTag : IExposable, IEquatable<QuestEffectTag>
{
    private string key;
    public readonly string Key => key;

    public string Label;
    public string Description;

    public QuestEffectTag() { }
    public QuestEffectTag(string key) => this.key = key;
    public QuestEffectTag(string key, string label, string description)
    {
        this.key = key;
        Label = label;
        Description = description;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref key, "key");
        Scribe_Values.Look(ref Label, "Label");
        Scribe_Values.Look(ref Description, "Description");
    }

    public override readonly string ToString() => $"{key}-{Label}";

    public override readonly int GetHashCode() => key.GetHashCode();

    public readonly bool Equals(QuestEffectTag other) => key == other.key;

    public override readonly bool Equals(object obj)
    {
        if (obj is QuestEffectTag other)
        {
            return Equals(other);
        }
        return false;
    }

    public static bool operator ==(QuestEffectTag left, QuestEffectTag right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(QuestEffectTag left, QuestEffectTag right)
    {
        return !left.Equals(right);
    }
}