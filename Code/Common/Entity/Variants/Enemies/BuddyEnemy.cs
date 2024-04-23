using Godot;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity;

namespace Lavender.Common.Entity.Variants.Enemies;

public partial class BuddyEnemy : EnemyEntity
{
    private const float GRAB_COOLDOWN_SECONDS = 4f;
    
    
    // IDEA: Buddy grabs nearby player and holds them until buddy dies or reaches the end!
    
    // Used for setting up for gameplay IMMEDIATELY after being spawned, but before SetupWave is called
    public override void Setup(uint netId, GameManager manager)
    {
        base.Setup(netId, manager);
        Stats.MovementSpeedMultiplier = 0.72f;
        Stats.Armor = 0.8f;
        Stats.Resistance = 0.66f;

        DestroyedEvent += OnDestroyed;

        _attackArea.BodyEntered += OnAttackRangeEntered;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        DestroyedEvent -= OnDestroyed;
        
        _attackArea.BodyEntered -= OnAttackRangeEntered;
    }

    // Used for final setup after being spawned and after the wave manager acknowledging it.
    public override void SetupWave(Marker3D[] botPathPoints, WaveManager waveManager)
    {
        base.SetupWave(botPathPoints, waveManager);
        
    }

    protected override void HandleTick()
    {
        base.HandleTick();

        if (Dead || !Enabled)
            return;
        
        if (_heldEntity != null)
        {
            _heldEntity.GlobalPosition = _grabSocket.GlobalPosition;
            
            if(!IsClient)
                _grabbedForTicks++;
        }
        else if(!IsClient)
            _grabCooldownTicks++;
        
        
        if (_grabbedForTicks > GameManager.SERVER_TICK_RATE * 6 && !IsClient)
        {
            ReleaseEntity(_heldEntity);
        }
    }

    protected override void OnSetGrabbedPacket(EntitySetGrabPacket packet, uint sourceNetId)
    {
        base.OnSetGrabbedPacket(packet, sourceNetId);
        if (packet.SourceNetId != NetId || Manager.GetEntityFromNetId(packet.TargetNetId) is not LivingEntity targetLivingEntity)
            return;

        if (packet.IsRelease)
            ReleaseEntity(targetLivingEntity);
        else
            GrabEntity(targetLivingEntity);
    }

    public void GrabEntity(LivingEntity entity)
    {
        if (_heldEntity != null || entity.GrabbedById != (uint)StaticNetId.Null)
            return;
        
        _heldEntity = entity;
        _heldEntity.TriggerGrabbedBy(this);
        
        if (IsClient)
            return;
        
        _grabbedForTicks = 0;
        
        Manager.BroadcastPacketToClients(new EntitySetGrabPacket()
        {
            SourceNetId = NetId,
            TargetNetId = _heldEntity.NetId,
            IsRelease = false,
        });

    }

    public void ReleaseEntity(LivingEntity entity)
    {
        if (_heldEntity != entity || _heldEntity == null)
            return;

        uint heldEntityNetId = _heldEntity.NetId;
        _heldEntity.TriggerGrabbedBy(null);
        _heldEntity = null;
        
        if (IsClient)
            return;
        
        Manager.BroadcastPacketToClients(new EntitySetGrabPacket()
        {
            SourceNetId = NetId,
            TargetNetId = heldEntityNetId,
            IsRelease = true,
        });
        
    }
    
    
    // EVENT HANDLERS //
    private void OnAttackRangeEntered(Node3D body)
    {
        if (_grabCooldownTicks < (GRAB_COOLDOWN_SECONDS * GameManager.SERVER_TICK_RATE))
            return;
        if (GD.Randf() < 0.5f)
            return;


        if (body is PlayerEntity playerEntity && playerEntity != _heldEntity)
        {
            GrabEntity(playerEntity);
            _grabCooldownTicks = 0;
        }
    }
    private void OnDestroyed(IGameEntity sourceEntity)
    {
        if (_heldEntity != null)
            ReleaseEntity(_heldEntity);
    }
    
    // EVENTS //
    public event EntitySourceTargetEventHandler OnGrabbedEntityEvent;
    public event EntitySourceTargetEventHandler OnReleasedEntityEvent;
    
    
    
    

    [Export]
    private Node3D _grabSocket;
    
    [Export]
    private Area3D _attackArea;

    private LivingEntity _heldEntity;

    private uint _grabbedForTicks = 0;
    private uint _grabCooldownTicks = 0;
}