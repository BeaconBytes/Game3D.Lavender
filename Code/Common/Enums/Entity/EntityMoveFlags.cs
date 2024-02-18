using System;

namespace Lavender.Common.Enums.Entity;

[Flags]
public enum EntityMoveFlags : byte
{
    None = 0,
    
    Jump = 1,
    Sprint = 2,
    
    Frozen = 4,
}