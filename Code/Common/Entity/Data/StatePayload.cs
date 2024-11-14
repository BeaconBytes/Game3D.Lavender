using Godot;

namespace Lavender.Common.Entity.Data;

public record struct StatePayload
{
    public uint tick;
    public Vector3 position;
    public Vector3 rotation;
}