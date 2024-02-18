using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Other;

/// <summary>
/// This class/packet is PURELY for debugging certain actions across the network.
/// There is ZERO protocol/use case: just test, then remove test before pushing.
/// </summary>
public class DebugActionPacket : GamePacket
{
    public string Message { get; set; }
    public int Augment { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(Message);
        writer.Put(Augment);
    }

    public override void Deserialize(NetDataReader reader)
    {
        Message = reader.GetString();
        Augment = reader.GetInt();
    }
}