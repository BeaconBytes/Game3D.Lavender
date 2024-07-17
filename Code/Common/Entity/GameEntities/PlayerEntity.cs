using Godot;
using Lavender.Common.Managers;

namespace Lavender.Common.Entity.GameEntities;

public partial class PlayerEntity : HumanoidEntityBase
{
    public override void Setup(uint netId, GameManager manager)
    {
        base.Setup(netId, manager);
    }


    [Export]
    private Camera3D _camera;

    private Vector3 _targetedLerpPosition = Vector3.Zero;
}