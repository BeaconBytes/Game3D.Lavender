using Godot;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity;

namespace Lavender.Common.Entity.Variants.Enemies;

public partial class BuddyEnemy : EnemyEntity
{
    // IDEA: Buddy grabs nearby player and holds them until buddy dies or reaches the end!
    
    // Used for setting up for gameplay IMMEDIATELY after being spawned, but before SetupWave is called
    public override void Setup(uint netId, GameManager manager)
    {
        base.Setup(netId, manager);
        Stats.MovementSpeedMultiplier = 0.72f;
        Stats.Armor = 0.8f;
        Stats.Resistance = 0.66f;
    }

    // Used for final setup after being spawned and after the wave manager acknowledging it.
    public override void SetupWave(Marker3D[] botPathPoints, WaveManager waveManager)
    {
        base.SetupWave(botPathPoints, waveManager);
        
    }

    protected override void HandleTick()
    {
        base.HandleTick();
        
    }

    public void GrabEntity(LivingEntity entity)
    {
        if (IsClient)
            return;
        if (_heldEntity != null)
            return;
        
        _heldEntity = entity;
        _heldEntity.IsControlsFrozen = true;
        Manager.RemoveChild(_heldEntity);
        _grabSocket.AddChild(_heldEntity);
        
        Manager.BroadcastPacketToClients(new EntitySetGrabPacket()
        {
            SourceNetId = NetId,
            TargetNetId = _heldEntity.NetId,
            IsRelease = false,
        });
    }

    public void ReleaseEntity(LivingEntity entity)
    {
        if (IsClient)
            return;
        if (_heldEntity != entity)
            return;

        _grabSocket.RemoveChild(_heldEntity);
        Manager.AddChild(_heldEntity);
        _heldEntity.IsControlsFrozen = false;
        
        Manager.BroadcastPacketToClients(new EntitySetGrabPacket()
        {
            SourceNetId = NetId,
            TargetNetId = _heldEntity.NetId,
            IsRelease = true,
        });
        
        _heldEntity = null;
    }

    [Export]
    private Node3D _grabSocket;

    private LivingEntity _heldEntity;
}