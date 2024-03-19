using System.Collections.Generic;
using Godot;
using Lavender.Common.Enums.Types;
using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Mapping;

public class MapChunkPacket : GamePacket
{
    public BlockType[] BlockTypes { get; set; }
    public Vector3I[] BlockPositions { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(BlockTypes.Length);
        for (int i = 0; i < BlockTypes.Length; i++)
        {
            writer.Put((byte)BlockTypes[i]);
            writer.Put(BlockPositions[i]);
        }
    }

    public override void Deserialize(NetDataReader reader)
    {
        int entryCount = reader.GetInt();
        if (entryCount <= 0)
            return;
        List<BlockType> tmpTypes = new();
        List<Vector3I> tmpPos = new();
        for (int i = 0; i < entryCount; i++)
        {
            tmpTypes.Add((BlockType)reader.GetByte());
            tmpPos.Add(reader.GetVector3I());
        }

        BlockTypes = tmpTypes.ToArray();
        BlockPositions = tmpPos.ToArray();
    }
}