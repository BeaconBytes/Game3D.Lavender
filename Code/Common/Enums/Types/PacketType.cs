namespace Lavender.Common.Enums.Types;

public enum PacketType : byte
{
    Unknown,
    
    // OTHER
    DebugAction,

    // Generic
    Handshake,
    Identify,
    AuthMe,
    Heartbeat,
    SetupClient,
    Disconnect,
    
    // World/Map
    WorldSetup,
    MapNotification,

    // Entity Management
    SpawnEntity,
    DestroyEntity,
    ForceSyncEntity,

    // Entity Updates
    EntityRotate,
    EntityMoveTo,
    EntityTeleport,
    EntityInputPayload,
    EntityStatePayload,

    // Structures
    
}