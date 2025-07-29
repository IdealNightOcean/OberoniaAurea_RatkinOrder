using System;

namespace OberoniaAurea.RatkinOrder;

public interface ITagStrToValue<T> where T : struct
{
    bool TryGetTagValue(string tag, out T value);
    void SetTagValue(string tag, T value, bool addIfMiss);
    void OffsetTagValueBy(string tag, T offset, bool addIfMiss);
    void ScaleTagValueBy(string tag, T scale, bool addIfMiss);
    void ModifyTagValueBy(string tag, Func<T, T> modifier, bool addIfMiss);
    bool HasTag(string tag);
    void RemoveTag(string tag);
}
