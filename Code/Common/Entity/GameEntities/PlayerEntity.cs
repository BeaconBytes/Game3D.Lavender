using Godot;
using Lavender.Common.Managers;

namespace Lavender.Common.Entity.GameEntities;

public partial class PlayerEntity : HumanoidEntityBase
{
    public override void Setup(uint netId, GameManager manager)
    {
        base.Setup(netId, manager);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (!IsClient)
            return;

        if (Manager.ClientController.ReceiverEntity != this)
        {
            LastProcessedState = LatestServerState;
            _targetedLerpPosition = LatestServerState.position;
            
            WorldPosition = WorldPosition.Lerp(_targetedLerpPosition, Stats.FullMoveSpeed * (float)delta);
            RotateHead(LatestServerState.rotation);
        }
    }


    [Export]
    private Camera3D _camera;

    private Vector3 _targetedLerpPosition = Vector3.Zero;
}