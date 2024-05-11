using Godot;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Entity.Movement;

public class EntityMoveToPacket : GamePacket
{
    public uint NetId { get; set; }
    public Vector3 Position { get; set; }
    public Vector3? Rotation { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(Position);

        writer.Put(Rotation.HasValue);
        if (Rotation.HasValue)
            writer.Put(Rotation.Value);
    }

    public override void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetUInt();
        Position = reader.GetVector3();

        bool hasRotationValue = reader.GetBool();
        if (hasRotationValue)
            Rotation = reader.GetVector3();
        else
            Rotation = null;
    }
}