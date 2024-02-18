using Godot;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Entity.Movement;

public class EntityRotatePacket : GamePacket
{
    public uint NetId { get; set; }
    public Vector3 Rotation { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(Rotation);
    }

    public override void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetUInt();
        Rotation = reader.GetVector3();
    }
}