using Godot;
using Lavender.Common.Enums.Entity;

namespace Lavender.Common.Entity.Data;

public struct RawInputs
{
    public Vector3 MoveInput;
    public Vector3 LookInput;
    public EntityMoveFlags FlagsInput;
}