using System;

namespace Lavender.Common.Enums.Entity;

[Flags]
public enum EntityMoveFlags : uint
{
    None = 0,
    
    Jump = 1,
    Sprint = 2,
    Duck = 4,
    // UnusedOne = 8,
    // UnusedTwo = 16,
    
    Frozen = 32,
    
    PrimaryAttack = 64,
    SecondaryAttack = 128,
}