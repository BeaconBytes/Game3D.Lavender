using Godot;
using Lavender.Common.Enums.Entity;

namespace Lavender.Common.Entity.Data;

public struct InputPayload
{
    public uint tick;
    public Vector3 moveInput;
    public Vector3 lookInput;
    public EntityMoveFlags flagsInput;
}