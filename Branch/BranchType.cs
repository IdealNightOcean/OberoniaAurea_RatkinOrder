using System;

namespace OberoniaAurea.RatkinOrder;

[Flags]
public enum BranchType : byte
{
    Normal = 0,
    Friendly = 1,
    Honor = 2,
    Mobile = 4
}