using LiteNetLib.Utils;

namespace Lavender.Common.Networking.Packets.Variants.Protocol;

public class AuthMePacket : GamePacket
{
    public string Username { get; set; }
    public string Password { get; set; }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(Username);
        writer.Put(Password);
    }

    public override void Deserialize(NetDataReader reader)
    {
        Username = reader.GetString();
        Password = reader.GetString();
    }
}