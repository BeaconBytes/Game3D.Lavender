using Godot;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Mapping;

public class MapInitializePacket : GamePacket
{
    public ushort ChunkSize { get; protected set; }
    public uint WorldHeight { get; protected set; }
    public uint WorldSize { get; protected set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(ChunkSize);
        writer.Put(WorldHeight);
        writer.Put(WorldSize);
    }

    public override void Deserialize(NetDataReader reader)
    {
        ChunkSize = reader.GetUShort();
        WorldHeight = reader.GetUInt();
        WorldSize = reader.GetUInt();
    }
}