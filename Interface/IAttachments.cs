using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public interface IAttachments
{
    List<Thing> Attachments { get; }

    void AddAttachment(Thing attachment);
    void AddAttachments(IEnumerable<Thing> attachments);
}
