using Godot;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Enums.Net;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Registers;

namespace Lavender.Common.Entity;

public partial class BrainEntity : LivingEntity
{
    public override void _Ready()
    {
        base._Ready();

        TeleportedEvent += OnTeleported;
        NavAgent.VelocityComputed += OnVelocityComputed;
        NavAgent.MaxSpeed = GetMoveSpeed();
        
        
        Register.Packets.Subscribe<EntityMoveToPacket>(OnEntityMoveToPacket);
    }

    private void OnTeleported(IGameEntity sourceEntity)
    {
        Velocity = Vector3.Zero;
        NavAgent.SetVelocityForced(Vector3.Zero);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        TeleportedEvent -= OnTeleported;
        NavAgent.VelocityComputed -= OnVelocityComputed;
    }


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

    protected override void HandleTick()
    {
        base.HandleTick();

        if (!Enabled)
            return;
        
        if (!IsClient)
        {
            Vector3 curPos = GlobalPosition;
            Vector3 nextPos = NavAgent.GetNextPathPosition();

            Vector3 moveDirVec = curPos.DirectionTo(nextPos);

            Vector3 newVel = ProcessMovementVelocity(moveDirVec);
            Velocity = newVel;
            MoveAndSlide(MinTimeBetweenTicks);
            NavAgent.Velocity = newVel;

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
            // GlobalPosition = GlobalPosition.Lerp(_targetedMovePos, (float)delta * 1.65f);
            GlobalPosition = _targetedMovePos;
        }
    }

    public void SetDesiredPathLocation(Vector3 pos)
    {
        NavAgent.TargetPosition = pos;
    }

    [Export]
    protected NavigationAgent3D NavAgent { get; private set; }

    private Vector3 _targetedMovePos = Vector3.Zero;
    private Vector3 _lastSyncedPosition = Vector3.Zero;
}