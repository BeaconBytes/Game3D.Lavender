using Godot;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Registers;

namespace Lavender.Common.Entity;

public partial class BrainEntity : LivingEntity
{
    public override void Setup(uint netId, GameManager manager)
    {
        base.Setup(netId, manager);
        
        NavAgent.MaxSpeed = GetMoveSpeed();

        if (IsClient)
            NavAgent.AvoidanceEnabled = false;

        NavAgent.SetAvoidanceLayerValue(1, false);
        NavAgent.SetAvoidanceLayerValue(8, false);
        NavAgent.SetAvoidanceMaskValue(1, false);
        NavAgent.SetAvoidanceMaskValue(8, false);
        if (IsClient)
        {
            NavAgent.SetAvoidanceLayerValue(1, true);
            NavAgent.SetAvoidanceMaskValue(1, true);
        }
        else
        {
            NavAgent.SetAvoidanceLayerValue(8, true);
            NavAgent.SetAvoidanceMaskValue(8, true);
        }
        
        TeleportedEvent += OnTeleported;
        NavAgent.VelocityComputed += OnVelocityComputed;
        Stats.StatChangedEvent += OnStatChanged;
        
        Register.Packets.Subscribe<EntityMoveToPacket>(OnEntityMoveToPacket);
    }


    public override void _ExitTree()
    {
        TeleportedEvent -= OnTeleported;
        NavAgent.VelocityComputed -= OnVelocityComputed;
        
        base._ExitTree();
    }
    
    
    protected override void HandleTick()
    {
        base.HandleTick();

        if (!Enabled)
            return;
        
        if (!IsClient)
        {
            if (_lastSyncedPosition == GlobalPosition)
                return;
            Manager.BroadcastPacketToClients(new EntityMoveToPacket()
            {
                NetId = NetId,
                Position = GlobalPosition,
            });

            _lastSyncedPosition = GlobalPosition;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (!Enabled)
            return;
        
        if (IsClient)
        {
            GlobalPosition = GlobalPosition.Lerp(_targetedMovePos, (float)delta * GetMoveSpeed());
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (Destroyed)
            return;
        
        if(!IsClient)
        {
            if (NavAgent.IsNavigationFinished())
            {
                Vector3 newVel = ProcessMovementVelocity(Vector3.Zero, deltaTime: (float)delta);

                OnVelocityComputed(newVel);
            }
            else
            {
                Vector3 curPos = GlobalPosition;
                Vector3 nextPos = NavAgent.GetNextPathPosition();

                Vector3 newDirectionVector = curPos.DirectionTo(nextPos);
                Vector3 newVel = ProcessMovementVelocity(newDirectionVector, deltaTime: (float)delta);
            
                if (NavAgent.AvoidanceEnabled)
                    NavAgent.Velocity = newVel;
                else
                    OnVelocityComputed(newVel);
            }
            
        }
    }

    public void SetDesiredPathLocation(Vector3 pos)
    {
        NavAgent.TargetPosition = pos;
        LookAt(new Vector3(pos.X, GlobalPosition.Y, pos.Z));
        SnapRotationTo(GlobalRotation);
    }

    protected void TriggerPathCompletedEvent()
    {
        OnCompletedPathEvent?.Invoke(this);
    }
    
    // Event Handlers //
    private void OnEntityMoveToPacket(EntityMoveToPacket packet, uint sourceNetId)
    {
        if (!IsClient || packet.NetId != NetId || sourceNetId != (uint)StaticNetId.Server)
            return;

        _targetedMovePos = packet.Position;
    }
    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        Velocity = safeVelocity;
        MoveAndSlide();
    }
    private void OnTeleported(IGameEntity sourceEntity)
    {
        Velocity = Vector3.Zero;
        NavAgent.SetVelocityForced(Vector3.Zero);
        _targetedMovePos = GlobalPosition;
    }
    private void OnStatChanged(StatType statType, float newValue)
    {
        if (statType is StatType.MoveSpeedBase or StatType.MoveSpeedMultiplier)
        {
            NavAgent.MaxSpeed = GetMoveSpeed();
        }
    }

    [Export]
    protected NavigationAgent3D NavAgent { get; private set; }

    private Vector3 _targetedMovePos = Vector3.Zero;
    private Vector3 _lastSyncedPosition = Vector3.Zero;

    public delegate void BrainCompletedPathHandler(BrainEntity brainEntity);

    public event BrainCompletedPathHandler OnCompletedPathEvent;
}