using Godot;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Entity.Movement;

public class ForceSyncEntityPacket : GamePacket
{
    public uint NetId { get; set; }
    public uint CurrentTick { get; set; }
    public Vector3 CurrentPos { get; set; }
    public Vector3 CurrentRotation { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(CurrentTick);
        writer.Put(CurrentPos);
        writer.Put(CurrentRotation);
    }

    public override void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetUInt();
        CurrentTick = reader.GetUInt();
        CurrentPos = reader.GetVector3();
        CurrentRotation = reader.GetVector3();
    }
}