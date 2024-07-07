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
    Acknowledge,
    Heartbeat,
    SetupClient,
    Disconnect,
    Destroy,
    
    // World/Map
    WorldSetup,
    MapNotification,
    
    // Controller
    SpawnController,

    // Entity Management
    SpawnEntity,
    SetControlling,

    // Entity Updates
    EntityRotate,
    EntityMoveTo,
    EntityTeleport,
    EntityInputPayload,
    EntityStatePayload,
    EntitySetGrab,
    EntityHitTarget,
    
    // Entity Data
    EntityValueChanged,

    // Structures
    
}